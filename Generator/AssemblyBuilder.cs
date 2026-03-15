using AST.Nodes;
using AST.Types;
using Symbols;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Generator
{
    internal class AssemblyBuilder
    {
        private readonly SymbolTable _table;
        private AssemblyDefinition _assembly = null!;
        private ModuleDefinition _module = null!;

        // Current emission context
        private TypeDefinition? _currentType;
        private MethodDefinition? _currentMethod;
        private ILProcessor? _il;

        // Track locals per method
        private readonly Dictionary<string, VariableDefinition> _locals = new();
        private readonly Dictionary<string, int> _parameterIndices = new();
        private bool _currentMethodIsInstance;

        // Free-function wrapper types per namespace
        private readonly Dictionary<string, TypeDefinition> _freeModuleTypes = new();

        // Loop context for break/continue (stack of (continueTarget, breakTarget) instruction pairs)
        private readonly Stack<(Instruction continueTarget, Instruction breakTarget)> _loopStack = new();

        internal AssemblyBuilder(SymbolTable table)
        {
            _table = table;
        }

        internal void Init(AssemblyDefinition assembly)
        {
            _assembly = assembly;
            _module = assembly.MainModule;
        }

        internal void Build(INode node)
        {
            if (node is FileNode file)
            {
                EmitFile(file);
            }
        }

        // -------------------------------------------------------------------
        //  Top-level: File / Module
        // -------------------------------------------------------------------

        private void EmitFile(FileNode node)
        {
            foreach (var child in node.Children)
            {
                if (child is ModuleNode module)
                {
                    EmitModule(module);
                }
                // ImportNodes are handled at the Roslyn reference level, not in IL
            }
        }

        private void EmitModule(ModuleNode node)
        {
            foreach (var child in node.Children)
            {
                switch (child)
                {
                    case ClassNode cls:
                        EmitClass(cls, node.Name);
                        break;
                    case StructNode str:
                        EmitStruct(str, node.Name);
                        break;
                    case EnumNode en:
                        EmitEnum(en, node.Name);
                        break;
                    case ContractNode contract:
                        EmitContract(contract, node.Name);
                        break;
                    case FuncNode func:
                        EmitFreeFunction(func, node.Name);
                        break;
                }
            }
        }

        // -------------------------------------------------------------------
        //  Type declarations
        // -------------------------------------------------------------------

        private void EmitClass(ClassNode node, string ns)
        {
            var attrs = TypeAttributes.Class | TypeAttributes.BeforeFieldInit | CecilAccessLevel(node.AccessLevel);

            var baseRef = _module.ImportReference(typeof(object));
            if (node.BaseType is TypeReferenceNode baseTypeRef)
            {
                baseRef = ResolveTypeReference(baseTypeRef);
            }

            var typeDef = new TypeDefinition(ns, node.Name, attrs, baseRef);

            // Contracts → interfaces
            foreach (var contract in node.Contracts)
            {
                if (contract is TypeReferenceNode contractRef)
                {
                    typeDef.Interfaces.Add(new InterfaceImplementation(ResolveTypeReference(contractRef)));
                }
            }

            _module.Types.Add(typeDef);

            var prevType = _currentType;
            _currentType = typeDef;

            if (node.Body != null)
            {
                EmitTypeBody(node.Body);
            }

            _currentType = prevType;
        }

        private void EmitStruct(StructNode node, string ns)
        {
            var attrs = TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit
                        | CecilAccessLevel(node.AccessLevel);

            var valueTypeRef = _module.ImportReference(typeof(System.ValueType));
            var typeDef = new TypeDefinition(ns, node.Name, attrs, valueTypeRef);

            foreach (var contract in node.Contracts)
            {
                if (contract is TypeReferenceNode contractRef)
                {
                    typeDef.Interfaces.Add(new InterfaceImplementation(ResolveTypeReference(contractRef)));
                }
            }

            _module.Types.Add(typeDef);

            var prevType = _currentType;
            _currentType = typeDef;

            if (node.Body != null)
            {
                EmitTypeBody(node.Body);
            }

            _currentType = prevType;
        }

        private void EmitEnum(EnumNode node, string ns)
        {
            var attrs = TypeAttributes.Class | TypeAttributes.Sealed | CecilAccessLevel(node.AccessLevel);
            var enumBaseRef = _module.ImportReference(typeof(System.Enum));
            var typeDef = new TypeDefinition(ns, node.Name, attrs, enumBaseRef);

            // Special __value field for enums
            var valueField = new Mono.Cecil.FieldDefinition(
                "value__",
                FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName,
                _module.ImportReference(typeof(int)));
            typeDef.Fields.Add(valueField);

            // Enum cases
            int enumValue = 0;
            if (node.Body != null)
            {
                foreach (var child in node.Body.Children)
                {
                    if (child is IdentifierNode ident)
                    {
                        var caseField = new Mono.Cecil.FieldDefinition(
                            ident.Value,
                            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal,
                            typeDef);
                        caseField.Constant = enumValue++;
                        typeDef.Fields.Add(caseField);
                    }
                    else if (child is PropertyNode prop)
                    {
                        var caseField = new Mono.Cecil.FieldDefinition(
                            prop.Name,
                            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal,
                            typeDef);
                        caseField.Constant = enumValue++;
                        typeDef.Fields.Add(caseField);
                    }
                }
            }

            _module.Types.Add(typeDef);
        }

        private void EmitContract(ContractNode node, string ns)
        {
            var attrs = TypeAttributes.Interface | TypeAttributes.Abstract | CecilAccessLevel(node.AccessLevel);
            var typeDef = new TypeDefinition(ns, node.Name, attrs, null);

            _module.Types.Add(typeDef);

            var prevType = _currentType;
            _currentType = typeDef;

            if (node.Body != null)
            {
                EmitContractBody(node.Body);
            }

            _currentType = prevType;
        }

        private void EmitContractBody(BlockNode body)
        {
            foreach (var child in body.Children)
            {
                switch (child)
                {
                    case FuncNode func:
                        EmitContractMethod(func);
                        break;
                    case PropertyNode prop:
                        EmitContractProperty(prop);
                        break;
                }
            }
        }

        private void EmitContractMethod(FuncNode node)
        {
            if (_currentType == null || node.ReturnType is not TypeReferenceNode retTypeRef) return;

            var returnType = ResolveTypeReference(retTypeRef);
            var attrs = MethodAttributes.Public | MethodAttributes.HideBySig
                        | MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Virtual;

            var method = new MethodDefinition(
                Shared.Utils.IonaToCSharpName(node.Name),
                attrs,
                returnType);

            foreach (var param in node.Parameters)
            {
                var paramType = ResolveTypeReference(param.TypeNode);
                method.Parameters.Add(new Mono.Cecil.ParameterDefinition(param.Name, ParameterAttributes.None, paramType));
            }

            _currentType.Methods.Add(method);
        }

        private void EmitContractProperty(PropertyNode node)
        {
            if (_currentType == null || node.TypeNode == null) return;

            var propType = ResolveTypeReference(node.TypeNode);
            var propDef = new Mono.Cecil.PropertyDefinition(node.Name, PropertyAttributes.None, propType);

            if (node.Get != null || node.Set == null)
            {
                var getter = new MethodDefinition(
                    $"get_{node.Name}",
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName
                    | MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Virtual,
                    propType);
                propDef.GetMethod = getter;
                _currentType.Methods.Add(getter);
            }

            if (node.Set != null)
            {
                var setter = new MethodDefinition(
                    $"set_{node.Name}",
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName
                    | MethodAttributes.NewSlot | MethodAttributes.Abstract | MethodAttributes.Virtual,
                    _module.ImportReference(typeof(void)));
                setter.Parameters.Add(new Mono.Cecil.ParameterDefinition("value", ParameterAttributes.None, propType));
                propDef.SetMethod = setter;
                _currentType.Methods.Add(setter);
            }

            _currentType.Properties.Add(propDef);
        }

        // -------------------------------------------------------------------
        //  Type body members (class/struct)
        // -------------------------------------------------------------------

        private void EmitTypeBody(BlockNode body)
        {
            foreach (var child in body.Children)
            {
                switch (child)
                {
                    case PropertyNode prop:
                        EmitProperty(prop);
                        break;
                    case FuncNode func:
                        EmitMethod(func);
                        break;
                    case InitNode init:
                        EmitInit(init);
                        break;
                    case OperatorNode op:
                        EmitOperator(op);
                        break;
                }
            }
        }

        private void EmitProperty(PropertyNode node)
        {
            if (_currentType == null || node.TypeNode == null) return;

            var fieldType = ResolveTypeReference(node.TypeNode);
            var isField = node.Get == null && node.Set == null;

            if (isField)
            {
                var fieldAttrs = CecilFieldAccess(node.AccessLevel);
                var fieldDef = new Mono.Cecil.FieldDefinition(node.Name, fieldAttrs, fieldType);
                _currentType.Fields.Add(fieldDef);
            }
            else
            {
                // Backing field
                var backingField = new Mono.Cecil.FieldDefinition(
                    $"<{node.Name}>k__BackingField",
                    FieldAttributes.Private,
                    fieldType);
                _currentType.Fields.Add(backingField);

                var propDef = new Mono.Cecil.PropertyDefinition(node.Name, PropertyAttributes.None, fieldType);

                // Getter
                var getAttrs = CecilMethodAccess(node.AccessLevel) | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
                var getter = new MethodDefinition($"get_{node.Name}", getAttrs, fieldType);
                getter.Body = new Mono.Cecil.Cil.MethodBody(getter);
                var getIl = getter.Body.GetILProcessor();
                getIl.Emit(OpCodes.Ldarg_0);
                getIl.Emit(OpCodes.Ldfld, backingField);
                getIl.Emit(OpCodes.Ret);
                propDef.GetMethod = getter;
                _currentType.Methods.Add(getter);

                // Setter
                if (node.Set != null)
                {
                    var setAttrs = CecilMethodAccess(node.AccessLevel) | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
                    var setter = new MethodDefinition($"set_{node.Name}", setAttrs, _module.ImportReference(typeof(void)));
                    setter.Parameters.Add(new Mono.Cecil.ParameterDefinition("value", ParameterAttributes.None, fieldType));
                    setter.Body = new Mono.Cecil.Cil.MethodBody(setter);
                    var setIl = setter.Body.GetILProcessor();
                    setIl.Emit(OpCodes.Ldarg_0);
                    setIl.Emit(OpCodes.Ldarg_1);
                    setIl.Emit(OpCodes.Stfld, backingField);
                    setIl.Emit(OpCodes.Ret);
                    propDef.SetMethod = setter;
                    _currentType.Methods.Add(setter);
                }

                _currentType.Properties.Add(propDef);
            }
        }

        // -------------------------------------------------------------------
        //  Methods / Constructors / Operators
        // -------------------------------------------------------------------

        private void EmitMethod(FuncNode node)
        {
            if (_currentType == null || node.ReturnType is not TypeReferenceNode retTypeRef) return;

            var returnType = ResolveTypeReference(retTypeRef);
            var attrs = CecilMethodAccess(node.AccessLevel) | MethodAttributes.HideBySig;

            if (node.IsStatic)
                attrs |= MethodAttributes.Static;

            var method = new MethodDefinition(
                Shared.Utils.IonaToCSharpName(node.Name),
                attrs,
                returnType);

            BuildMethodParams(method, node.Parameters, !node.IsStatic);

            method.Body = new Mono.Cecil.Cil.MethodBody(method);

            _currentType.Methods.Add(method);

            EmitMethodBody(method, node.Body, !node.IsStatic);
        }

        private void EmitInit(InitNode node)
        {
            if (_currentType == null) return;

            var attrs = CecilMethodAccess(node.AccessLevel) | MethodAttributes.HideBySig
                        | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

            var ctor = new MethodDefinition(
                ".ctor",
                attrs,
                _module.ImportReference(typeof(void)));

            BuildMethodParams(ctor, node.Parameters, isInstance: true);

            ctor.Body = new Mono.Cecil.Cil.MethodBody(ctor);

            _currentType.Methods.Add(ctor);

            // Emit base..ctor call first
            var il = ctor.Body.GetILProcessor();
            var baseCtorRef = _module.ImportReference(
                _currentType.BaseType.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 0));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, baseCtorRef);

            EmitMethodBody(ctor, node.Body, isInstance: true);
        }

        private void EmitOperator(OperatorNode node)
        {
            if (_currentType == null || node.ReturnType is not TypeReferenceNode retTypeRef) return;

            var returnType = ResolveTypeReference(retTypeRef);
            var attrs = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig
                        | MethodAttributes.SpecialName;

            string methodName = node.Op switch
            {
                OperatorType.Add => "op_Addition",
                OperatorType.Subtract => "op_Subtraction",
                OperatorType.Multiply => "op_Multiply",
                OperatorType.Divide => "op_Division",
                OperatorType.Modulo => "op_Modulus",
                OperatorType.Equal => "op_Equality",
                OperatorType.NotEqual => "op_Inequality",
                OperatorType.GreaterThan => "op_GreaterThan",
                OperatorType.GreaterThanOrEqual => "op_GreaterThanOrEqual",
                OperatorType.LessThan => "op_LessThan",
                OperatorType.LessThanOrEqual => "op_LessThanOrEqual",
                _ => null!
            };

            if (methodName == null) return;

            var method = new MethodDefinition(methodName, attrs, returnType);
            BuildMethodParams(method, node.Parameters, isInstance: false);

            method.Body = new Mono.Cecil.Cil.MethodBody(method);
            _currentType.Methods.Add(method);

            EmitMethodBody(method, node.Body, isInstance: false);
        }

        private void EmitFreeFunction(FuncNode node, string ns)
        {
            if (node.ReturnType is not TypeReferenceNode retTypeRef) return;

            // Get or create the static "Module" wrapper class for this namespace
            if (!_freeModuleTypes.TryGetValue(ns, out var moduleType))
            {
                moduleType = new TypeDefinition(
                    ns,
                    "Module",
                    TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                    _module.ImportReference(typeof(object)));
                _module.Types.Add(moduleType);
                _freeModuleTypes[ns] = moduleType;
            }

            var returnType = ResolveTypeReference(retTypeRef);
            var attrs = CecilMethodAccess(node.AccessLevel) | MethodAttributes.Static | MethodAttributes.HideBySig;

            var method = new MethodDefinition(
                Shared.Utils.IonaToCSharpName(node.Name),
                attrs,
                returnType);

            BuildMethodParams(method, node.Parameters, isInstance: false);

            method.Body = new Mono.Cecil.Cil.MethodBody(method);
            moduleType.Methods.Add(method);

            EmitMethodBody(method, node.Body, isInstance: false);
        }

        // -------------------------------------------------------------------
        //  Method body emission
        // -------------------------------------------------------------------

        private void EmitMethodBody(MethodDefinition method, BlockNode? body, bool isInstance)
        {
            if (body == null)
            {
                var il2 = method.Body.GetILProcessor();
                il2.Emit(OpCodes.Ret);
                return;
            }

            var prevMethod = _currentMethod;
            var prevIl = _il;
            var prevIsInstance = _currentMethodIsInstance;

            _currentMethod = method;
            _il = method.Body.GetILProcessor();
            _currentMethodIsInstance = isInstance;
            _locals.Clear();
            _parameterIndices.Clear();

            // Build parameter index map
            int paramOffset = isInstance ? 1 : 0;
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                _parameterIndices[method.Parameters[i].Name] = i + paramOffset;
            }

            EmitBlock(body);

            // Ensure method ends with ret
            if (_il.Body.Instructions.Count == 0 || _il.Body.Instructions.Last().OpCode != OpCodes.Ret)
            {
                _il.Emit(OpCodes.Ret);
            }

            _currentMethod = prevMethod;
            _il = prevIl;
            _currentMethodIsInstance = prevIsInstance;
        }

        private void BuildMethodParams(MethodDefinition method, List<ParameterNode> parameters, bool isInstance)
        {
            foreach (var param in parameters)
            {
                var paramType = ResolveTypeReference(param.TypeNode);
                method.Parameters.Add(new Mono.Cecil.ParameterDefinition(param.Name, ParameterAttributes.None, paramType));
            }
        }

        // -------------------------------------------------------------------
        //  Statement emission
        // -------------------------------------------------------------------

        private void EmitBlock(BlockNode node)
        {
            foreach (var child in node.Children)
            {
                EmitNode(child);
            }
        }

        private void EmitNode(INode node)
        {
            if (node.Status == INode.ResolutionStatus.Failed) return;

            switch (node)
            {
                case VariableNode variable:
                    EmitVariable(variable);
                    break;
                case AssignmentNode assignment:
                    EmitAssignment(assignment);
                    break;
                case IfNode ifNode:
                    EmitIfStatement(ifNode);
                    break;
                case WhileNode whileLoop:
                    EmitWhileLoop(whileLoop);
                    break;
                case ForNode forLoop:
                    EmitForLoop(forLoop);
                    break;
                case BreakNode:
                    EmitBreak();
                    break;
                case ContinueNode:
                    EmitContinue();
                    break;
                case ReturnNode ret:
                    EmitReturn(ret);
                    break;
                case FuncCallNode funcCall:
                    EmitFuncCall(funcCall);
                    // Pop result if used as statement and method returns non-void
                    if (funcCall.ResultType != null && funcCall.ResultType.FullyQualifiedName != "Iona.Builtins.Void")
                    {
                        _il!.Emit(OpCodes.Pop);
                    }
                    break;
                case InitCallNode initCall:
                    EmitInitCall(initCall);
                    // Pop result if used as statement
                    _il!.Emit(OpCodes.Pop);
                    break;
                case PropAccessNode propAccess:
                    EmitPropAccess(propAccess);
                    break;
                case BinaryExpressionNode binary:
                    EmitBinaryExpression(binary);
                    break;
                case BlockNode block:
                    EmitBlock(block);
                    break;
            }
        }

        private void EmitVariable(VariableNode node)
        {
            if (_il == null || _currentMethod == null) return;

            Mono.Cecil.TypeReference varType;
            if (node.TypeNode != null)
            {
                varType = ResolveTypeReference(node.TypeNode);
            }
            else if (node.Value is IExpressionNode expr && expr.ResultType != null)
            {
                varType = ResolveTypeReference(expr.ResultType);
            }
            else
            {
                varType = _module.ImportReference(typeof(object));
            }

            var localDef = new VariableDefinition(varType);
            _currentMethod.Body.Variables.Add(localDef);
            _locals[node.Name] = localDef;

            if (node.Value != null)
            {
                EmitExpression(node.Value);
                EmitStloc(localDef);
            }
        }

        private void EmitAssignment(AssignmentNode node)
        {
            if (_il == null) return;

            // Simple variable assignment
            if (node.Target is IdentifierNode ident && _locals.TryGetValue(ident.Value, out var localVar))
            {
                if (node.AssignmentType == AssignmentType.Assign)
                {
                    EmitExpression(node.Value);
                    EmitStloc(localVar);
                }
                else
                {
                    // Compound assignment: load, compute, store
                    EmitLdloc(localVar);
                    EmitExpression(node.Value);
                    EmitCompoundOp(node.AssignmentType);
                    EmitStloc(localVar);
                }
                return;
            }

            // Parameter assignment
            if (node.Target is IdentifierNode paramIdent && _parameterIndices.TryGetValue(paramIdent.Value, out var paramIdx))
            {
                EmitExpression(node.Value);
                _il.Emit(OpCodes.Starg, paramIdx);
                return;
            }

            // Property access assignment (this.x = ...)
            if (node.Target is PropAccessNode propAccess)
            {
                EmitPropAccessStore(propAccess, node.Value);
            }
        }

        private void EmitReturn(ReturnNode node)
        {
            if (_il == null) return;

            if (node.Value != null)
            {
                EmitExpression(node.Value);
            }

            _il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits: if condition { body } [else if condition { body }]* [else { body }]
        /// CIL pattern: evaluate condition, brfalse to next clause or end, emit body, br to end
        /// </summary>
        private void EmitIfStatement(IfNode node)
        {
            if (_il == null || _currentMethod == null) return;

            var endLabel = _il.Create(OpCodes.Nop);

            // Emit the "if" branch
            var nextLabel = _il.Create(OpCodes.Nop);
            EmitExpression(node.Condition);
            _il.Emit(OpCodes.Brfalse, nextLabel);
            EmitBlock(node.Body);
            _il.Emit(OpCodes.Br, endLabel);
            _il.Append(nextLabel);

            // Emit "else if" / "else" clauses
            foreach (var clause in node.ElseClauses)
            {
                if (clause.Condition != null)
                {
                    // else if
                    nextLabel = _il.Create(OpCodes.Nop);
                    EmitExpression(clause.Condition);
                    _il.Emit(OpCodes.Brfalse, nextLabel);
                    EmitBlock(clause.Body);
                    _il.Emit(OpCodes.Br, endLabel);
                    _il.Append(nextLabel);
                }
                else
                {
                    // plain else
                    EmitBlock(clause.Body);
                }
            }

            _il.Append(endLabel);
        }

        /// <summary>
        /// Emits: while condition { body }
        /// CIL pattern:
        ///   br condition
        ///   body: ... loop body ...
        ///   condition: evaluate, brtrue body
        ///   exit: (nop)
        /// </summary>
        private void EmitWhileLoop(WhileNode node)
        {
            if (_il == null || _currentMethod == null) return;

            var bodyStart = _il.Create(OpCodes.Nop);
            var conditionStart = _il.Create(OpCodes.Nop);
            var exitLabel = _il.Create(OpCodes.Nop);

            // Jump to condition check first
            _il.Emit(OpCodes.Br, conditionStart);

            // Body
            _il.Append(bodyStart);

            // Push loop context for break/continue
            _loopStack.Push((conditionStart, exitLabel));
            EmitBlock(node.Body);
            _loopStack.Pop();

            // Condition
            _il.Append(conditionStart);
            EmitExpression(node.Condition);
            _il.Emit(OpCodes.Brtrue, bodyStart);

            // Exit
            _il.Append(exitLabel);
        }

        private void EmitForLoop(ForNode node)
        {
            if (_il == null || _currentMethod == null) return;

            if (node.Iterable is RangeExpressionNode range)
            {
                EmitRangeForLoop(node, range);
            }
            // TODO: Collection iteration (for item in items) — requires IEnumerable support
        }

        /// <summary>
        /// Emits a range-based for loop: for x in start...end { body }
        /// CIL pattern:
        ///   init: load start, store to iterator local
        ///   br condition
        ///   body: ... loop body ...
        ///   increment: ldloc iterator, ldc.i4.1, add, stloc iterator
        ///   condition: ldloc iterator, load end, ble body
        ///   exit: (nop)
        /// </summary>
        private void EmitRangeForLoop(ForNode node, RangeExpressionNode range)
        {
            // Create the iterator local variable
            var iteratorType = _module.ImportReference(typeof(int));
            var iteratorLocal = new VariableDefinition(iteratorType);
            _currentMethod!.Body.Variables.Add(iteratorLocal);

            // Register the iterator in locals (unless it's a discard "_")
            if (node.IteratorName != "_")
            {
                _locals[node.IteratorName] = iteratorLocal;
            }

            // Initialize iterator = start
            EmitExpression(range.Start);
            EmitStloc(iteratorLocal);

            // Create label instructions
            var bodyStart = _il!.Create(OpCodes.Nop);
            var incrementStart = _il.Create(OpCodes.Nop);
            var conditionStart = _il.Create(OpCodes.Nop);
            var exitLabel = _il.Create(OpCodes.Nop);

            // Jump to condition check first
            _il.Emit(OpCodes.Br, conditionStart);

            // Body
            _il.Append(bodyStart);

            // Push loop context for break/continue
            _loopStack.Push((incrementStart, exitLabel));

            EmitBlock(node.Body);

            _loopStack.Pop();

            // Increment: iterator = iterator + 1
            _il.Append(incrementStart);
            EmitLdloc(iteratorLocal);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Add);
            EmitStloc(iteratorLocal);

            // Condition: if iterator <= end, goto body
            _il.Append(conditionStart);
            EmitLdloc(iteratorLocal);
            EmitExpression(range.End);
            _il.Emit(OpCodes.Ble, bodyStart);

            // Exit
            _il.Append(exitLabel);

            // Clean up iterator from locals if it was a discard
            if (node.IteratorName == "_")
            {
                // No cleanup needed — local is just unused
            }
        }

        private void EmitBreak()
        {
            if (_il == null || _loopStack.Count == 0) return;
            var (_, breakTarget) = _loopStack.Peek();
            _il.Emit(OpCodes.Br, breakTarget);
        }

        private void EmitContinue()
        {
            if (_il == null || _loopStack.Count == 0) return;
            var (continueTarget, _) = _loopStack.Peek();
            _il.Emit(OpCodes.Br, continueTarget);
        }

        // -------------------------------------------------------------------
        //  Expression emission (pushes value onto stack)
        // -------------------------------------------------------------------

        private void EmitExpression(INode node)
        {
            if (_il == null) return;

            switch (node)
            {
                case LiteralNode literal:
                    EmitLiteral(literal);
                    break;
                case IdentifierNode identifier:
                    EmitIdentifier(identifier);
                    break;
                case BinaryExpressionNode binary:
                    EmitBinaryExpression(binary);
                    break;
                case UnaryExpressionNode unary:
                    EmitUnaryExpression(unary);
                    break;
                case FuncCallNode funcCall:
                    EmitFuncCall(funcCall);
                    break;
                case InitCallNode initCall:
                    EmitInitCall(initCall);
                    break;
                case PropAccessNode propAccess:
                    EmitPropAccess(propAccess);
                    break;
                case ScopeResolutionNode scope:
                    EmitScopeResolution(scope);
                    break;
                case SelfNode:
                    _il.Emit(OpCodes.Ldarg_0);
                    break;
                case EnumCaseAccessNode enumCase:
                    EmitEnumCaseAccess(enumCase);
                    break;
            }
        }

        private void EmitLiteral(LiteralNode node)
        {
            if (_il == null) return;

            switch (node.LiteralType)
            {
                case LiteralType.Integer:
                    EmitLdcI4(int.Parse(node.Value));
                    break;
                case LiteralType.Double:
                    _il.Emit(OpCodes.Ldc_R8, double.Parse(node.Value));
                    break;
                case LiteralType.Float:
                    _il.Emit(OpCodes.Ldc_R4, float.Parse(node.Value));
                    break;
                case LiteralType.Boolean:
                    _il.Emit(node.Value == "true" ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    break;
                case LiteralType.String:
                    _il.Emit(OpCodes.Ldstr, node.Value);
                    break;
                case LiteralType.Char:
                    _il.Emit(OpCodes.Ldc_I4, (int)node.Value[0]);
                    break;
                case LiteralType.Null:
                    _il.Emit(OpCodes.Ldnull);
                    break;
            }
        }

        private void EmitIdentifier(IdentifierNode node)
        {
            if (_il == null) return;

            // Check locals first
            if (_locals.TryGetValue(node.Value, out var localVar))
            {
                EmitLdloc(localVar);
                return;
            }

            // Check parameters
            if (_parameterIndices.TryGetValue(node.Value, out var paramIdx))
            {
                EmitLdarg(paramIdx);
                return;
            }

            // Fallback: try ILValue for renamed identifiers
            if (_locals.TryGetValue(node.ILValue, out var ilLocal))
            {
                EmitLdloc(ilLocal);
                return;
            }

            if (_parameterIndices.TryGetValue(node.ILValue, out var ilParamIdx))
            {
                EmitLdarg(ilParamIdx);
            }
        }

        private void EmitBinaryExpression(BinaryExpressionNode node)
        {
            if (_il == null) return;

            EmitExpression(node.Left);
            EmitExpression(node.Right);

            switch (node.Operation)
            {
                case BinaryOperation.Add:
                    _il.Emit(OpCodes.Add);
                    break;
                case BinaryOperation.Subtract:
                    _il.Emit(OpCodes.Sub);
                    break;
                case BinaryOperation.Multiply:
                    _il.Emit(OpCodes.Mul);
                    break;
                case BinaryOperation.Divide:
                    _il.Emit(OpCodes.Div);
                    break;
                case BinaryOperation.Mod:
                    _il.Emit(OpCodes.Rem);
                    break;
                case BinaryOperation.Equal:
                    _il.Emit(OpCodes.Ceq);
                    break;
                case BinaryOperation.NotEqual:
                    _il.Emit(OpCodes.Ceq);
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Ceq);
                    break;
                case BinaryOperation.GreaterThan:
                    _il.Emit(OpCodes.Cgt);
                    break;
                case BinaryOperation.LessThan:
                    _il.Emit(OpCodes.Clt);
                    break;
                case BinaryOperation.GreaterThanOrEqual:
                    _il.Emit(OpCodes.Clt);
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Ceq);
                    break;
                case BinaryOperation.LessThanOrEqual:
                    _il.Emit(OpCodes.Cgt);
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Ceq);
                    break;
                case BinaryOperation.And:
                    _il.Emit(OpCodes.And);
                    break;
                case BinaryOperation.Or:
                    _il.Emit(OpCodes.Or);
                    break;
            }
        }

        private void EmitUnaryExpression(UnaryExpressionNode node)
        {
            if (_il == null || node.Operand == null) return;

            EmitExpression(node.Operand);

            switch (node.Operation)
            {
                case UnaryOperation.Negation:
                    _il.Emit(OpCodes.Neg);
                    break;
                case UnaryOperation.Not:
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Ceq);
                    break;
                case UnaryOperation.BitwiseNot:
                    _il.Emit(OpCodes.Not);
                    break;
            }
        }

        private void EmitFuncCall(FuncCallNode node)
        {
            if (_il == null) return;

            // Push arguments
            foreach (var arg in node.Args)
            {
                EmitExpression(arg.Value);
            }

            // Resolve the method reference
            var methodRef = ResolveMethodReference(node);
            if (methodRef != null)
            {
                _il.Emit(OpCodes.Call, methodRef);
            }
        }

        private void EmitInitCall(InitCallNode node)
        {
            if (_il == null) return;

            // Push arguments
            foreach (var arg in node.Args)
            {
                EmitExpression(arg.Value);
            }

            // Resolve constructor
            var ctorRef = ResolveCtorReference(node);
            if (ctorRef != null)
            {
                _il.Emit(OpCodes.Newobj, ctorRef);
            }
        }

        private void EmitPropAccess(PropAccessNode node)
        {
            if (_il == null) return;

            // Emit the object
            EmitExpression(node.Object);

            // Then load the field/property
            if (node.Property is IdentifierNode propIdent)
            {
                EmitFieldOrPropertyLoad(propIdent);
            }
            else if (node.Property is FuncCallNode funcCall)
            {
                // Method call on instance - the object is already on stack
                foreach (var arg in funcCall.Args)
                {
                    EmitExpression(arg.Value);
                }
                var methodRef = ResolveMethodReference(funcCall);
                if (methodRef != null)
                {
                    _il.Emit(OpCodes.Callvirt, methodRef);
                }
            }
            else if (node.Property is PropAccessNode nested)
            {
                // Chained access: first load field, then continue
                if (nested.Object is IdentifierNode nestedIdent)
                {
                    EmitFieldOrPropertyLoad(nestedIdent);
                }
                EmitPropAccess(nested);
            }
        }

        private void EmitPropAccessStore(PropAccessNode propAccess, INode value)
        {
            if (_il == null) return;

            // Emit the object
            EmitExpression(propAccess.Object);

            // Emit the value
            EmitExpression(value);

            // Store into field
            if (propAccess.Property is IdentifierNode propIdent && _currentType != null)
            {
                var field = _currentType.Fields.FirstOrDefault(f => f.Name == propIdent.Value || f.Name == propIdent.ILValue);
                if (field != null)
                {
                    _il.Emit(OpCodes.Stfld, field);
                }
            }
        }

        private void EmitFieldOrPropertyLoad(IdentifierNode ident)
        {
            if (_il == null || _currentType == null) return;

            var field = _currentType.Fields.FirstOrDefault(f => f.Name == ident.Value || f.Name == ident.ILValue);
            if (field != null)
            {
                _il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
            }
        }

        private void EmitScopeResolution(ScopeResolutionNode node)
        {
            if (_il == null) return;

            if (node.Property is EnumCaseAccessNode caseAccess)
            {
                EmitEnumCaseAccess(caseAccess);
            }
            else if (node.Property is FuncCallNode funcCall)
            {
                // Static method call
                foreach (var arg in funcCall.Args)
                {
                    EmitExpression(arg.Value);
                }
                var methodRef = ResolveMethodReference(funcCall);
                if (methodRef != null)
                {
                    _il.Emit(OpCodes.Call, methodRef);
                }
            }
            else if (node.Property is IdentifierNode ident)
            {
                // Static field access
                EmitIdentifier(ident);
            }
        }

        private void EmitEnumCaseAccess(EnumCaseAccessNode node)
        {
            if (_il == null) return;

            // Enum values are integer constants
            if (node.ResultType != null)
            {
                var enumType = ResolveTypeReference(node.ResultType);
                var enumTypeDef = enumType.Resolve();
                if (enumTypeDef != null)
                {
                    var caseField = enumTypeDef.Fields.FirstOrDefault(f => f.Name == node.Case.Value);
                    if (caseField != null && caseField.Constant is int val)
                    {
                        EmitLdcI4(val);
                        return;
                    }
                }
            }

            // Fallback
            _il.Emit(OpCodes.Ldc_I4_0);
        }

        // -------------------------------------------------------------------
        //  Compound assignment helpers
        // -------------------------------------------------------------------

        private void EmitCompoundOp(AssignmentType type)
        {
            if (_il == null) return;

            switch (type)
            {
                case AssignmentType.AddAssign:
                    _il.Emit(OpCodes.Add);
                    break;
                case AssignmentType.SubAssign:
                    _il.Emit(OpCodes.Sub);
                    break;
                case AssignmentType.MulAssign:
                    _il.Emit(OpCodes.Mul);
                    break;
                case AssignmentType.DivAssign:
                    _il.Emit(OpCodes.Div);
                    break;
                case AssignmentType.ModAssign:
                    _il.Emit(OpCodes.Rem);
                    break;
                case AssignmentType.AndAssign:
                    _il.Emit(OpCodes.And);
                    break;
                case AssignmentType.OrAssign:
                    _il.Emit(OpCodes.Or);
                    break;
                case AssignmentType.XorAssign:
                    _il.Emit(OpCodes.Xor);
                    break;
                case AssignmentType.ShlAssign:
                    _il.Emit(OpCodes.Shl);
                    break;
                case AssignmentType.ShrAssign:
                    _il.Emit(OpCodes.Shr);
                    break;
            }
        }

        // -------------------------------------------------------------------
        //  IL helpers
        // -------------------------------------------------------------------

        private void EmitLdcI4(int value)
        {
            switch (value)
            {
                case -1: _il!.Emit(OpCodes.Ldc_I4_M1); break;
                case 0: _il!.Emit(OpCodes.Ldc_I4_0); break;
                case 1: _il!.Emit(OpCodes.Ldc_I4_1); break;
                case 2: _il!.Emit(OpCodes.Ldc_I4_2); break;
                case 3: _il!.Emit(OpCodes.Ldc_I4_3); break;
                case 4: _il!.Emit(OpCodes.Ldc_I4_4); break;
                case 5: _il!.Emit(OpCodes.Ldc_I4_5); break;
                case 6: _il!.Emit(OpCodes.Ldc_I4_6); break;
                case 7: _il!.Emit(OpCodes.Ldc_I4_7); break;
                case 8: _il!.Emit(OpCodes.Ldc_I4_8); break;
                default:
                    if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                        _il!.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    else
                        _il!.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
        }

        private void EmitLdloc(VariableDefinition local)
        {
            int idx = local.Index;
            switch (idx)
            {
                case 0: _il!.Emit(OpCodes.Ldloc_0); break;
                case 1: _il!.Emit(OpCodes.Ldloc_1); break;
                case 2: _il!.Emit(OpCodes.Ldloc_2); break;
                case 3: _il!.Emit(OpCodes.Ldloc_3); break;
                default: _il!.Emit(OpCodes.Ldloc, local); break;
            }
        }

        private void EmitStloc(VariableDefinition local)
        {
            int idx = local.Index;
            switch (idx)
            {
                case 0: _il!.Emit(OpCodes.Stloc_0); break;
                case 1: _il!.Emit(OpCodes.Stloc_1); break;
                case 2: _il!.Emit(OpCodes.Stloc_2); break;
                case 3: _il!.Emit(OpCodes.Stloc_3); break;
                default: _il!.Emit(OpCodes.Stloc, local); break;
            }
        }

        private void EmitLdarg(int index)
        {
            switch (index)
            {
                case 0: _il!.Emit(OpCodes.Ldarg_0); break;
                case 1: _il!.Emit(OpCodes.Ldarg_1); break;
                case 2: _il!.Emit(OpCodes.Ldarg_2); break;
                case 3: _il!.Emit(OpCodes.Ldarg_3); break;
                default: _il!.Emit(OpCodes.Ldarg, index); break;
            }
        }

        // -------------------------------------------------------------------
        //  Type resolution
        // -------------------------------------------------------------------

        internal Mono.Cecil.TypeReference ResolveTypeReference(TypeReferenceNode node)
        {
            return ResolveTypeByFQN(node.FullyQualifiedName, node.TypeKind);
        }

        internal Mono.Cecil.TypeReference ResolveTypeByFQN(string fqn, Kind kind = Kind.Unknown)
        {
            // Check builtins first
            switch (fqn)
            {
                case "Iona.Builtins.Bool": return _module.ImportReference(typeof(bool));
                case "Iona.Builtins.Double": return _module.ImportReference(typeof(double));
                case "Iona.Builtins.Float": return _module.ImportReference(typeof(float));
                case "Iona.Builtins.Int8": return _module.ImportReference(typeof(sbyte));
                case "Iona.Builtins.Int16": return _module.ImportReference(typeof(short));
                case "Iona.Builtins.Int32": return _module.ImportReference(typeof(int));
                case "Iona.Builtins.Int64": return _module.ImportReference(typeof(long));
                case "Iona.Builtins.Int": return _module.ImportReference(typeof(nint));
                case "Iona.Builtins.UInt8": return _module.ImportReference(typeof(byte));
                case "Iona.Builtins.UInt16": return _module.ImportReference(typeof(ushort));
                case "Iona.Builtins.UInt32": return _module.ImportReference(typeof(uint));
                case "Iona.Builtins.UInt64": return _module.ImportReference(typeof(ulong));
                case "Iona.Builtins.UInt": return _module.ImportReference(typeof(nuint));
                case "Iona.Builtins.String": return _module.ImportReference(typeof(string));
                case "Iona.Builtins.Void": return _module.ImportReference(typeof(void));
            }

            // Check types already in the module
            var localType = _module.Types.FirstOrDefault(t => $"{t.Namespace}.{t.Name}" == fqn);
            if (localType != null) return localType;

            // Check well-known System types
            var systemType = System.Type.GetType(fqn);
            if (systemType != null) return _module.ImportReference(systemType);

            // Fallback: create a type reference
            var lastDot = fqn.LastIndexOf('.');
            var ns = lastDot >= 0 ? fqn[..lastDot] : "";
            var name = lastDot >= 0 ? fqn[(lastDot + 1)..] : fqn;
            var isValueType = kind == Kind.Struct || kind == Kind.Enum;

            return new Mono.Cecil.TypeReference(ns, name, _module, _module) { IsValueType = isValueType };
        }

        private MethodReference? ResolveMethodReference(FuncCallNode node)
        {
            // Try to resolve from the symbol table
            var targetName = node.Target.ILValue;
            var csharpName = Shared.Utils.IonaToCSharpName(targetName);

            // Search in current type
            if (_currentType != null)
            {
                var localMethod = _currentType.Methods.FirstOrDefault(m => m.Name == csharpName);
                if (localMethod != null) return localMethod;
            }

            // Search in all module types (including free-function Module classes)
            foreach (var type in _module.Types)
            {
                var method = type.Methods.FirstOrDefault(m => m.Name == csharpName);
                if (method != null) return method;
            }

            // Try .NET framework methods via reflection
            if (node.Target.ResultType != null)
            {
                var ownerType = ResolveTypeReference(node.Target.ResultType);
                var resolved = ownerType.Resolve();
                if (resolved != null)
                {
                    var method = resolved.Methods.FirstOrDefault(m => m.Name == csharpName && m.Parameters.Count == node.Args.Count);
                    if (method != null) return _module.ImportReference(method);
                }
            }

            return null;
        }

        private MethodReference? ResolveCtorReference(InitCallNode node)
        {
            var typeRef = ResolveTypeByFQN(node.TypeFullName);
            var resolved = typeRef.Resolve();
            if (resolved != null)
            {
                var ctor = resolved.Methods.FirstOrDefault(m => m.IsConstructor && m.Parameters.Count == node.Args.Count);
                if (ctor != null) return _module.ImportReference(ctor);
            }

            return null;
        }

        // -------------------------------------------------------------------
        //  Cecil attribute helpers
        // -------------------------------------------------------------------

        private static TypeAttributes CecilAccessLevel(AccessLevel level)
        {
            return level switch
            {
                AccessLevel.Public => TypeAttributes.Public,
                AccessLevel.Private => TypeAttributes.NotPublic,
                _ => TypeAttributes.NotPublic
            };
        }

        private static MethodAttributes CecilMethodAccess(AccessLevel level)
        {
            return level switch
            {
                AccessLevel.Public => MethodAttributes.Public,
                AccessLevel.Private => MethodAttributes.Private,
                _ => MethodAttributes.Assembly
            };
        }

        private static FieldAttributes CecilFieldAccess(AccessLevel level)
        {
            return level switch
            {
                AccessLevel.Public => FieldAttributes.Public,
                AccessLevel.Private => FieldAttributes.Private,
                _ => FieldAttributes.Assembly
            };
        }
    }
}
