using AST.Nodes;
using AST.Types;
using IR.Instructions;

namespace IR;

/// <summary>
/// Walks a type-checked AST (FileNode with resolved types) and lowers it
/// into the IR. This is the AST -> IR translation pass.
///
/// Creates one IrModule per file, with functions lowered into SSA basic blocks.
/// </summary>
public class IrLowering
{
    private IrModule? _currentModule;
    private IrFunction? _currentFunction;
    private IrBasicBlock? _currentBlock;

    /// <summary>
    /// Maps local variable names to their AllocInst results within the current function.
    /// </summary>
    private readonly Dictionary<string, IrValue> _locals = new();

    /// <summary>
    /// Maps parameter names to their SSA values within the current function.
    /// </summary>
    private readonly Dictionary<string, IrValue> _params = new();

    /// <summary>
    /// The @self value for instance methods.
    /// </summary>
    private IrValue? _selfValue;

    /// <summary>
    /// Stack of (continueTarget, breakTarget) blocks for the innermost loop.
    /// Used by break/continue statements to branch to the correct block.
    /// For while loops: continue -> while.cond, break -> while.exit
    /// For for loops: continue -> for.inc, break -> for.exit
    /// </summary>
    private readonly Stack<(IrBasicBlock continueTarget, IrBasicBlock breakTarget)> _loopStack = new();

    /// <summary>
    /// Builds an IrModule from a type-checked FileNode.
    /// </summary>
    public IrModule Build(FileNode file)
    {
        _currentModule = new IrModule(file.Name);

        foreach (var child in file.Children)
        {
            LowerTopLevel(child);
        }

        return _currentModule;
    }

    private void LowerTopLevel(INode node)
    {
        switch (node)
        {
            case ModuleNode moduleNode:
                LowerModule(moduleNode);
                break;
            case ClassNode classNode:
                LowerTypeDecl(classNode);
                break;
            case StructNode structNode:
                LowerTypeDecl(structNode);
                break;
            case EnumNode enumNode:
                LowerEnumDecl(enumNode);
                break;
            case FuncNode funcNode:
                LowerFunction(funcNode, owningType: null);
                break;
            case ImportNode:
                // Imports don't produce IR
                break;
        }
    }

    private void LowerModule(ModuleNode moduleNode)
    {
        // A module node contains children (classes, structs, funcs, etc.)
        foreach (var child in moduleNode.Children)
        {
            LowerTopLevel(child);
        }
    }

    private void LowerTypeDecl(INode typeNode)
    {
        string name;
        BlockNode? body;
        IrTypeDeclKind kind;
        IrType irType;

        if (typeNode is ClassNode classNode)
        {
            name = classNode.FullyQualifiedName ?? classNode.Name;
            body = classNode.Body;
            kind = IrTypeDeclKind.Class;
            irType = new IrClassType(name);
        }
        else if (typeNode is StructNode structNode)
        {
            name = structNode.FullyQualifiedName ?? structNode.Name;
            body = structNode.Body;
            kind = IrTypeDeclKind.Struct;
            irType = new IrStructType(name);
        }
        else
        {
            return;
        }

        var typeDecl = new IrTypeDecl(name, kind);

        // Process the body to find properties, functions, inits
        if (body != null)
        {
            foreach (var member in body.Children)
            {
                switch (member)
                {
                    case PropertyNode prop:
                        var fieldType = IrTypes.FromTypeReference(prop.TypeNode);
                        typeDecl.Fields.Add(new IrFieldDecl(prop.Name, fieldType, isMutable: true));
                        break;

                    case FuncNode funcNode:
                        LowerFunction(funcNode, owningType: irType);
                        break;

                    case InitNode initNode:
                        LowerInit(initNode, owningType: irType);
                        break;
                }
            }
        }

        _currentModule!.AddTypeDeclaration(typeDecl);
    }

    private void LowerEnumDecl(EnumNode enumNode)
    {
        var name = enumNode.FullyQualifiedName ?? enumNode.Name;
        var typeDecl = new IrTypeDecl(name, IrTypeDeclKind.Enum);
        _currentModule!.AddTypeDeclaration(typeDecl);
    }

    private void LowerFunction(FuncNode funcNode, IrType? owningType)
    {
        var returnType = IrTypes.FromTypeReference(funcNode.ReturnType);
        var qualifiedName = BuildFunctionName(funcNode, owningType);

        var func = new IrFunction(qualifiedName, returnType)
        {
            IsInstance = !funcNode.IsStatic && owningType != null,
            IsConstructor = false,
            OwningType = owningType
        };

        BeginFunction(func);

        // Create @self parameter for instance methods
        if (func.IsInstance && owningType != null)
        {
            _selfValue = func.CreateValue(owningType, "self");
        }

        // Create parameters
        foreach (var param in funcNode.Parameters)
        {
            var paramType = IrTypes.FromTypeReference(param.TypeNode);
            var paramValue = func.CreateValue(paramType, param.Name);
            func.Parameters.Add(new IrParameter(param.Name, paramType, paramValue));
            _params[param.Name] = paramValue;
        }

        // Create entry block
        var entryBlock = func.CreateBlock("entry");
        _currentBlock = entryBlock;

        // Lower the function body
        if (funcNode.Body != null)
        {
            LowerBlock(funcNode.Body);
        }

        // Ensure the function is terminated
        EnsureTerminated(returnType);

        EndFunction();
        _currentModule!.AddFunction(func);
    }

    private void LowerInit(InitNode initNode, IrType owningType)
    {
        var qualifiedName = owningType.Name + "..ctor";
        var func = new IrFunction(qualifiedName, IrTypes.Void)
        {
            IsInstance = true,
            IsConstructor = true,
            OwningType = owningType
        };

        BeginFunction(func);

        _selfValue = func.CreateValue(owningType, "self");

        foreach (var param in initNode.Parameters)
        {
            var paramType = IrTypes.FromTypeReference(param.TypeNode);
            var paramValue = func.CreateValue(paramType, param.Name);
            func.Parameters.Add(new IrParameter(param.Name, paramType, paramValue));
            _params[param.Name] = paramValue;
        }

        var entryBlock = func.CreateBlock("entry");
        _currentBlock = entryBlock;

        if (initNode.Body != null)
        {
            LowerBlock(initNode.Body);
        }

        EnsureTerminated(IrTypes.Void);

        EndFunction();
        _currentModule!.AddFunction(func);
    }

    private void LowerBlock(BlockNode block)
    {
        foreach (var child in block.Children)
        {
            LowerStatement(child);
        }
    }

    private void LowerStatement(INode node)
    {
        switch (node)
        {
            case VariableNode varNode:
                LowerVariableDecl(varNode);
                break;

            case AssignmentNode assignNode:
                LowerAssignment(assignNode);
                break;

            case ReturnNode returnNode:
                LowerReturn(returnNode);
                break;

            case FuncCallNode funcCallNode:
                LowerFuncCall(funcCallNode);
                break;

            case MethodCallNode methodCallNode:
                LowerMethodCall(methodCallNode);
                break;

            case InitCallNode initCallNode:
                LowerInitCall(initCallNode);
                break;

            case WhileNode whileNode:
                LowerWhile(whileNode);
                break;

            case IfNode ifNode:
                LowerIf(ifNode);
                break;

            case ForNode forNode:
                LowerFor(forNode);
                break;

            case BreakNode:
                LowerBreak();
                break;

            case ContinueNode:
                LowerContinue();
                break;

            // Nested types and functions within a body are handled by LowerTypeDecl
            case ClassNode classNode:
                LowerTypeDecl(classNode);
                break;

            case StructNode structNode:
                LowerTypeDecl(structNode);
                break;

            case FuncNode funcNode:
                LowerFunction(funcNode, owningType: null);
                break;
        }
    }

    private void LowerVariableDecl(VariableNode varNode)
    {
        var varType = IrTypes.FromTypeReference(varNode.TypeNode);
        var allocResult = _currentFunction!.CreateValue(varType, varNode.Name);
        Emit(new AllocInst(varNode.Name, varType, allocResult));
        _locals[varNode.Name] = allocResult;

        // If the variable has an initializer, store it
        if (varNode.Value != null)
        {
            var initValue = LowerExpression(varNode.Value);
            if (initValue != null)
            {
                Emit(new StoreInst(initValue, allocResult));
            }
        }
    }

    private void LowerAssignment(AssignmentNode assignNode)
    {
        var value = LowerExpression(assignNode.Value);
        if (value == null) return;

        // The target can be a VarAccessNode, PropAccessNode, etc.
        switch (assignNode.Target)
        {
            case VarAccessNode varAccess:
            {
                var varName = varAccess.Name.Value;
                if (_locals.TryGetValue(varName, out var slot))
                {
                    Emit(new StoreInst(value, slot));
                }
                break;
            }

            case PropAccessNode propAccess:
            {
                var receiver = LowerExpression(propAccess.Object);
                if (receiver != null && propAccess.Property is IdentifierNode propId)
                {
                    var propType = IrTypes.FromTypeReference(propAccess.ResultType);
                    Emit(new PropertyAccessInst(
                        receiver, propId.Value, propType,
                        isLoad: false, result: null, storeValue: value));
                }
                break;
            }

            case IdentifierNode idNode:
            {
                if (_locals.TryGetValue(idNode.Value, out var slot))
                {
                    Emit(new StoreInst(value, slot));
                }
                break;
            }
        }
    }

    private void LowerReturn(ReturnNode returnNode)
    {
        if (returnNode.Value != null)
        {
            var retValue = LowerExpression(returnNode.Value);
            Emit(new ReturnInst(retValue));
        }
        else
        {
            Emit(new ReturnInst());
        }
    }

    /// <summary>
    /// Lowers an expression and returns the SSA value it produces.
    /// </summary>
    private IrValue? LowerExpression(INode node)
    {
        switch (node)
        {
            case LiteralNode literal:
                return LowerLiteral(literal);

            case BinaryExpressionNode binary:
                return LowerBinaryExpr(binary);

            case ComparisonExpressionNode comparison:
                return LowerComparisonExpr(comparison);

            case VarAccessNode varAccess:
                return LowerVarAccess(varAccess);

            case ParamAccessNode paramAccess:
                return LowerParamAccess(paramAccess);

            case PropAccessNode propAccess:
                return LowerPropAccess(propAccess);

            case SelfNode:
                return _selfValue;

            case IdentifierNode identifier:
                return LowerIdentifier(identifier);

            case FuncCallNode funcCall:
                return LowerFuncCall(funcCall);

            case MethodCallNode methodCall:
                return LowerMethodCall(methodCall);

            case InitCallNode initCall:
                return LowerInitCall(initCall);

            case IExpressionNode expr:
                // Fallback for other expression types
                var fallbackType = IrTypes.FromTypeReference(expr.ResultType);
                return _currentFunction!.CreateValue(fallbackType);

            default:
                return null;
        }
    }

    private IrValue LowerLiteral(LiteralNode literal)
    {
        var (kind, irType) = literal.LiteralType switch
        {
            LiteralType.Integer => (ConstantKind.Integer, (IrType)IrTypes.I32),
            LiteralType.Float => (ConstantKind.Float, (IrType)IrTypes.F32),
            LiteralType.Double => (ConstantKind.Double, (IrType)IrTypes.F64),
            LiteralType.Boolean => (ConstantKind.Bool, (IrType)IrTypes.Bool),
            LiteralType.String => (ConstantKind.String, (IrType)IrTypes.String),
            LiteralType.Char => (ConstantKind.Char, (IrType)IrTypes.Char),
            LiteralType.Null => (ConstantKind.Null, (IrType)IrTypes.Void),
            _ => (ConstantKind.Integer, (IrType)IrTypes.I32)
        };

        // If the literal has a resolved type, prefer that
        if (literal.ResultType != null)
        {
            irType = IrTypes.FromTypeReference(literal.ResultType);
        }

        var result = _currentFunction!.CreateValue(irType);
        Emit(new ConstantInst(kind, literal.Value, irType, result));
        return result;
    }

    private IrValue LowerBinaryExpr(BinaryExpressionNode binary)
    {
        var left = LowerExpression(binary.Left);
        var right = LowerExpression(binary.Right);

        if (left == null || right == null)
        {
            var fallbackType = IrTypes.FromTypeReference(binary.ResultType);
            return _currentFunction!.CreateValue(fallbackType);
        }

        // Check if this is actually a comparison disguised as a binary op
        var op = binary.Operation switch
        {
            BinaryOperation.Add => BinaryOp.Add,
            BinaryOperation.Subtract => BinaryOp.Sub,
            BinaryOperation.Multiply => BinaryOp.Mul,
            BinaryOperation.Divide => BinaryOp.Div,
            BinaryOperation.Mod => BinaryOp.Mod,
            BinaryOperation.And => BinaryOp.And,
            BinaryOperation.Or => BinaryOp.Or,
            // Comparisons embedded in BinaryOperation
            BinaryOperation.Equal => (BinaryOp?)null,
            BinaryOperation.NotEqual => null,
            BinaryOperation.LessThan => null,
            BinaryOperation.LessThanOrEqual => null,
            BinaryOperation.GreaterThan => null,
            BinaryOperation.GreaterThanOrEqual => null,
            _ => BinaryOp.Add
        };

        if (op == null)
        {
            // This is a comparison operation
            var cmpOp = binary.Operation switch
            {
                BinaryOperation.Equal => CompareOp.Equal,
                BinaryOperation.NotEqual => CompareOp.NotEqual,
                BinaryOperation.LessThan => CompareOp.LessThan,
                BinaryOperation.LessThanOrEqual => CompareOp.LessThanOrEqual,
                BinaryOperation.GreaterThan => CompareOp.GreaterThan,
                BinaryOperation.GreaterThanOrEqual => CompareOp.GreaterThanOrEqual,
                _ => CompareOp.Equal
            };

            var cmpResult = _currentFunction!.CreateValue(IrTypes.Bool);
            Emit(new CompareInst(cmpOp, left, right, cmpResult));
            return cmpResult;
        }

        var resultType = IrTypes.FromTypeReference(binary.ResultType);
        var result = _currentFunction!.CreateValue(resultType);
        Emit(new BinaryInst(op.Value, left, right, result));
        return result;
    }

    private IrValue LowerComparisonExpr(ComparisonExpressionNode comparison)
    {
        var left = comparison.Left != null ? LowerExpression(comparison.Left) : null;
        var right = comparison.Right != null ? LowerExpression(comparison.Right) : null;

        if (left == null || right == null)
        {
            return _currentFunction!.CreateValue(IrTypes.Bool);
        }

        var op = comparison.Operation switch
        {
            ComparisonOperation.Equal => CompareOp.Equal,
            ComparisonOperation.NotEqual => CompareOp.NotEqual,
            ComparisonOperation.LessThan => CompareOp.LessThan,
            ComparisonOperation.LessThanOrEqual => CompareOp.LessThanOrEqual,
            ComparisonOperation.GreaterThan => CompareOp.GreaterThan,
            ComparisonOperation.GreaterThanOrEqual => CompareOp.GreaterThanOrEqual,
            _ => CompareOp.Equal
        };

        var result = _currentFunction!.CreateValue(IrTypes.Bool);
        Emit(new CompareInst(op, left, right, result));
        return result;
    }

    private IrValue LowerVarAccess(VarAccessNode varAccess)
    {
        var varName = varAccess.Name.Value;

        if (_locals.TryGetValue(varName, out var slot))
        {
            var loadType = IrTypes.FromTypeReference(varAccess.ResultType);
            var result = _currentFunction!.CreateValue(loadType, varName);
            Emit(new LoadInst(slot, result));
            return result;
        }

        // Fallback: create an unresolved value
        var fallbackType = IrTypes.FromTypeReference(varAccess.ResultType);
        return _currentFunction!.CreateValue(fallbackType, varName);
    }

    private IrValue LowerParamAccess(ParamAccessNode paramAccess)
    {
        var paramName = paramAccess.Name.Value;

        if (_params.TryGetValue(paramName, out var paramValue))
        {
            return paramValue;
        }

        var fallbackType = IrTypes.FromTypeReference(paramAccess.ResultType);
        return _currentFunction!.CreateValue(fallbackType, paramName);
    }

    private IrValue LowerPropAccess(PropAccessNode propAccess)
    {
        var receiver = LowerExpression(propAccess.Object);
        if (receiver == null)
        {
            var ft = IrTypes.FromTypeReference(propAccess.ResultType);
            return _currentFunction!.CreateValue(ft);
        }

        var propName = propAccess.Property is IdentifierNode id ? id.Value : propAccess.Property.ToString() ?? "";
        var propType = IrTypes.FromTypeReference(propAccess.ResultType);
        var result = _currentFunction!.CreateValue(propType);
        Emit(new PropertyAccessInst(receiver, propName, propType, isLoad: true, result: result));
        return result;
    }

    private IrValue? LowerIdentifier(IdentifierNode identifier)
    {
        // Check locals first
        if (_locals.TryGetValue(identifier.Value, out var slot))
        {
            var loadType = IrTypes.FromTypeReference(identifier.ResultType);
            var result = _currentFunction!.CreateValue(loadType, identifier.Value);
            Emit(new LoadInst(slot, result));
            return result;
        }

        // Check params
        if (_params.TryGetValue(identifier.Value, out var paramVal))
        {
            return paramVal;
        }

        var fallbackType = IrTypes.FromTypeReference(identifier.ResultType);
        return _currentFunction!.CreateValue(fallbackType, identifier.Value);
    }

    private IrValue? LowerFuncCall(FuncCallNode funcCall)
    {
        var args = new List<IrCallArg>();
        foreach (var arg in funcCall.Args)
        {
            var argValue = LowerExpression(arg.Value);
            if (argValue != null)
            {
                args.Add(new IrCallArg(arg.Name, argValue));
            }
        }

        var returnType = IrTypes.FromTypeReference(funcCall.ResultType);
        IrValue? result = returnType is IrPrimitiveType p && p.Primitive == PrimitiveKind.Void
            ? null
            : _currentFunction!.CreateValue(returnType);

        Emit(new CallInst(funcCall.Target.Value, args, returnType, result));
        return result;
    }

    private IrValue? LowerMethodCall(MethodCallNode methodCall)
    {
        var receiver = LowerExpression(methodCall.Object);

        var args = new List<IrCallArg>();
        foreach (var arg in methodCall.Args)
        {
            var argValue = LowerExpression(arg.Value);
            if (argValue != null)
            {
                args.Add(new IrCallArg(arg.Name, argValue));
            }
        }

        var returnType = IrTypes.FromTypeReference(methodCall.ResultType);
        IrValue? result = returnType is IrPrimitiveType p && p.Primitive == PrimitiveKind.Void
            ? null
            : _currentFunction!.CreateValue(returnType);

        Emit(new CallInst(methodCall.Target.Value, args, returnType, result, receiver: receiver));
        return result;
    }

    private IrValue? LowerInitCall(InitCallNode initCall)
    {
        var args = new List<IrCallArg>();
        foreach (var arg in initCall.Args)
        {
            var argValue = LowerExpression(arg.Value);
            if (argValue != null)
            {
                args.Add(new IrCallArg(arg.Name, argValue));
            }
        }

        var returnType = IrTypes.FromTypeReference(initCall.ResultType);
        var result = _currentFunction!.CreateValue(returnType);

        Emit(new CallInst(initCall.TypeFullName + "..ctor", args, returnType, result, isConstructor: true));
        return result;
    }

    // ------------------------------------------------------------------
    // Control flow lowering
    // ------------------------------------------------------------------

    private void LowerWhile(WhileNode whileNode)
    {
        var condBlock = _currentFunction!.CreateBlock("while.cond");
        var bodyBlock = _currentFunction!.CreateBlock("while.body");
        var exitBlock = _currentFunction!.CreateBlock("while.exit");

        // Branch from current block to condition
        Emit(new BranchInst(condBlock));

        // Condition block: evaluate condition, conditional branch
        _currentBlock = condBlock;
        var condValue = LowerExpression(whileNode.Condition);
        if (condValue != null)
        {
            Emit(new CondBranchInst(condValue, bodyBlock, exitBlock));
        }

        // Body block: emit body, branch back to condition
        _currentBlock = bodyBlock;
        _loopStack.Push((condBlock, exitBlock));
        LowerBlock(whileNode.Body);
        _loopStack.Pop();
        if (!_currentBlock.IsTerminated)
        {
            Emit(new BranchInst(condBlock));
        }

        // Continue in exit block
        _currentBlock = exitBlock;
    }

    private void LowerIf(IfNode ifNode)
    {
        var endBlock = _currentFunction!.CreateBlock("if.end");

        // Determine what the false branch of the if-condition should be
        IrBasicBlock falseBlock;
        if (ifNode.ElseClauses.Count > 0)
        {
            var firstClause = ifNode.ElseClauses[0];
            falseBlock = _currentFunction!.CreateBlock(firstClause.Condition != null ? "if.else1" : "if.else");
        }
        else
        {
            falseBlock = endBlock;
        }

        var thenBlock = _currentFunction!.CreateBlock("if.then");

        // Evaluate condition in current block, conditional branch
        var condValue = LowerExpression(ifNode.Condition);
        if (condValue != null)
        {
            Emit(new CondBranchInst(condValue, thenBlock, falseBlock));
        }

        // Then block
        _currentBlock = thenBlock;
        LowerBlock(ifNode.Body);
        if (!_currentBlock.IsTerminated)
        {
            Emit(new BranchInst(endBlock));
        }

        // Emit else-if / else clauses
        for (int i = 0; i < ifNode.ElseClauses.Count; i++)
        {
            var clause = ifNode.ElseClauses[i];

            if (clause.Condition != null)
            {
                // else-if clause
                _currentBlock = falseBlock;

                // Determine the next false target
                IrBasicBlock nextFalseBlock;
                if (i + 1 < ifNode.ElseClauses.Count)
                {
                    var nextClause = ifNode.ElseClauses[i + 1];
                    nextFalseBlock = _currentFunction!.CreateBlock(
                        nextClause.Condition != null ? $"if.else{i + 2}" : "if.else");
                }
                else
                {
                    nextFalseBlock = endBlock;
                }

                var elseThenBlock = _currentFunction!.CreateBlock($"if.elsethen{i + 1}");
                var elseCondValue = LowerExpression(clause.Condition);
                if (elseCondValue != null)
                {
                    Emit(new CondBranchInst(elseCondValue, elseThenBlock, nextFalseBlock));
                }

                // Emit the else-if body
                _currentBlock = elseThenBlock;
                LowerBlock(clause.Body);
                if (!_currentBlock.IsTerminated)
                {
                    Emit(new BranchInst(endBlock));
                }

                falseBlock = nextFalseBlock;
            }
            else
            {
                // Plain else clause
                _currentBlock = falseBlock;
                LowerBlock(clause.Body);
                if (!_currentBlock.IsTerminated)
                {
                    Emit(new BranchInst(endBlock));
                }
            }
        }

        // Continue in end block
        _currentBlock = endBlock;
    }

    private void LowerFor(ForNode forNode)
    {
        if (forNode.Iterable is RangeExpressionNode range)
        {
            LowerRangeFor(forNode, range);
        }
        // TODO: Collection iteration (for item in items) — requires IEnumerable support
    }

    private void LowerRangeFor(ForNode forNode, RangeExpressionNode range)
    {
        // Allocate the iterator variable
        var iterAlloc = _currentFunction!.CreateValue(IrTypes.I32, forNode.IteratorName);
        Emit(new AllocInst(forNode.IteratorName, IrTypes.I32, iterAlloc));

        // Register iterator in locals (unless discard)
        if (forNode.IteratorName != "_")
        {
            _locals[forNode.IteratorName] = iterAlloc;
        }

        // Initialize iterator = range.Start
        var startValue = LowerExpression(range.Start);
        if (startValue != null)
        {
            Emit(new StoreInst(startValue, iterAlloc));
        }

        // Evaluate range end (once, before the loop)
        var endValue = LowerExpression(range.End);

        var condBlock = _currentFunction!.CreateBlock("for.cond");
        var bodyBlock = _currentFunction!.CreateBlock("for.body");
        var incBlock = _currentFunction!.CreateBlock("for.inc");
        var exitBlock = _currentFunction!.CreateBlock("for.exit");

        // Branch to condition
        Emit(new BranchInst(condBlock));

        // Condition block: iterator <= end
        _currentBlock = condBlock;
        var iterLoad = _currentFunction!.CreateValue(IrTypes.I32, forNode.IteratorName);
        Emit(new LoadInst(iterAlloc, iterLoad));
        if (endValue != null)
        {
            var cmpResult = _currentFunction!.CreateValue(IrTypes.Bool);
            Emit(new CompareInst(CompareOp.LessThanOrEqual, iterLoad, endValue, cmpResult));
            Emit(new CondBranchInst(cmpResult, bodyBlock, exitBlock));
        }

        // Body block
        _currentBlock = bodyBlock;
        _loopStack.Push((incBlock, exitBlock));
        LowerBlock(forNode.Body);
        _loopStack.Pop();
        if (!_currentBlock.IsTerminated)
        {
            Emit(new BranchInst(incBlock));
        }

        // Increment block: iterator = iterator + 1
        _currentBlock = incBlock;
        var iterLoad2 = _currentFunction!.CreateValue(IrTypes.I32, forNode.IteratorName);
        Emit(new LoadInst(iterAlloc, iterLoad2));
        var oneConst = _currentFunction!.CreateValue(IrTypes.I32);
        Emit(new ConstantInst(ConstantKind.Integer, "1", IrTypes.I32, oneConst));
        var incResult = _currentFunction!.CreateValue(IrTypes.I32);
        Emit(new BinaryInst(BinaryOp.Add, iterLoad2, oneConst, incResult));
        Emit(new StoreInst(incResult, iterAlloc));
        Emit(new BranchInst(condBlock));

        // Continue in exit block
        _currentBlock = exitBlock;
    }

    private void LowerBreak()
    {
        if (_loopStack.Count == 0) return;
        var (_, breakTarget) = _loopStack.Peek();
        Emit(new BranchInst(breakTarget));
    }

    private void LowerContinue()
    {
        if (_loopStack.Count == 0) return;
        var (continueTarget, _) = _loopStack.Peek();
        Emit(new BranchInst(continueTarget));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void BeginFunction(IrFunction func)
    {
        _currentFunction = func;
        _locals.Clear();
        _params.Clear();
        _selfValue = null;
        _loopStack.Clear();
    }

    private void EndFunction()
    {
        _currentFunction = null;
        _currentBlock = null;
        _locals.Clear();
        _params.Clear();
        _selfValue = null;
        _loopStack.Clear();
    }

    private void Emit(IrInstruction instruction)
    {
        _currentBlock?.Append(instruction);
    }

    private void EnsureTerminated(IrType returnType)
    {
        if (_currentBlock != null && !_currentBlock.IsTerminated)
        {
            if (returnType is IrPrimitiveType p && p.Primitive == PrimitiveKind.Void)
            {
                Emit(new ReturnInst());
            }
            else
            {
                // For non-void functions without an explicit return, emit return void
                // (this would be a type error in a real compiler, but we handle it gracefully)
                Emit(new ReturnInst());
            }
        }
    }

    private static string BuildFunctionName(FuncNode funcNode, IrType? owningType)
    {
        if (owningType != null)
        {
            return $"{owningType.Name}.{funcNode.Name}";
        }

        // For free functions, try to get a qualified name from the module hierarchy
        var moduleName = GetModuleName(funcNode);
        return moduleName != null ? $"{moduleName}.{funcNode.Name}" : funcNode.Name;
    }

    private static string? GetModuleName(INode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ModuleNode moduleNode)
                return moduleNode.Name;
            current = current.Parent;
        }
        return null;
    }
}
