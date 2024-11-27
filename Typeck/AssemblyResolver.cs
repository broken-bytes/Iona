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
        private SymbolTable _symbolTable = new();

        internal AssemblyResolver()
        {
        }

        internal void AddAssembliesToSymbolTable(List<Assembly> assemblies, SymbolTable table)
        {
            _symbolTable = table;

            foreach (var assembly in assemblies)
            {
                ConstructTypesForAssembly(assembly);
            }

            foreach (var assembly in assemblies)
            {
                PopulateMembersForAssembly(assembly);
            }
        }
        
        private void ConstructTypesForAssembly(Assembly assembly)
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

                foreach (var type in types)
                {
                    var nspace = type.Namespace;

                    if (nspace == null)
                    {
                        continue;
                    }

                    var split = nspace.Split('.');

                    var module = _symbolTable.Modules.Find(m => m.Name == split.First());
                    if (module == null)
                    {
                        module = new ModuleSymbol(split.First(), assembly.FullName);
                        _symbolTable.Modules.Add(module);
                    }

                    foreach (var name in split.Skip(1))
                    {
                        var nextModule = module.Symbols.OfType<ModuleSymbol>()
                            .ToList()
                            .FirstOrDefault(m => m.Name == name);
                        if (nextModule == null)
                        {
                            var newModule = new ModuleSymbol(name, assembly.FullName);
                            newModule.Parent = module;
                            module.Symbols.Add(newModule);
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
                    symbol.Parent = module;
                    module.Symbols.Add(symbol);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void PopulateMembersForAssembly(Assembly assembly)
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

                foreach (var type in types)
                {
                    var typeSymbol = _symbolTable.FindTypeByFQN(type.FullName);

                    if (typeSymbol == null)
                    {
                        continue;
                    }

                    foreach (var member in type.GetMembers())
                    {
                        if (member.MemberType == MemberTypes.Method)
                        {
                            var method = member as MethodInfo;
                            var ionaName = Shared.Utils.CSharpToIonaName(method.Name);
                            var funcSymbol = new FuncSymbol(ionaName, method.Name);

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
                                    generic.Parent = typeSymbol;
                                    funcSymbol.Symbols.Add(generic);
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
                                var unboxed =
                                    Shared.Utils.GetUnboxedName(method.ReturnType.FullName ?? method.ReturnType.Name);
                                returnType = _symbolTable.FindTypeByFQN(unboxed);

                                if (returnType is null)
                                {
                                    continue;
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
                                    var paramType = _symbolTable.FindTypeByFQN(
                                        param?.ParameterType.FullName ??
                                        param?.ParameterType.Name);

                                    paramSymbol = new ParameterSymbol(param.Name, paramType, null);
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


                                opSymbol.Symbols.AddRange(parameters);

                                typeSymbol.Symbols.Add(opSymbol);

                                continue;
                            }

                            // If the func is a regular func and not an operator, add it
                            foreach (var parameter in parameters)
                            {
                                funcSymbol.Symbols.Add(parameter);
                                parameter.Parent = funcSymbol;
                            }

                            typeSymbol.Symbols.Add(funcSymbol);
                        }
                        else if (member.MemberType == MemberTypes.Field)
                        {
                            var field = member as FieldInfo;
                            var fieldType =
                                _symbolTable.FindTypeByFQN(field?.FieldType.FullName ?? field.FieldType.Name);

                            var ionaName = Shared.Utils.CSharpToIonaName(field.Name);
                            
                            // If the type is an enum we add enum cases instead of properties
                            if (type.IsEnum)
                            {
                                var enumCase = new EnumCaseSymbol(ionaName, field.Name);
                                typeSymbol.Symbols.Add(enumCase);
                                enumCase.Parent = typeSymbol;
                                
                                continue;
                            }
                            
                            var fieldSymbol = new PropertySymbol(
                                ionaName, 
                                field.Name, 
                                fieldType, 
                                field.IsStatic, false);
                            fieldSymbol.Parent = typeSymbol;
                            typeSymbol.Symbols.Add(fieldSymbol);
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
                            
                            var propType = _symbolTable.FindTypeByFQN(unboxed);
                            var propertySymbol = new PropertySymbol(
                                ionaName,
                                prop.Name,
                                propType,
                                prop.GetGetMethod()?.IsStatic ?? false,
                                true,
                                getterAccessLevel,
                                setterAccessLevel
                            );
                            propertySymbol.Parent = typeSymbol;
                            typeSymbol.Symbols.Add(propertySymbol);
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
                                var paramType = _symbolTable.FindTypeByFQN(boxedName);

                                var paramSymbol = new ParameterSymbol(param.Name, paramType, initSymbol);
                                initSymbol.Parent = typeSymbol;
                                initSymbol.Symbols.Add(paramSymbol);
                            }

                            typeSymbol.Symbols.Add(initSymbol);
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