//|--- AssemblyBuilder.cs --------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

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

        // Async method emission context
        private bool _isAsyncMoveNext;
        private TypeDefinition? _stateMachineType;
        private FieldDefinition? _stateField;
        private FieldDefinition? _builderField;
        private TypeReference? _builderType;
        private int _currentAwaitIndex;
        private readonly Dictionary<string, FieldDefinition> _hoistedLocals = new();
        private readonly Dictionary<string, FieldDefinition> _hoistedParams = new();
        private FieldDefinition? _asyncThisField;
        private Instruction? _asyncReturnLabel;
        private Instruction? _asyncSuccessLabel;
        private Instruction[]? _asyncResumeLabels;
        private bool _asyncIsVoid;
        private VariableDefinition? _asyncResultLocal;

        // Cached NullableAttribute type and constructor for CIL metadata
        private TypeDefinition? _nullableAttrType;
        private MethodDefinition? _nullableAttrCtorByte;

        // Entry point method for exe output (set when a free function named "main" is found)
        internal MethodDefinition? EntryPoint { get; private set; }

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
                    case RecordNode rec:
                        EmitRecord(rec, node.Name);
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
            // Closed by default: emit Sealed unless the class is marked `open`.
            var attrs = TypeAttributes.Class | TypeAttributes.BeforeFieldInit | CecilAccessLevel(node.AccessLevel);
            if (!node.IsOpen)
            {
                attrs |= TypeAttributes.Sealed;
            }

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

        private void EmitRecord(RecordNode node, string ns)
        {
            // Records are immutable value types (stack-allocated, structural equality via
            // System.ValueType). Immutability is enforced by the type checker.
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

            SynthesizeMemberwiseCtor(node.Body);

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

            SynthesizeMemberwiseCtor(node.Body);

            _currentType = prevType;
        }

        // Records and structs without an explicit init get a memberwise initializer taking
        // all stored properties as parameters (named after them) and assigning each field.
        private void SynthesizeMemberwiseCtor(BlockNode? body)
        {
            if (_currentType == null || body == null)
            {
                return;
            }

            if (_currentType.Methods.Any(m => m.IsConstructor))
            {
                return;
            }

            var storedProps = body.Children.OfType<PropertyNode>()
                .Where(p => p.Get == null && p.Set == null && p.TypeNode != null)
                .ToList();

            var ctorAttrs = MethodAttributes.Public | MethodAttributes.HideBySig
                | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            var ctor = new MethodDefinition(".ctor", ctorAttrs, _module.ImportReference(typeof(void)));

            foreach (var prop in storedProps)
            {
                var paramType = ResolveTypeReference(prop.TypeNode!);
                ctor.Parameters.Add(new ParameterDefinition(prop.Name, ParameterAttributes.None, paramType));
            }

            ctor.Body = new Mono.Cecil.Cil.MethodBody(ctor);
            _currentType.Methods.Add(ctor);

            var il = ctor.Body.GetILProcessor();

            var baseCtor = _currentType.BaseType?.Resolve()?.Methods
                .FirstOrDefault(m => m.IsConstructor && m.Parameters.Count == 0);
            if (baseCtor != null && !_currentType.IsValueType)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, _module.ImportReference(baseCtor));
            }

            var argIndex = 1;
            foreach (var prop in storedProps)
            {
                var field = _currentType.Fields.FirstOrDefault(f => f.Name == prop.Name);
                if (field != null)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    switch (argIndex)
                    {
                        case 1: il.Emit(OpCodes.Ldarg_1); break;
                        case 2: il.Emit(OpCodes.Ldarg_2); break;
                        case 3: il.Emit(OpCodes.Ldarg_3); break;
                        default: il.Emit(OpCodes.Ldarg, argIndex); break;
                    }
                    il.Emit(OpCodes.Stfld, field);
                }
                argIndex++;
            }

            il.Emit(OpCodes.Ret);
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

            ApplyNullableAttribute(method.MethodReturnType, retTypeRef, returnType);

            foreach (var param in node.Parameters)
            {
                var paramType = ResolveTypeReference(param.TypeNode);
                var paramDef = new Mono.Cecil.ParameterDefinition(param.Name, ParameterAttributes.None, paramType);
                ApplyNullableAttribute(paramDef, param.TypeNode, paramType);
                method.Parameters.Add(paramDef);
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

            ApplyNullableAttribute(propDef, node.TypeNode, propType);
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
                ApplyNullableAttribute(fieldDef, node.TypeNode, fieldType);
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

                ApplyNullableAttribute(propDef, node.TypeNode, fieldType);
                _currentType.Properties.Add(propDef);
            }
        }

        // -------------------------------------------------------------------
        //  Methods / Constructors / Operators
        // -------------------------------------------------------------------

        private void EmitMethod(FuncNode node)
        {
            if (_currentType == null || node.ReturnType is not TypeReferenceNode retTypeRef) return;

            if (node.IsAsync)
            {
                var attrs = CecilMethodAccess(node.AccessLevel) | MethodAttributes.HideBySig;
                if (node.IsStatic) attrs |= MethodAttributes.Static;
                if (node.IsOpen) attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot;
                else if (node.IsOverride) attrs |= MethodAttributes.Virtual;
                EmitAsyncMethod(node, _currentType, attrs, !node.IsStatic);
                return;
            }

            var returnType = ResolveTypeReference(retTypeRef);
            var attrs2 = CecilMethodAccess(node.AccessLevel) | MethodAttributes.HideBySig;

            if (node.IsStatic)
                attrs2 |= MethodAttributes.Static;

            // `open` opens a fresh virtual slot; `override` reuses the inherited slot.
            if (node.IsOpen)
            {
                attrs2 |= MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (node.IsOverride)
            {
                attrs2 |= MethodAttributes.Virtual;
            }

            var method = new MethodDefinition(
                Shared.Utils.IonaToCSharpName(node.Name),
                attrs2,
                returnType);

            // Apply [Nullable] to return type for reference types
            if (retTypeRef.Name != "Void")
            {
                ApplyNullableAttribute(method.MethodReturnType, retTypeRef, returnType);
            }

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

            if (node.IsAsync)
            {
                var attrs = CecilMethodAccess(node.AccessLevel) | MethodAttributes.Static | MethodAttributes.HideBySig;
                EmitAsyncMethod(node, moduleType, attrs, isInstance: false);
                return;
            }

            var returnType = ResolveTypeReference(retTypeRef);
            var attrs2 = CecilMethodAccess(node.AccessLevel) | MethodAttributes.Static | MethodAttributes.HideBySig;

            var method = new MethodDefinition(
                Shared.Utils.IonaToCSharpName(node.Name),
                attrs2,
                returnType);

            ApplyNullableAttribute(method.MethodReturnType, retTypeRef, returnType);

            BuildMethodParams(method, node.Parameters, isInstance: false);

            method.Body = new Mono.Cecil.Cil.MethodBody(method);
            moduleType.Methods.Add(method);

            EmitMethodBody(method, node.Body, isInstance: false);

            // Track entry point: a free function named "main" becomes the program entry point
            if (node.Name == "main")
            {
                EntryPoint = method;
            }
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
                var paramDef = new Mono.Cecil.ParameterDefinition(param.Name, ParameterAttributes.None, paramType);
                ApplyNullableAttribute(paramDef, param.TypeNode, paramType);
                method.Parameters.Add(paramDef);
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
                case GuardNode guardNode:
                    EmitGuardStatement(guardNode);
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
                case ScopeResolutionNode scope:
                {
                    var scopeMethodRef = EmitScopeResolution(scope);
                    // Pop result if used as statement and method returns non-void at IL level
                    if (scopeMethodRef != null && scopeMethodRef.ReturnType.FullName != "System.Void")
                    {
                        _il!.Emit(OpCodes.Pop);
                    }
                    break;
                }
                case PropAccessNode propAccess:
                    EmitPropAccess(propAccess);
                    break;
                case AwaitExpressionNode awaitExpr:
                    EmitAwaitExpression(awaitExpr);
                    // Pop result if used as statement and await produces a value
                    // (void awaits and Task awaits produce nothing on the stack)
                    {
                        var awaitFqn = awaitExpr.ResultType?.FullyQualifiedName ?? "Iona.Builtins.Void";
                        if (awaitFqn != "Iona.Builtins.Void" && awaitFqn != "System.Threading.Tasks.Task")
                            _il!.Emit(OpCodes.Pop);
                    }
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

            // For scope resolution calls, resolve the actual return type via Cecil
            // (the type system may not fully represent array types, etc.)
            var cecilType = TryResolveCecilReturnType(node.Value);
            if (cecilType != null)
            {
                varType = cecilType;
            }
            else if (node.TypeNode != null)
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

            // In async MoveNext, hoist locals to state machine fields
            if (_isAsyncMoveNext && _stateMachineType != null)
            {
                var field = new FieldDefinition(
                    node.Name,
                    FieldAttributes.Public,
                    varType);
                _stateMachineType.Fields.Add(field);
                _hoistedLocals[node.Name] = field;

                if (node.Value != null)
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    EmitExpression(node.Value);
                    _il.Emit(OpCodes.Stfld, field);
                }
                return;
            }

            var localDef = new VariableDefinition(varType);
            _currentMethod.Body.Variables.Add(localDef);
            _locals[node.Name] = localDef;

            if (node.Value != null)
            {
                EmitExpression(node.Value);
                EmitWrapOptionalValueType(varType, node.Value);
                EmitStloc(localDef);
            }
        }

        // Wrap a non-optional value-type value in Nullable<T> when assigning to an optional
        // value-type target (e.g. `let x: Int32? = 5`).
        private void EmitWrapOptionalValueType(Mono.Cecil.TypeReference targetType, IExpressionNode value)
        {
            if (_il == null)
            {
                return;
            }

            if (targetType is not GenericInstanceType nullableType
                || nullableType.ElementType.FullName != "System.Nullable`1")
            {
                return;
            }

            if (value.ResultType != null && value.ResultType.IsOptional)
            {
                return;
            }

            var ctor = _module.ImportReference(
                    typeof(System.Nullable<>).GetConstructors()[0])
                .MakeHostInstanceGeneric(nullableType);
            _il.Emit(OpCodes.Newobj, ctor);
        }

        private void EmitAssignment(AssignmentNode node)
        {
            if (_il == null) return;

            // Async: hoisted local assignment
            if (_isAsyncMoveNext && node.Target is IdentifierNode asyncIdent
                && _hoistedLocals.TryGetValue(asyncIdent.Value, out var hoistedField))
            {
                if (node.AssignmentType == AssignmentType.Assign)
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    EmitExpression(node.Value);
                    _il.Emit(OpCodes.Stfld, hoistedField);
                }
                else
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Dup);
                    _il.Emit(OpCodes.Ldfld, hoistedField);
                    EmitExpression(node.Value);
                    EmitCompoundOp(node.AssignmentType);
                    _il.Emit(OpCodes.Stfld, hoistedField);
                }
                return;
            }

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

            if (_isAsyncMoveNext && _asyncSuccessLabel != null)
            {
                // Async return: store result and leave to success exit
                if (!_asyncIsVoid && node.Value != null && _asyncResultLocal != null)
                {
                    EmitExpression(node.Value);
                    EmitStloc(_asyncResultLocal);
                }
                _il.Emit(OpCodes.Leave, _asyncSuccessLabel);
                return;
            }

            if (node.Value != null)
            {
                EmitExpression(node.Value);
            }

            _il.Emit(OpCodes.Ret);
        }

        private void EmitGuardStatement(GuardNode node)
        {
            if (_il == null || _currentMethod == null) return;

            var continueLabel = _il.Create(OpCodes.Nop);

            if (node.BindingName != null && node.BindingExpression != null)
            {
                // Binding guard: guard var/let name = expr else { ... }
                // Emit the expression (the optional value)
                EmitExpression(node.BindingExpression);

                // Resolve the type of the expression to determine if it's Nullable<T> or reference
                Mono.Cecil.TypeReference exprType;
                if (node.BindingTypeNode != null)
                {
                    exprType = ResolveTypeReference(node.BindingTypeNode);
                }
                else
                {
                    exprType = _module.ImportReference(typeof(object));
                }

                bool isNullableValueType = exprType is GenericInstanceType git
                    && git.ElementType.FullName == "System.Nullable`1";

                if (isNullableValueType)
                {
                    var nullableType = (GenericInstanceType)exprType;
                    var innerType = nullableType.GenericArguments[0];

                    // Store the Nullable<T> in a temp local
                    var tempLocal = new VariableDefinition(exprType);
                    _currentMethod.Body.Variables.Add(tempLocal);
                    EmitStloc(tempLocal);

                    // Create the unwrapped binding local (inner type, not Nullable<T>)
                    var bindingLocal = new VariableDefinition(innerType);
                    _currentMethod.Body.Variables.Add(bindingLocal);
                    _locals[node.BindingName] = bindingLocal;

                    // Check .HasValue
                    EmitLdloca(tempLocal);
                    var hasValueMethod = _module.ImportReference(
                        exprType.Resolve().Properties.First(p => p.Name == "HasValue").GetMethod)
                        .MakeHostInstanceGeneric(nullableType);
                    _il.Emit(OpCodes.Call, hasValueMethod);
                    _il.Emit(OpCodes.Brtrue, continueLabel);

                    // No value — emit else body
                    EmitBlock(node.Body);

                    // After else body (which must return/break), extract .Value into binding
                    var afterElse = _il.Create(OpCodes.Nop);
                    _il.Emit(OpCodes.Br, afterElse);
                    _il.Append(continueLabel);

                    EmitLdloca(tempLocal);
                    var getValueMethod = _module.ImportReference(
                        exprType.Resolve().Properties.First(p => p.Name == "Value").GetMethod)
                        .MakeHostInstanceGeneric(nullableType);
                    _il.Emit(OpCodes.Call, getValueMethod);
                    EmitStloc(bindingLocal);

                    _il.Append(afterElse);
                }
                else
                {
                    // Reference type — original logic
                    var bindingLocal = new VariableDefinition(exprType);
                    _currentMethod.Body.Variables.Add(bindingLocal);
                    _locals[node.BindingName] = bindingLocal;

                    // Duplicate: one for null check, one for storing
                    _il.Emit(OpCodes.Dup);
                    EmitStloc(bindingLocal);

                    // If not null, jump to continue
                    _il.Emit(OpCodes.Brtrue, continueLabel);

                    // Value was null — emit the else body
                    EmitBlock(node.Body);

                    _il.Append(continueLabel);
                }
            }
            else if (node.Condition != null)
            {
                // Condition guard: guard condition else { ... }
                EmitExpression(node.Condition);
                _il.Emit(OpCodes.Brtrue, continueLabel);

                // Condition was false — emit the else body
                EmitBlock(node.Body);

                // Continue execution
                _il.Append(continueLabel);
            }
        }

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
                case ArrayAccessNode arrayAccess:
                    EmitArrayAccess(arrayAccess);
                    break;
                case LiteralNode literal:
                    EmitLiteral(literal);
                    break;
                case InterpolatedStringNode interp:
                    EmitInterpolatedString(interp);
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
                case AwaitExpressionNode awaitExpr:
                    EmitAwaitExpression(awaitExpr);
                    break;
                case SelfNode:
                    if (_isAsyncMoveNext && _asyncThisField != null)
                    {
                        _il.Emit(OpCodes.Ldarg_0);
                        _il.Emit(OpCodes.Ldfld, _asyncThisField);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Ldarg_0);
                    }
                    break;
                case SuperNode:
                    // `super` and `self` both push `this`; the difference is the call site —
                    // EmitPropAccess uses non-virtual `call` (and resolves on the base type)
                    // when the receiver is SuperNode.
                    _il.Emit(OpCodes.Ldarg_0);
                    break;
                case EnumCaseAccessNode enumCase:
                    EmitEnumCaseAccess(enumCase);
                    break;
                case ForceUnwrapNode forceUnwrap:
                    EmitForceUnwrap(forceUnwrap);
                    break;
            }
        }

        private void EmitForceUnwrap(ForceUnwrapNode node)
        {
            if (_il == null || _currentMethod == null) return;

            EmitExpression(node.Expression);

            var exprType = node.Expression.ResultType != null
                ? ResolveTypeReference(node.Expression.ResultType)
                : _module.ImportReference(typeof(object));

            if (exprType is GenericInstanceType nullableType
                && nullableType.ElementType.FullName == "System.Nullable`1")
            {
                var temp = new VariableDefinition(exprType);
                _currentMethod.Body.Variables.Add(temp);
                EmitStloc(temp);
                EmitLdloca(temp);

                var getValue = _module.ImportReference(
                        exprType.Resolve().Properties.First(p => p.Name == "Value").GetMethod)
                    .MakeHostInstanceGeneric(nullableType);
                _il.Emit(OpCodes.Call, getValue);
                return;
            }

            var ok = _il.Create(OpCodes.Nop);
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Brtrue, ok);
            _il.Emit(OpCodes.Ldstr, "unexpectedly found nil while unwrapping an optional value");
            var ctor = _module.ImportReference(
                typeof(System.InvalidOperationException).GetConstructor(new[] { typeof(string) }));
            _il.Emit(OpCodes.Newobj, ctor);
            _il.Emit(OpCodes.Throw);
            _il.Append(ok);
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
                    // Strip surrounding quotes added by the lexer, then resolve standard
                    // backslash escapes (\n, \t, \r, \0, \\, \", \$).
                    var strVal = node.Value;
                    if (strVal.Length >= 2 && strVal.StartsWith('"') && strVal.EndsWith('"'))
                        strVal = strVal[1..^1];
                    _il.Emit(OpCodes.Ldstr, UnescapeStringLiteral(strVal));
                    break;
                case LiteralType.Char:
                    _il.Emit(OpCodes.Ldc_I4, (int)node.Value[0]);
                    break;
                case LiteralType.Null:
                    _il.Emit(OpCodes.Ldnull);
                    break;
            }
        }

        private static string UnescapeStringLiteral(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];
                    switch (next)
                    {
                        case 'n': sb.Append('\n'); i++; continue;
                        case 't': sb.Append('\t'); i++; continue;
                        case 'r': sb.Append('\r'); i++; continue;
                        case '0': sb.Append('\0'); i++; continue;
                        case '\\': sb.Append('\\'); i++; continue;
                        case '"': sb.Append('"'); i++; continue;
                        case '$': sb.Append('$'); i++; continue;
                    }
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        // Lower a Kotlin-style `"text ${expr} more"` to `string.Concat(object?[]{ ... })`.
        // Concat's params-object overload internally calls ToString on each non-string element
        // and treats null as "", which matches Kotlin's interpolation semantics.
        private void EmitInterpolatedString(InterpolatedStringNode node)
        {
            if (_il == null) return;

            if (node.Segments.Count == 0)
            {
                _il.Emit(OpCodes.Ldstr, string.Empty);
                return;
            }

            var objectType = _module.ImportReference(typeof(object));
            var concatMethod = _module.ImportReference(
                typeof(string).GetMethod("Concat", new[] { typeof(object[]) })!);

            EmitLdcI4(node.Segments.Count);
            _il.Emit(OpCodes.Newarr, objectType);

            for (int i = 0; i < node.Segments.Count; i++)
            {
                _il.Emit(OpCodes.Dup);
                EmitLdcI4(i);
                EmitExpression(node.Segments[i]);

                // Box value types so the object[] slot is well-typed.
                var segExpr = node.Segments[i] as IExpressionNode;
                if (segExpr?.ResultType != null)
                {
                    var segType = ResolveTypeByFQN(segExpr.ResultType.FullyQualifiedName, segExpr.ResultType.TypeKind);
                    var segDef = segType.Resolve();
                    if (segDef != null && segDef.IsValueType)
                    {
                        _il.Emit(OpCodes.Box, segType);
                    }
                }
                _il.Emit(OpCodes.Stelem_Ref);
            }

            _il.Emit(OpCodes.Call, concatMethod);
        }

        private void EmitIdentifier(IdentifierNode node)
        {
            if (_il == null) return;

            // Async: check hoisted locals and params first
            if (_isAsyncMoveNext)
            {
                if (_hoistedLocals.TryGetValue(node.Value, out var hoistedLocal))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Ldfld, hoistedLocal);
                    return;
                }
                if (_hoistedParams.TryGetValue(node.Value, out var hoistedParam))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Ldfld, hoistedParam);
                    return;
                }
                if (_hoistedLocals.TryGetValue(node.ILValue, out var hoistedIlLocal))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Ldfld, hoistedIlLocal);
                    return;
                }
            }

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
            if (_il == null || _currentMethod == null) return;

            // Emit the object
            EmitExpression(node.Object);

            // Optional chaining: if object is null/no-value, skip property access and push null/default
            if (node.IsOptionalChain)
            {
                // Determine if the object expression is a Nullable<T> value type
                Mono.Cecil.TypeReference? objType = null;
                if (node.Object is IdentifierNode objIdent && _currentType != null)
                {
                    var field = _currentType.Fields.FirstOrDefault(f => f.Name == objIdent.Value || f.Name == objIdent.ILValue);
                    if (field != null) objType = field.FieldType;
                }

                bool isNullableValueType = objType is GenericInstanceType git
                    && git.ElementType.FullName == "System.Nullable`1";

                if (isNullableValueType)
                {
                    var nullableType = (GenericInstanceType)objType!;
                    var endLabel = _il.Create(OpCodes.Nop);
                    var nullLabel = _il.Create(OpCodes.Nop);

                    // Store the Nullable<T> value in a temp local
                    var tempLocal = new VariableDefinition(nullableType);
                    _currentMethod.Body.Variables.Add(tempLocal);
                    EmitStloc(tempLocal);

                    // Check .HasValue
                    EmitLdloca(tempLocal);
                    var hasValueMethod = _module.ImportReference(
                        objType!.Resolve().Properties.First(p => p.Name == "HasValue").GetMethod)
                        .MakeHostInstanceGeneric(nullableType);
                    _il.Emit(OpCodes.Call, hasValueMethod);
                    _il.Emit(OpCodes.Brfalse, nullLabel);

                    // Has value — extract .Value and access property
                    EmitLdloca(tempLocal);
                    var getValueMethod = _module.ImportReference(
                        objType.Resolve().Properties.First(p => p.Name == "Value").GetMethod)
                        .MakeHostInstanceGeneric(nullableType);
                    _il.Emit(OpCodes.Call, getValueMethod);
                    EmitPropAccessMember(node);
                    _il.Emit(OpCodes.Br, endLabel);

                    // No value — push null
                    _il.Append(nullLabel);
                    _il.Emit(OpCodes.Ldnull);

                    _il.Append(endLabel);
                }
                else
                {
                    // Reference type — null check with dup
                    var nullLabel = _il.Create(OpCodes.Nop);
                    var endLabel = _il.Create(OpCodes.Nop);

                    _il.Emit(OpCodes.Dup);
                    _il.Emit(OpCodes.Brfalse, nullLabel);

                    EmitPropAccessMember(node);
                    _il.Emit(OpCodes.Br, endLabel);

                    _il.Append(nullLabel);
                    _il.Emit(OpCodes.Pop);
                    _il.Emit(OpCodes.Ldnull);

                    _il.Append(endLabel);
                }
            }
            else
            {
                EmitPropAccessMember(node);
            }
        }

        private void EmitPropAccessMember(PropAccessNode node)
        {
            if (_il == null) return;

            // Load the field/property
            if (node.Property is IdentifierNode propIdent)
            {
                // The object's static type — not _currentType — owns the field.
                Mono.Cecil.TypeReference? ownerType = null;
                if (node.Object is SelfNode && _currentType != null)
                {
                    ownerType = _currentType;
                }
                else if (node.Object is IExpressionNode objExpr && objExpr.ResultType != null)
                {
                    ownerType = ResolveTypeByFQN(objExpr.ResultType.FullyQualifiedName, objExpr.ResultType.TypeKind);
                }
                EmitFieldOrPropertyLoad(propIdent, ownerType);
            }
            else if (node.Property is FuncCallNode funcCall)
            {
                // Method call on instance - the object is already on stack
                foreach (var arg in funcCall.Args)
                {
                    EmitExpression(arg.Value);
                }

                MethodReference? methodRef;
                if (node.Object is SuperNode && _currentType?.BaseType != null)
                {
                    // `super.foo()` resolves on the base type and is called non-virtually so
                    // the inherited implementation runs even from inside an override.
                    methodRef = ResolveMethodOnType(_currentType.BaseType, funcCall);
                }
                else
                {
                    methodRef = ResolveMethodReference(funcCall);
                }

                if (methodRef != null)
                {
                    var op = node.Object is SuperNode ? OpCodes.Call : OpCodes.Callvirt;
                    _il.Emit(op, methodRef);
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

            if (propAccess.Property is not IdentifierNode propIdent)
            {
                return;
            }

            // Resolve the owning type from the object's static type — not _currentType,
            // which would only be right for `self.x = ...` and break `other.x = ...`.
            Mono.Cecil.TypeDefinition? ownerDef = null;
            if (propAccess.Object is SelfNode)
            {
                ownerDef = _currentType;
            }
            else if (propAccess.Object is IExpressionNode objExpr && objExpr.ResultType != null)
            {
                ownerDef = ResolveTypeByFQN(objExpr.ResultType.FullyQualifiedName, objExpr.ResultType.TypeKind).Resolve();
            }
            ownerDef ??= _currentType;
            if (ownerDef == null) return;

            var field = ownerDef.Fields.FirstOrDefault(f => f.Name == propIdent.Value || f.Name == propIdent.ILValue);
            if (field != null)
            {
                var fieldRef = field.Module == _module ? (FieldReference)field : _module.ImportReference(field);
                _il.Emit(OpCodes.Stfld, fieldRef);
                return;
            }

            // Fall back to a property setter for types that only expose properties.
            var prop = ownerDef.Properties.FirstOrDefault(p => p.Name == propIdent.Value || p.Name == propIdent.ILValue);
            if (prop?.SetMethod != null)
            {
                var setter = _module.ImportReference(prop.SetMethod);
                _il.Emit(prop.SetMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, setter);
            }
        }

        private void EmitFieldOrPropertyLoad(IdentifierNode ident, Mono.Cecil.TypeReference? ownerType = null)
        {
            if (_il == null) return;

            // Resolve the owning type: prefer the explicitly supplied one (e.g. the
            // ResultType of the PropAccess.Object), fall back to the currently emitting
            // type for bare-identifier `self`-style loads.
            Mono.Cecil.TypeDefinition? ownerDef = null;
            if (ownerType != null)
            {
                ownerDef = ownerType.Resolve();
            }
            ownerDef ??= _currentType;
            if (ownerDef == null) return;

            var field = ownerDef.Fields.FirstOrDefault(f => f.Name == ident.Value || f.Name == ident.ILValue);
            if (field != null)
            {
                var fieldRef = field.Module == _module ? (FieldReference)field : _module.ImportReference(field);
                _il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldRef);
                return;
            }

            // Try a property getter (e.g. cross-assembly Builtins types expose props, not fields).
            var prop = ownerDef.Properties.FirstOrDefault(p => p.Name == ident.Value || p.Name == ident.ILValue);
            if (prop?.GetMethod != null)
            {
                var getter = _module.ImportReference(prop.GetMethod);
                _il.Emit(prop.GetMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, getter);
            }
        }

        private MethodReference? EmitScopeResolution(ScopeResolutionNode node)
        {
            if (_il == null) return null;

            if (node.Property is EnumCaseAccessNode caseAccess)
            {
                EmitEnumCaseAccess(caseAccess);
                return null;
            }
            else if (node.Property is FuncCallNode funcCall)
            {
                // Static method call
                foreach (var arg in funcCall.Args)
                {
                    EmitExpression(arg.Value);
                }

                // First try standard resolution (works when typechecker set ResultType)
                var methodRef = ResolveMethodReference(funcCall);
                // If that fails, resolve via the scope type (e.g., Console::writeLine → System.Console.WriteLine)
                if (methodRef == null)
                {
                    var scopeTypeSymbol = _table.FindTypeBy(node.Root, node.Scope.Value, null);
                    if (scopeTypeSymbol != null)
                    {
                        var scopeFqn = scopeTypeSymbol.FullyQualifiedName;
                        var scopeType = ResolveTypeByFQN(scopeFqn);
                        var resolved = scopeType.Resolve();
                        if (resolved != null)
                        {
                            var csharpName = Shared.Utils.IonaToCSharpName(funcCall.Target.Value);
                            // Try matching by argument types first
                            var argTypeNames = funcCall.Args.Select(a => InferNetTypeName(a.Value)).ToList();
                            MethodDefinition? method = null;
                            if (argTypeNames.All(t => t != null))
                            {
                                method = resolved.Methods.FirstOrDefault(m =>
                                    m.Name == csharpName &&
                                    m.Parameters.Count == funcCall.Args.Count &&
                                    m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(argTypeNames!));
                            }
                            if (method == null)
                            {
                                // Fall back to matching by name and parameter count only
                                method = resolved.Methods.FirstOrDefault(
                                    m => m.Name == csharpName && m.Parameters.Count == funcCall.Args.Count);
                            }
                            if (method != null)
                            {
                                methodRef = _module.ImportReference(method);
                            }
                        }
                    }
                }

                if (methodRef != null)
                {
                    _il.Emit(OpCodes.Call, methodRef);
                }
                return methodRef;
            }
            else if (node.Property is IdentifierNode ident)
            {
                // Static field access
                EmitIdentifier(ident);
            }
            return null;
        }

        private void EmitArrayAccess(ArrayAccessNode node)
        {
            if (_il == null) return;

            // Load array reference onto stack
            EmitExpression(node.Array);
            // Load index onto stack
            EmitExpression(node.Index);
            // Emit ldelem for the appropriate element type
            _il.Emit(OpCodes.Ldelem_Ref);
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

        private void EmitLdloca(VariableDefinition local)
        {
            int idx = local.Index;
            if (idx <= 255)
                _il!.Emit(OpCodes.Ldloca_S, local);
            else
                _il!.Emit(OpCodes.Ldloca, local);
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
            var baseType = ResolveTypeByFQN(node.FullyQualifiedName, node.TypeKind);

            // For optional value types (Int?, Bool?, etc.), wrap in Nullable<T>
            if (node.IsOptional && baseType.IsValueType)
            {
                var nullableTypeDef = _module.ImportReference(typeof(System.Nullable<>));
                var nullableOfT = new GenericInstanceType(nullableTypeDef);
                nullableOfT.GenericArguments.Add(baseType);
                return nullableOfT;
            }

            // Reference type optionals don't need wrapping — they're inherently nullable.
            // NullableAttribute metadata is added separately by the caller.
            return baseType;
        }

        private Mono.Cecil.TypeReference? TryResolveCecilReturnType(INode value)
        {
            if (value is not ScopeResolutionNode scope || scope.Property is not FuncCallNode funcCall)
                return null;

            var scopeTypeSymbol = _table.FindTypeBy(scope.Root, scope.Scope.Value, null);
            if (scopeTypeSymbol == null) return null;

            var scopeType = ResolveTypeByFQN(scopeTypeSymbol.FullyQualifiedName);
            var resolved = scopeType?.Resolve();
            if (resolved == null) return null;

            var csharpName = Shared.Utils.IonaToCSharpName(funcCall.Target.Value);
            var method = resolved.Methods.FirstOrDefault(
                m => m.Name == csharpName && m.Parameters.Count == funcCall.Args.Count);

            if (method != null)
                return _module.ImportReference(method.ReturnType);

            return null;
        }

        private string? InferNetTypeName(IExpressionNode expr)
        {
            if (expr.ResultType != null)
            {
                // Convert Iona builtin names to .NET type names
                var fqn = expr.ResultType.FullyQualifiedName;
                return IonaToNetTypeName(fqn);
            }
            if (expr is LiteralNode lit)
            {
                return lit.LiteralType switch
                {
                    LiteralType.String => "System.String",
                    LiteralType.Integer => "System.Int32",
                    LiteralType.Double => "System.Double",
                    LiteralType.Float => "System.Single",
                    LiteralType.Boolean => "System.Boolean",
                    LiteralType.Char => "System.Char",
                    _ => null,
                };
            }
            // For array access, infer the element type from the local variable
            if (expr is ArrayAccessNode access && access.Array is IdentifierNode ident
                && _locals.TryGetValue(ident.Value, out var localDef))
            {
                var localType = localDef.VariableType;
                if (localType is Mono.Cecil.ArrayType arrayType)
                    return arrayType.ElementType.FullName;
            }
            return null;
        }

        private static string IonaToNetTypeName(string fqn)
        {
            return fqn switch
            {
                "Iona.Builtins.Bool" => "System.Boolean",
                "Iona.Builtins.String" => "System.String",
                "Iona.Builtins.Int8" => "System.SByte",
                "Iona.Builtins.Int16" => "System.Int16",
                "Iona.Builtins.Int32" => "System.Int32",
                "Iona.Builtins.Int64" => "System.Int64",
                "Iona.Builtins.UInt8" => "System.Byte",
                "Iona.Builtins.UInt16" => "System.UInt16",
                "Iona.Builtins.UInt32" => "System.UInt32",
                "Iona.Builtins.UInt64" => "System.UInt64",
                "Iona.Builtins.Float" => "System.Single",
                "Iona.Builtins.Double" => "System.Double",
                "Iona.Builtins.Char" => "System.Char",
                "Iona.Builtins.Void" => "System.Void",
                _ => fqn,
            };
        }

        internal Mono.Cecil.TypeReference ResolveTypeByFQN(string fqn, Kind kind = Kind.Unknown)
        {
            // Map Iona builtins (full or bare name) to their System equivalents.
            var builtinName = fqn.Contains('.') ? fqn : $"Iona.Builtins.{fqn}";
            switch (builtinName)
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

            // Check types already in the module — match on full name first, then bare name so
            // that an unresolved `Animal` (no namespace) still finds `App.Animal` in our module.
            var localType = _module.Types.FirstOrDefault(t => $"{t.Namespace}.{t.Name}" == fqn);
            if (localType == null && !fqn.Contains('.'))
            {
                localType = _module.Types.FirstOrDefault(t => t.Name == fqn);
            }
            if (localType != null) return localType;

            // Check well-known System types
            var systemType = System.Type.GetType(fqn);
            if (systemType != null) return _module.ImportReference(systemType);

            // Search all loaded assemblies (for types like System.Console in the System.Console assembly)
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var found = asm.GetType(fqn);
                    if (found != null) return _module.ImportReference(found);
                }
                catch { }
            }

            // Fallback: create a type reference
            var lastDot = fqn.LastIndexOf('.');
            var ns = lastDot >= 0 ? fqn[..lastDot] : "";
            var name = lastDot >= 0 ? fqn[(lastDot + 1)..] : fqn;
            var isValueType = kind == Kind.Struct || kind == Kind.Enum;

            return new Mono.Cecil.TypeReference(ns, name, _module, _module) { IsValueType = isValueType };
        }

        // -------------------------------------------------------------------
        //  Nullable attribute helpers
        // -------------------------------------------------------------------

        private void EnsureNullableAttribute()
        {
            if (_nullableAttrType != null) return;

            var attrBaseRef = _module.ImportReference(typeof(System.Attribute));
            var attrBaseCtor = _module.ImportReference(
                typeof(System.Attribute).GetConstructor(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null, System.Type.EmptyTypes, null));
            var byteRef = _module.ImportReference(typeof(byte));
            var byteArrayRef = _module.ImportReference(typeof(byte[]));
            var voidRef = _module.ImportReference(typeof(void));

            _nullableAttrType = new TypeDefinition(
                "System.Runtime.CompilerServices",
                "NullableAttribute",
                TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                attrBaseRef);

            // public readonly byte[] NullableFlags;
            var flagsField = new FieldDefinition(
                "NullableFlags",
                FieldAttributes.Public | FieldAttributes.InitOnly,
                byteArrayRef);
            _nullableAttrType.Fields.Add(flagsField);

            // public NullableAttribute(byte flag) { NullableFlags = new byte[] { flag }; }
            _nullableAttrCtorByte = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig |
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                voidRef);
            _nullableAttrCtorByte.Parameters.Add(
                new Mono.Cecil.ParameterDefinition("flag", ParameterAttributes.None, byteRef));
            _nullableAttrCtorByte.Body = new Mono.Cecil.Cil.MethodBody(_nullableAttrCtorByte);
            var il = _nullableAttrCtorByte.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, attrBaseCtor);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Newarr, byteRef);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stelem_I1);
            il.Emit(OpCodes.Stfld, flagsField);
            il.Emit(OpCodes.Ret);
            _nullableAttrType.Methods.Add(_nullableAttrCtorByte);

            _module.Types.Add(_nullableAttrType);
        }

        private CustomAttribute MakeNullableAttribute(byte flag)
        {
            EnsureNullableAttribute();
            var attr = new CustomAttribute(_nullableAttrCtorByte);
            attr.ConstructorArguments.Add(
                new CustomAttributeArgument(_module.ImportReference(typeof(byte)), flag));
            return attr;
        }

        private void ApplyNullableAttribute(ICustomAttributeProvider target, TypeReferenceNode typeNode, Mono.Cecil.TypeReference resolvedType)
        {
            // Value types use Nullable<T> wrapping — no attribute needed
            if (resolvedType.IsValueType) return;

            // Reference types: mark nullable (2) or not-nullable (1)
            // Both optional (?) and implicitly unwrapped optional (!) are nullable at IL level
            byte flag = (typeNode.IsOptional || typeNode.IsImplicitlyUnwrapped) ? (byte)2 : (byte)1;
            target.CustomAttributes.Add(MakeNullableAttribute(flag));
        }

        // Look up a method by name + arg count on a specific Cecil type (and its base chain).
        // Used for `super.foo(...)` where we want the inherited slot, not the override on Self.
        private MethodReference? ResolveMethodOnType(TypeReference type, FuncCallNode node)
        {
            var csharpName = Shared.Utils.IonaToCSharpName(node.Target.ILValue);
            var current = type;
            while (current != null)
            {
                var def = current.Resolve();
                if (def == null) { return null; }
                var method = def.Methods.FirstOrDefault(m =>
                    m.Name == csharpName && m.Parameters.Count == node.Args.Count);
                if (method != null)
                {
                    return _module.ImportReference(method);
                }
                current = def.BaseType;
            }
            return null;
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

            var freeFunc = _table.CheckIfFuncExists(node.Root, null, node);
            if (freeFunc.IsSuccess)
            {
                var fn = freeFunc.Unwrapped();
                if (!string.IsNullOrEmpty(fn.CsharpOwnerFqn))
                {
                    var ownerDef = ResolveTypeByFQN(fn.CsharpOwnerFqn!).Resolve();
                    var external = ownerDef?.Methods.FirstOrDefault(m =>
                        m.IsStatic && m.Name == fn.CsharpName && m.Parameters.Count == node.Args.Count);
                    if (external != null)
                    {
                        return _module.ImportReference(external);
                    }
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
        //  Async method emission
        // -------------------------------------------------------------------

        private int CountAwaits(INode node)
        {
            int count = node is AwaitExpressionNode ? 1 : 0;
            switch (node)
            {
                case AwaitExpressionNode awaitExpr:
                    count += CountAwaits(awaitExpr.Expression);
                    break;
                case BlockNode block:
                    foreach (var child in block.Children) count += CountAwaits(child);
                    break;
                case VariableNode { Value: not null } variable:
                    count += CountAwaits(variable.Value);
                    break;
                case ReturnNode { Value: not null } ret:
                    count += CountAwaits(ret.Value);
                    break;
                case AssignmentNode assignment:
                    count += CountAwaits(assignment.Value);
                    break;
                case BinaryExpressionNode binary:
                    count += CountAwaits(binary.Left);
                    count += CountAwaits(binary.Right);
                    break;
                case IfNode ifNode:
                    if (ifNode.Condition != null) count += CountAwaits(ifNode.Condition);
                    if (ifNode.Body != null) count += CountAwaits(ifNode.Body);
                    foreach (var clause in ifNode.ElseClauses)
                        if (clause.Body != null) count += CountAwaits(clause.Body);
                    break;
                case WhileNode whileNode:
                    if (whileNode.Condition != null) count += CountAwaits(whileNode.Condition);
                    if (whileNode.Body != null) count += CountAwaits(whileNode.Body);
                    break;
                case ForNode forNode:
                    if (forNode.Body != null) count += CountAwaits(forNode.Body);
                    break;
                case FuncCallNode funcCall:
                    foreach (var arg in funcCall.Args) count += CountAwaits(arg.Value);
                    break;
                case ScopeResolutionNode scope:
                    count += CountAwaits(scope.Property);
                    break;
            }
            return count;
        }

        private MethodReference MakeBuilderMethodRef(TypeReference builderTypeRef, string methodName)
        {
            var builderDef = builderTypeRef.Resolve();
            var methodDef = builderDef.Methods.First(m => m.Name == methodName);
            if (builderTypeRef is GenericInstanceType bGeneric)
                return _module.ImportReference(methodDef).MakeHostInstanceGeneric(bGeneric);
            return _module.ImportReference(methodDef);
        }

        private void EmitAsyncMethod(
            FuncNode node,
            TypeDefinition ownerType,
            MethodAttributes methodAttrs,
            bool isInstance)
        {
            if (node.ReturnType is not TypeReferenceNode retTypeRef) return;

            var innerReturnType = ResolveTypeReference(retTypeRef);
            bool isVoid = retTypeRef.FullyQualifiedName == "Iona.Builtins.Void";

            // Outer return type: Task or Task<T>
            TypeReference outerReturnType;
            if (isVoid)
                outerReturnType = _module.ImportReference(typeof(System.Threading.Tasks.Task));
            else
            {
                var taskOpen = _module.ImportReference(typeof(System.Threading.Tasks.Task<>));
                var taskOfT = new GenericInstanceType(taskOpen);
                taskOfT.GenericArguments.Add(innerReturnType);
                outerReturnType = taskOfT;
            }

            // Builder type: AsyncTaskMethodBuilder or AsyncTaskMethodBuilder<T>
            TypeReference builderTypeRef;
            if (isVoid)
                builderTypeRef = _module.ImportReference(
                    typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder));
            else
            {
                var builderOpen = _module.ImportReference(
                    typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<>));
                var builderOfT = new GenericInstanceType(builderOpen);
                builderOfT.GenericArguments.Add(innerReturnType);
                builderTypeRef = builderOfT;
            }

            int awaitCount = node.Body != null ? CountAwaits(node.Body) : 0;

            // --- State machine type ---
            var smTypeName = $"<{Shared.Utils.IonaToCSharpName(node.Name)}>d__async";
            var smType = new TypeDefinition(
                "",
                smTypeName,
                TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                _module.ImportReference(typeof(object)));

            var iAsyncSmType = _module.ImportReference(
                typeof(System.Runtime.CompilerServices.IAsyncStateMachine));
            smType.Interfaces.Add(new InterfaceImplementation(iAsyncSmType));

            var stateField = new FieldDefinition("<>1__state", FieldAttributes.Public,
                _module.ImportReference(typeof(int)));
            smType.Fields.Add(stateField);

            var builderField = new FieldDefinition("<>t__builder", FieldAttributes.Public, builderTypeRef);
            smType.Fields.Add(builderField);

            FieldDefinition? thisField = null;
            if (isInstance)
            {
                thisField = new FieldDefinition("<>4__this", FieldAttributes.Public, ownerType);
                smType.Fields.Add(thisField);
            }

            var paramFields = new Dictionary<string, FieldDefinition>();
            foreach (var param in node.Parameters)
            {
                var paramType = ResolveTypeReference(param.TypeNode);
                var pField = new FieldDefinition(param.Name, FieldAttributes.Public, paramType);
                smType.Fields.Add(pField);
                paramFields[param.Name] = pField;
            }

            ownerType.NestedTypes.Add(smType);

            // --- State machine .ctor ---
            var smCtor = new MethodDefinition(".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig |
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                _module.ImportReference(typeof(void)));
            smCtor.Body = new Mono.Cecil.Cil.MethodBody(smCtor);
            var ctorIl = smCtor.Body.GetILProcessor();
            var objectCtor = _module.ImportReference(typeof(object).GetConstructor(System.Type.EmptyTypes));
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, objectCtor);
            ctorIl.Emit(OpCodes.Ret);
            smType.Methods.Add(smCtor);

            // --- MoveNext ---
            var moveNext = new MethodDefinition("MoveNext",
                MethodAttributes.Private | MethodAttributes.Final |
                MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                _module.ImportReference(typeof(void)));
            moveNext.Body = new Mono.Cecil.Cil.MethodBody(moveNext);

            var iAsyncSmDef = iAsyncSmType.Resolve();
            moveNext.Overrides.Add(_module.ImportReference(
                iAsyncSmDef.Methods.First(m => m.Name == "MoveNext")));
            smType.Methods.Add(moveNext);

            // --- SetStateMachine ---
            var setStateMachine = new MethodDefinition("SetStateMachine",
                MethodAttributes.Private | MethodAttributes.Final |
                MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                _module.ImportReference(typeof(void)));
            setStateMachine.Parameters.Add(new Mono.Cecil.ParameterDefinition(
                "stateMachine", ParameterAttributes.None, iAsyncSmType));
            setStateMachine.Body = new Mono.Cecil.Cil.MethodBody(setStateMachine);
            setStateMachine.Overrides.Add(_module.ImportReference(
                iAsyncSmDef.Methods.First(m => m.Name == "SetStateMachine")));

            var ssmIl = setStateMachine.Body.GetILProcessor();
            ssmIl.Emit(OpCodes.Ldarg_0);
            ssmIl.Emit(OpCodes.Ldflda, builderField);
            ssmIl.Emit(OpCodes.Ldarg_1);
            ssmIl.Emit(OpCodes.Call, MakeBuilderMethodRef(builderTypeRef, "SetStateMachine"));
            ssmIl.Emit(OpCodes.Ret);
            smType.Methods.Add(setStateMachine);

            // --- Emit MoveNext body ---
            EmitMoveNextBody(moveNext, node, smType, stateField, builderField,
                builderTypeRef, thisField, paramFields, awaitCount, innerReturnType, isVoid);

            // --- Stub method ---
            var stubMethod = new MethodDefinition(
                Shared.Utils.IonaToCSharpName(node.Name), methodAttrs, outerReturnType);
            BuildMethodParams(stubMethod, node.Parameters, isInstance);
            stubMethod.Body = new Mono.Cecil.Cil.MethodBody(stubMethod);
            EmitAsyncStub(stubMethod, smType, smCtor, stateField, builderField,
                builderTypeRef, thisField, paramFields, isInstance);
            stubMethod.Body.InitLocals = true;
            ownerType.Methods.Add(stubMethod);

            if (node.Name == "main" && !isInstance)
                EntryPoint = stubMethod;
        }

        private void EmitAsyncStub(
            MethodDefinition stub, TypeDefinition smType, MethodDefinition smCtor,
            FieldDefinition stateField, FieldDefinition builderField,
            TypeReference builderTypeRef, FieldDefinition? thisField,
            Dictionary<string, FieldDefinition> paramFields, bool isInstance)
        {
            var il = stub.Body.GetILProcessor();
            var smLocal = new VariableDefinition(smType);
            stub.Body.Variables.Add(smLocal);

            // var sm = new StateMachine();
            il.Emit(OpCodes.Newobj, smCtor);
            il.Emit(OpCodes.Stloc, smLocal);

            // sm.<>t__builder = AsyncTaskMethodBuilder<T>.Create();
            il.Emit(OpCodes.Ldloc, smLocal);
            il.Emit(OpCodes.Call, MakeBuilderMethodRef(builderTypeRef, "Create"));
            il.Emit(OpCodes.Stfld, builderField);

            // sm.<>1__state = -1;
            il.Emit(OpCodes.Ldloc, smLocal);
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Stfld, stateField);

            // Copy parameters to sm fields
            int paramOffset = isInstance ? 1 : 0;
            int paramIndex = 0;
            foreach (var (name, field) in paramFields)
            {
                il.Emit(OpCodes.Ldloc, smLocal);
                il.Emit(OpCodes.Ldarg, paramIndex + paramOffset);
                il.Emit(OpCodes.Stfld, field);
                paramIndex++;
            }

            // Copy 'this' if instance
            if (isInstance && thisField != null)
            {
                il.Emit(OpCodes.Ldloc, smLocal);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Stfld, thisField);
            }

            // sm.<>t__builder.Start(ref sm);
            il.Emit(OpCodes.Ldloc, smLocal);
            il.Emit(OpCodes.Ldflda, builderField);
            il.Emit(OpCodes.Ldloca, smLocal);
            var startRef = MakeBuilderMethodRef(builderTypeRef, "Start");
            var startGeneric = new GenericInstanceMethod(startRef);
            startGeneric.GenericArguments.Add(smType);
            il.Emit(OpCodes.Call, startGeneric);

            // return sm.<>t__builder.Task;
            il.Emit(OpCodes.Ldloc, smLocal);
            il.Emit(OpCodes.Ldflda, builderField);
            var builderDef = builderTypeRef.Resolve();
            var taskPropDef = builderDef.Properties.First(p => p.Name == "Task");
            MethodReference taskGetterRef;
            if (builderTypeRef is GenericInstanceType bGeneric)
                taskGetterRef = _module.ImportReference(taskPropDef.GetMethod).MakeHostInstanceGeneric(bGeneric);
            else
                taskGetterRef = _module.ImportReference(taskPropDef.GetMethod);
            il.Emit(OpCodes.Call, taskGetterRef);
            il.Emit(OpCodes.Ret);
        }

        private void EmitMoveNextBody(
            MethodDefinition moveNext, FuncNode funcNode, TypeDefinition smType,
            FieldDefinition stateField, FieldDefinition builderField,
            TypeReference builderTypeRef, FieldDefinition? thisField,
            Dictionary<string, FieldDefinition> paramFields, int awaitCount,
            TypeReference innerReturnType, bool isVoid)
        {
            // Save context
            var prevMethod = _currentMethod;
            var prevIl = _il;
            var prevIsInstance = _currentMethodIsInstance;
            var prevIsAsync = _isAsyncMoveNext;
            var prevSmType = _stateMachineType;
            var prevStateField = _stateField;
            var prevBuilderField = _builderField;
            var prevBuilderType = _builderType;
            var prevAwaitIndex = _currentAwaitIndex;
            var prevHoistedLocals = new Dictionary<string, FieldDefinition>(_hoistedLocals);
            var prevHoistedParams = new Dictionary<string, FieldDefinition>(_hoistedParams);
            var prevReturnLabel = _asyncReturnLabel;
            var prevSuccessLabel = _asyncSuccessLabel;
            var prevResumeLabels = _asyncResumeLabels;
            var prevAsyncIsVoid = _asyncIsVoid;
            var prevResultLocal = _asyncResultLocal;
            var prevThisField = _asyncThisField;
            var prevLocals = new Dictionary<string, VariableDefinition>(_locals);
            var prevParamIndices = new Dictionary<string, int>(_parameterIndices);

            _currentMethod = moveNext;
            _il = moveNext.Body.GetILProcessor();
            _currentMethodIsInstance = true;
            _isAsyncMoveNext = true;
            _stateMachineType = smType;
            _stateField = stateField;
            _builderField = builderField;
            _builderType = builderTypeRef;
            _currentAwaitIndex = 0;
            _hoistedLocals.Clear();
            _hoistedParams.Clear();
            foreach (var (name, field) in paramFields)
                _hoistedParams[name] = field;
            _asyncIsVoid = isVoid;
            _asyncThisField = thisField;
            _locals.Clear();
            _parameterIndices.Clear();

            // MoveNext locals
            var numLocal = new VariableDefinition(_module.ImportReference(typeof(int)));
            moveNext.Body.Variables.Add(numLocal);

            var exceptionLocal = new VariableDefinition(_module.ImportReference(typeof(Exception)));
            moveNext.Body.Variables.Add(exceptionLocal);

            VariableDefinition? resultLocal = null;
            if (!isVoid)
            {
                resultLocal = new VariableDefinition(innerReturnType);
                moveNext.Body.Variables.Add(resultLocal);
            }
            _asyncResultLocal = resultLocal;

            // Resume labels
            _asyncResumeLabels = new Instruction[awaitCount];
            for (int i = 0; i < awaitCount; i++)
                _asyncResumeLabels[i] = _il.Create(OpCodes.Nop);

            // Return and success labels (instructions created now, appended later)
            var successLabel = _il.Create(OpCodes.Nop);
            var returnLabel = _il.Create(OpCodes.Ret);
            _asyncSuccessLabel = successLabel;
            _asyncReturnLabel = returnLabel;

            // --- Load and cache state ---
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, stateField);
            EmitStloc(numLocal);

            // --- Try block start ---
            var tryStart = _il.Create(OpCodes.Nop);
            _il.Append(tryStart);

            // State dispatch switch
            if (awaitCount > 0)
            {
                EmitLdloc(numLocal);
                _il.Emit(OpCodes.Switch, _asyncResumeLabels);
            }

            // --- Body ---
            if (funcNode.Body != null)
                EmitBlock(funcNode.Body);

            // Fall-through: leave to success
            _il.Emit(OpCodes.Leave, successLabel);

            // --- Catch block ---
            var catchStart = _il.Create(OpCodes.Nop);
            _il.Append(catchStart);
            EmitStloc(exceptionLocal);

            // state = -2
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldc_I4, -2);
            _il.Emit(OpCodes.Stfld, stateField);

            // builder.SetException(exception)
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldflda, builderField);
            EmitLdloc(exceptionLocal);
            _il.Emit(OpCodes.Call, MakeBuilderMethodRef(builderTypeRef, "SetException"));
            _il.Emit(OpCodes.Leave, returnLabel);

            // --- After catch: success exit ---
            _il.Append(successLabel);

            // state = -2
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldc_I4, -2);
            _il.Emit(OpCodes.Stfld, stateField);

            // builder.SetResult([result])
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldflda, builderField);
            if (!isVoid && resultLocal != null)
                EmitLdloc(resultLocal);
            _il.Emit(OpCodes.Call, MakeBuilderMethodRef(builderTypeRef, "SetResult"));

            // Final return
            _il.Append(returnLabel);

            // Register exception handler
            moveNext.Body.InitLocals = true;
            moveNext.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = tryStart,
                TryEnd = catchStart,
                HandlerStart = catchStart,
                HandlerEnd = successLabel,
                CatchType = _module.ImportReference(typeof(Exception))
            });

            // Restore context
            _currentMethod = prevMethod;
            _il = prevIl;
            _currentMethodIsInstance = prevIsInstance;
            _isAsyncMoveNext = prevIsAsync;
            _stateMachineType = prevSmType;
            _stateField = prevStateField;
            _builderField = prevBuilderField;
            _builderType = prevBuilderType;
            _currentAwaitIndex = prevAwaitIndex;
            _hoistedLocals.Clear();
            foreach (var (k, v) in prevHoistedLocals) _hoistedLocals[k] = v;
            _hoistedParams.Clear();
            foreach (var (k, v) in prevHoistedParams) _hoistedParams[k] = v;
            _asyncReturnLabel = prevReturnLabel;
            _asyncSuccessLabel = prevSuccessLabel;
            _asyncResumeLabels = prevResumeLabels;
            _asyncIsVoid = prevAsyncIsVoid;
            _asyncResultLocal = prevResultLocal;
            _asyncThisField = prevThisField;
            _locals.Clear();
            foreach (var (k, v) in prevLocals) _locals[k] = v;
            _parameterIndices.Clear();
            foreach (var (k, v) in prevParamIndices) _parameterIndices[k] = v;
        }

        private void EmitAwaitExpression(AwaitExpressionNode node)
        {
            if (_il == null || !_isAsyncMoveNext || _asyncResumeLabels == null ||
                _stateField == null || _builderField == null || _builderType == null ||
                _stateMachineType == null || _currentMethod == null || _asyncReturnLabel == null)
                return;

            int awaitIndex = _currentAwaitIndex++;

            // Determine the inner type (what the await resolves to)
            TypeReference innerType;
            bool awaitIsVoid;
            var resultFqn = node.ResultType?.FullyQualifiedName ?? "Iona.Builtins.Void";
            if (resultFqn == "Iona.Builtins.Void" || resultFqn == "System.Threading.Tasks.Task")
            {
                innerType = _module.ImportReference(typeof(void));
                awaitIsVoid = true;
            }
            else
            {
                innerType = ResolveTypeReference(node.ResultType!);
                awaitIsVoid = false;
            }

            // Task type
            TypeReference taskType;
            TypeDefinition taskDef;
            if (awaitIsVoid)
            {
                taskType = _module.ImportReference(typeof(System.Threading.Tasks.Task));
                taskDef = taskType.Resolve();
            }
            else
            {
                var taskOpen = _module.ImportReference(typeof(System.Threading.Tasks.Task<>));
                var taskOfT = new GenericInstanceType(taskOpen);
                taskOfT.GenericArguments.Add(innerType);
                taskType = taskOfT;
                taskDef = taskOpen.Resolve();
            }

            // Awaiter type
            TypeReference awaiterType;
            TypeDefinition awaiterDef;
            if (awaitIsVoid)
            {
                awaiterType = _module.ImportReference(typeof(System.Runtime.CompilerServices.TaskAwaiter));
                awaiterDef = awaiterType.Resolve();
            }
            else
            {
                var awaiterOpen = _module.ImportReference(typeof(System.Runtime.CompilerServices.TaskAwaiter<>));
                var awaiterOfT = new GenericInstanceType(awaiterOpen);
                awaiterOfT.GenericArguments.Add(innerType);
                awaiterType = awaiterOfT;
                awaiterDef = awaiterOpen.Resolve();
            }

            // Awaiter field on state machine
            var awaiterField = new FieldDefinition(
                $"<>u__{awaitIndex + 1}", FieldAttributes.Private, awaiterType);
            _stateMachineType.Fields.Add(awaiterField);

            // Awaiter local in MoveNext
            var awaiterLocal = new VariableDefinition(awaiterType);
            _currentMethod.Body.Variables.Add(awaiterLocal);

            var resumeLabel = _asyncResumeLabels[awaitIndex];
            var completedLabel = _il.Create(OpCodes.Nop);

            // --- Emit inner expression (pushes Task<T> on stack) ---
            EmitExpression(node.Expression);

            // --- GetAwaiter() ---
            MethodReference getAwaiterRef;
            var getAwaiterDef = taskDef.Methods.First(m => m.Name == "GetAwaiter" && m.Parameters.Count == 0);
            if (taskType is GenericInstanceType taskGeneric)
                getAwaiterRef = _module.ImportReference(getAwaiterDef).MakeHostInstanceGeneric(taskGeneric);
            else
                getAwaiterRef = _module.ImportReference(getAwaiterDef);
            _il.Emit(OpCodes.Callvirt, getAwaiterRef);
            EmitStloc(awaiterLocal);

            // --- IsCompleted ---
            EmitLdloca(awaiterLocal);
            var isCompletedDef = awaiterDef.Properties.First(p => p.Name == "IsCompleted").GetMethod;
            MethodReference isCompletedRef;
            if (awaiterType is GenericInstanceType awaiterGeneric)
                isCompletedRef = _module.ImportReference(isCompletedDef).MakeHostInstanceGeneric(awaiterGeneric);
            else
                isCompletedRef = _module.ImportReference(isCompletedDef);
            _il.Emit(OpCodes.Call, isCompletedRef);
            _il.Emit(OpCodes.Brtrue, completedLabel);

            // --- Suspend: not completed ---
            // state = awaitIndex
            _il.Emit(OpCodes.Ldarg_0);
            EmitLdcI4(awaitIndex);
            _il.Emit(OpCodes.Stfld, _stateField);

            // Save awaiter to field
            _il.Emit(OpCodes.Ldarg_0);
            EmitLdloc(awaiterLocal);
            _il.Emit(OpCodes.Stfld, awaiterField);

            // builder.AwaitUnsafeOnCompleted(ref awaiter, ref this)
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldflda, _builderField);
            EmitLdloca(awaiterLocal);

            // Need ref to state machine (this)
            var smLocal = new VariableDefinition(_stateMachineType);
            _currentMethod.Body.Variables.Add(smLocal);
            _il.Emit(OpCodes.Ldarg_0);
            EmitStloc(smLocal);
            EmitLdloca(smLocal);

            var awaitUnsafeRef = MakeBuilderMethodRef(_builderType, "AwaitUnsafeOnCompleted");
            var awaitUnsafeGeneric = new GenericInstanceMethod(awaitUnsafeRef);
            awaitUnsafeGeneric.GenericArguments.Add(awaiterType);
            awaitUnsafeGeneric.GenericArguments.Add(_stateMachineType);
            _il.Emit(OpCodes.Call, awaitUnsafeGeneric);

            // Leave to return (suspend)
            _il.Emit(OpCodes.Leave, _asyncReturnLabel);

            // --- Resume point ---
            _il.Append(resumeLabel);

            // Restore awaiter from field
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, awaiterField);
            EmitStloc(awaiterLocal);

            // Clear saved awaiter
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldflda, awaiterField);
            _il.Emit(OpCodes.Initobj, awaiterType);

            // Reset state to -1
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldc_I4_M1);
            _il.Emit(OpCodes.Stfld, _stateField);

            // --- GetResult ---
            _il.Append(completedLabel);
            EmitLdloca(awaiterLocal);
            var getResultDef = awaiterDef.Methods.First(m => m.Name == "GetResult" && m.Parameters.Count == 0);
            MethodReference getResultRef;
            if (awaiterType is GenericInstanceType awaiterGeneric2)
                getResultRef = _module.ImportReference(getResultDef).MakeHostInstanceGeneric(awaiterGeneric2);
            else
                getResultRef = _module.ImportReference(getResultDef);
            _il.Emit(OpCodes.Call, getResultRef);
            // For typed awaits, result is on the stack. For void, nothing.
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

    internal static class CecilExtensions
    {
        public static MethodReference MakeHostInstanceGeneric(this MethodReference self, GenericInstanceType hostType)
        {
            var reference = new MethodReference(self.Name, self.ReturnType, hostType)
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };
            foreach (var param in self.Parameters)
                reference.Parameters.Add(new Mono.Cecil.ParameterDefinition(param.ParameterType));
            foreach (var gp in self.GenericParameters)
                reference.GenericParameters.Add(new Mono.Cecil.GenericParameter(gp.Name, reference));
            return reference;
        }
    }
}
