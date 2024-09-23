using AST.Nodes;
using AST.Visitors;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Symbols;

namespace Generator
{
    public class Assembly
    {
        public string Name { get; set; }
        private SymbolTable? table;
        private AssemblyDefinition? assembly;
        private readonly AssemblyBuilder builder;

        public Assembly(string name, SymbolTable table)
        {
            Name = name;
            this.table = table;

            var mp = new ModuleParameters
            {
                Kind = ModuleKind.Dll
            };

            // Create the assembly
            var assemblyName = new AssemblyNameDefinition(name, new Version(1, 0, 0, 0));
            assembly = AssemblyDefinition.CreateAssembly(assemblyName, name, mp);

            builder = new AssemblyBuilder(assembly, table);
        }

        public Assembly Generate(INode node)
        {
            builder.Build(node);

            return this;
        }

        public void Build()
        {
            assembly?.Write(Name + ".dll");
        }
    }
}
