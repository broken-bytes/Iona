//|--- AssemblyResolver.cs -------------------------------------|
//
// This source code file is part of the Iona project.
//
// Copyright (c) 2026 Marcel Kulina
// Licensed under MIT
//
//|-------------------------------------------------------------|

using AST.Nodes;
using AST.Types;
using AST.Visitors;
using Symbols;
using Symbols.Symbols;
using System.Reflection;
using System.Runtime.Loader;

namespace Typeck
{
    public class AssemblyResolver
    {
        internal AssemblyResolver()
        {
        }

        internal void AddAssembliesToSymbolTable(List<Assembly> assemblies, SymbolTable table)
        {
            foreach (var assembly in assemblies)
            {
                ConstructTypesForAssembly(assembly, table);
            }

            foreach (var assembly in assemblies)
            {
                PopulateMembersForAssembly(assembly, table);
            }
        }

        private static TypeSymbol MakeOptional(TypeSymbol baseType)
        {
            if (baseType.IsOptional)
            {
                return baseType;
            }

            return new TypeSymbol(baseType.Name, baseType.TypeKind)
            {
                Symbols = baseType.Symbols,
                SymbolsByName = baseType.SymbolsByName,
                Parent = baseType.Parent,
                BaseType = baseType.BaseType,
                Contracts = baseType.Contracts,
                IsOptional = true,
                InnerType = baseType
            };
        }

        // C# reference types are always nullable at the CLR level, so Iona treats them as
        // optional unless the assembly's Nullable Reference Type metadata says otherwise.
        // Value types are non-optional except Nullable<V>.
        private static bool IsOptionalNetType(System.Type netType, IList<CustomAttributeData> memberAttrs, byte contextDefault)
        {
            if (netType.IsValueType)
            {
                return netType.IsGenericType && netType.GetGenericTypeDefinition() == typeof(Nullable<>);
            }

            byte flag = contextDefault;
            var nullableAttr = memberAttrs.FirstOrDefault(a =>
                a.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");

            if (nullableAttr != null && nullableAttr.ConstructorArguments.Count > 0)
            {
                var arg = nullableAttr.ConstructorArguments[0];
                if (arg.Value is byte b)
                {
                    flag = b;
                }
                else if (arg.Value is IReadOnlyCollection<CustomAttributeTypedArgument> arr && arr.Count > 0)
                {
                    flag = arr.First().Value is byte first ? first : (byte)0;
                }
            }

            // 1 = not-null, 2 = nullable, 0 = oblivious (no NRT info -> treat as nullable).
            return flag != 1;
        }

        private static byte NullableContext(params MemberInfo?[] scopes)
        {
            foreach (var scope in scopes)
            {
                if (scope == null)
                {
                    continue;
                }

                var ctx = scope.GetCustomAttributesData().FirstOrDefault(a =>
                    a.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");

                if (ctx != null && ctx.ConstructorArguments.Count > 0 && ctx.ConstructorArguments[0].Value is byte b)
                {
                    return b;
                }
            }

            return 0;
        }
        
        private void ConstructTypesForAssembly(Assembly assembly, SymbolTable table)
        {
            try
            {
                // Try Loading each of the dependencies
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    try
                    {
                        Assembly.Load(reference);
                    }
                    catch
                    {
                        continue;
                    }
                }

                var types = assembly.GetExportedTypes();

                // Also include forwarded types (e.g., System.Runtime forwards to System.Private.CoreLib)
                try
                {
                    var forwarded = assembly.GetForwardedTypes();
                    types = types.Concat(forwarded).ToArray();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Some forwarded types may not resolve; use the ones that did
                    var loaded = ex.Types.Where(t => t != null).ToArray();
                    types = types.Concat(loaded!).ToArray();
                }
                catch
                {
                    // Ignore other errors
                }

                foreach (var type in types)
                {
                    var nspace = type.Namespace;

                    if (nspace == null)
                    {
                        continue;
                    }

                    var split = nspace.Split('.');

                    table.ModulesByName.TryGetValue(split.First(), out var module);
                    if (module == null)
                    {
                        module = new ModuleSymbol(split.First(), assembly.FullName);
                        table.AddModule(module);
                    }

                    foreach (var name in split.Skip(1))
                    {
                        var nextModule = module.LookupAllSymbols(name)
                            .OfType<ModuleSymbol>().FirstOrDefault();
                        if (nextModule == null)
                        {
                            var newModule = new ModuleSymbol(name, assembly.FullName);
                            module.AddSymbol(newModule);
                            module = newModule;
                        }
                        else
                        {
                            module = nextModule;
                        }
                    }

                    TypeKind kind = TypeKind.Unknown;
                    if (type.IsClass)
                    {
                        kind = TypeKind.Class;

                    }
                    else if (type.IsInterface)
                    {
                        kind = TypeKind.Contract;
                    }
                    else if (type.IsEnum)
                    {
                        kind = TypeKind.Enum;
                    }
                    else if (type.IsValueType)
                    {
                        kind = TypeKind.Struct;
                    }

                    var symbol = new TypeSymbol(type.Name, kind);
                    module.AddSymbol(symbol);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void PopulateMembersForAssembly(Assembly assembly, SymbolTable table)
        {
            try
            {
                // Try Loading each of the dependencies 
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    try
                    {
                        Assembly.Load(reference);
                    }
                    catch
                    {
                        continue;
                    }
                }

                var types = assembly.GetExportedTypes();

                // Also include forwarded types for member population
                try
                {
                    var forwarded = assembly.GetForwardedTypes();
                    types = types.Concat(forwarded).ToArray();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var loaded = ex.Types.Where(t => t != null).ToArray();
                    types = types.Concat(loaded!).ToArray();
                }
                catch
                {
                    // Ignore other errors
                }

                foreach (var type in types)
                {
                    var typeSymbol = table.FindTypeByFQN(type.FullName);

                    if (typeSymbol == null)
                    {
                        continue;
                    }

                    var isFreeFunctionModule = type.Name == "Module" && type.IsAbstract && type.IsSealed;
                    var ownerModule = typeSymbol.Parent as ModuleSymbol;

                    foreach (var member in type.GetMembers())
                    {
                        if (member.MemberType == MemberTypes.Method)
                        {
                            var method = member as MethodInfo;

                            if (isFreeFunctionModule && (!method!.IsStatic || method.DeclaringType != type))
                            {
                                continue;
                            }
                            var ionaName = Shared.Utils.CSharpToIonaName(method.Name);
                            var funcSymbol = new FuncSymbol(ionaName, method.Name);

                            // Set access level from .NET visibility
                            if (method.IsPublic)
                                funcSymbol.AccessLevel = AccessLevel.Public;
                            else if (method.IsFamily || method.IsFamilyOrAssembly)
                                funcSymbol.AccessLevel = AccessLevel.Internal;
                            else
                                funcSymbol.AccessLevel = AccessLevel.Private;

                            funcSymbol.Parent = typeSymbol;
                            TypeSymbol? returnType = null;

                            // Check for generic parameters
                            if (method.ContainsGenericParameters || method.IsGenericMethod ||
                                method.IsGenericMethodDefinition)
                            {
                                var args = method.GetGenericArguments();

                                foreach (var arg in args)
                                {
                                    var generic = new GenericParameterSymbol(arg.Name);
                                    funcSymbol.AddSymbol(generic);
                                }
                            }

                            var genericReturn = funcSymbol
                                .Symbols
                                .OfType<GenericParameterSymbol>()
                                .FirstOrDefault(symbol => symbol.Name == method.ReturnType.Name);

                            if (genericReturn is not null)
                            {
                                funcSymbol.ReturnType = new TypeSymbol(genericReturn.Name, TypeKind.Generic);
                            }
                            else
                            {
                                // Find the type symbol
                                var rawReturnName = method.ReturnType.FullName ?? method.ReturnType.Name;
                                // Strip array suffix for lookup (array types not yet in type system)
                                var baseReturnName = rawReturnName.TrimEnd('[', ']');
                                var unboxed = Shared.Utils.GetUnboxedName(baseReturnName);
                                returnType = table.FindTypeByFQN(unboxed);

                                if (returnType is null)
                                {
                                    continue;
                                }

                                if (IsOptionalNetType(method.ReturnType, method.ReturnParameter.GetCustomAttributesData(), NullableContext(method, type)))
                                {
                                    returnType = MakeOptional(returnType);
                                }

                                funcSymbol.ReturnType = returnType;
                            }
                            
                            var parameters = new List<ParameterSymbol>();
                            foreach (var param in method.GetParameters())
                            {
                                ParameterSymbol paramSymbol = null;
                                if (funcSymbol.Symbols.OfType<GenericParameterSymbol>()
                                    .Any(symbol => symbol.Name == param.Name))
                                {
                                    paramSymbol = new ParameterSymbol(param.Name, true, null);
                                }
                                else
                                {
                                    var rawParamName = param?.ParameterType.FullName ??
                                        param?.ParameterType.Name;
                                    var unboxedParam = Shared.Utils.GetUnboxedName(rawParamName);
                                    var paramType = table.FindTypeByFQN(unboxedParam);

                                    var paramOptional = paramType != null && param != null &&
                                        IsOptionalNetType(param.ParameterType, param.GetCustomAttributesData(), NullableContext(method, type));

                                    if (paramOptional)
                                    {
                                        paramType = MakeOptional(paramType);
                                    }

                                    paramSymbol = new ParameterSymbol(param.Name, paramType, null)
                                    {
                                        IsOptional = paramOptional
                                    };
                                }

                                parameters.Add(paramSymbol);
                            }

                            if (method.IsSpecialName)
                            {
                                OperatorType op;
                                // Check every C# builtin op_ and assign the operator accordingly
                                if (member.Name == "op_Addition")
                                {
                                    op = OperatorType.Add;
                                }
                                else if (member.Name == "op_Subtraction")
                                {
                                    op = OperatorType.Subtract;
                                }
                                else if (member.Name == "op_Multiply")
                                {
                                    op = OperatorType.Multiply;
                                }
                                else if (member.Name == "op_Division")
                                {
                                    op = OperatorType.Divide;
                                }
                                else if (member.Name == "op_Modulus")
                                {
                                    op = OperatorType.Multiply;
                                }
                                else if (member.Name == "op_Exponent")
                                {
                                    // TODO: Iona does not yet have a proper syntax for this
                                    continue;
                                }
                                else if (member.Name == "op_Equals")
                                {
                                    op = OperatorType.Equal;
                                }
                                else if (member.Name == "op_LessThan")
                                {
                                    op = OperatorType.LessThan;
                                }
                                else if (member.Name == "op_GreaterThan")
                                {
                                    op = OperatorType.GreaterThan;
                                }
                                else if (member.Name == "op_GreaterThanOrEqual")
                                {
                                    op = OperatorType.GreaterThanOrEqual;
                                }
                                else if (member.Name == "op_LessThan")
                                {
                                    op = OperatorType.LessThan;
                                }
                                else if (member.Name == "op_GreaterThanOrEqual")
                                {
                                    op = OperatorType.GreaterThanOrEqual;
                                }
                                else
                                {
                                    continue;
                                }

                                var opSymbol = new OperatorSymbol(op)
                                {
                                    ReturnType = returnType
                                };


                                foreach (var param in parameters)
                                {
                                    opSymbol.AddSymbol(param);
                                }

                                typeSymbol.AddSymbol(opSymbol);

                                continue;
                            }

                            // If the func is a regular func and not an operator, add it
                            foreach (var parameter in parameters)
                            {
                                funcSymbol.AddSymbol(parameter);
                            }

                            // Iona emits free functions as static methods on a "Module" class
                            // (see AssemblyBuilder.EmitFreeFunction). Surface them back as free
                            // functions on the enclosing module rather than as members of a type.
                            if (isFreeFunctionModule && ownerModule != null)
                            {
                                funcSymbol.Parent = ownerModule;
                                funcSymbol.CsharpOwnerFqn = type.FullName;
                                ownerModule.AddSymbol(funcSymbol);
                            }
                            else
                            {
                                typeSymbol.AddSymbol(funcSymbol);
                            }
                        }
                        else if (member.MemberType == MemberTypes.Field)
                        {
                            var field = member as FieldInfo;
                            var fieldType =
                                table.FindTypeByFQN(field?.FieldType.FullName ?? field.FieldType.Name);

                            if (fieldType != null && field != null &&
                                IsOptionalNetType(field.FieldType, field.GetCustomAttributesData(), NullableContext(type)))
                            {
                                fieldType = MakeOptional(fieldType);
                            }

                            var ionaName = Shared.Utils.CSharpToIonaName(field.Name);
                            
                            // If the type is an enum we add enum cases instead of properties
                            if (type.IsEnum)
                            {
                                var enumCase = new EnumCaseSymbol(ionaName, field.Name);
                                typeSymbol.AddSymbol(enumCase);
                                
                                continue;
                            }
                            
                            var fieldSymbol = new PropertySymbol(
                                ionaName,
                                field.Name,
                                fieldType,
                                field.IsStatic, false);
                            typeSymbol.AddSymbol(fieldSymbol);
                        }
                        else if (member.MemberType == MemberTypes.Property)
                        {
                            var prop = member as PropertyInfo;
                            AccessLevel getterAccessLevel = AccessLevel.Internal;
                            if (prop.GetGetMethod()?.IsPublic ?? false)
                            {
                                getterAccessLevel = AccessLevel.Public;
                            }
                            else if (prop.GetGetMethod()?.IsPrivate ?? false)
                            {
                                getterAccessLevel = AccessLevel.Private;
                            }

                            AccessLevel setterAccessLevel = AccessLevel.Internal;
                            if (prop.GetSetMethod()?.IsPublic ?? false)
                            {
                                setterAccessLevel = AccessLevel.Public;
                            }
                            else if (prop.GetSetMethod()?.IsPrivate ?? false)
                            {
                                setterAccessLevel = AccessLevel.Private;
                            }

                            // Get the boxed name 
                            var unboxed = Shared.Utils.GetUnboxedName(
                                prop?.PropertyType.FullName ??
                                prop.PropertyType.Name
                            );

                            var ionaName = Shared.Utils.CSharpToIonaName(prop.Name);
                            
                            var propType = table.FindTypeByFQN(unboxed);

                            if (propType != null && prop != null &&
                                IsOptionalNetType(prop.PropertyType, prop.GetCustomAttributesData(), NullableContext(prop.GetGetMethod(), type)))
                            {
                                propType = MakeOptional(propType);
                            }

                            var propertySymbol = new PropertySymbol(
                                ionaName,
                                prop.Name,
                                propType,
                                prop.GetGetMethod()?.IsStatic ?? false,
                                true,
                                getterAccessLevel,
                                setterAccessLevel
                            );
                            typeSymbol.AddSymbol(propertySymbol);
                        }
                        else if (member.MemberType == MemberTypes.Constructor)
                        {
                            var ctor = member as ConstructorInfo;

                            var initSymbol = new InitSymbol();
                            initSymbol.ReturnType = typeSymbol;

                            foreach (var param in ctor.GetParameters())
                            {
                                var boxedName =
                                    Shared.Utils.GetUnboxedName(param?.ParameterType.FullName ??
                                                                param?.ParameterType.Name);
                                var paramType = table.FindTypeByFQN(boxedName);

                                var paramSymbol = new ParameterSymbol(param.Name, paramType, initSymbol);
                                initSymbol.AddSymbol(paramSymbol);
                            }

                            initSymbol.Parent = typeSymbol;
                            typeSymbol.AddSymbol(initSymbol);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}