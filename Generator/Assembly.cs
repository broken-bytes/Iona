using AST.Nodes;
using Generator.Types;
using Symbols;

namespace Generator
{
    public class Assembly
    {
        public string Name { get; set; }
        private readonly AssemblyBuilder builder;
        AssemblyDefinition assembly;

        public Assembly(string name, SymbolTable table)
        {
            Name = name;

            assembly = new AssemblyDefinition(name);

            builder = new AssemblyBuilder(table, new ILEmitter(), assembly);
        }

        public Assembly Generate(INode node)
        {
            builder.Build(node);

            return this;
        }

        public void Build()
        {
            // Create or empty file at `Name + ".dll"`
            File.Create(Name + ".dll").Close();

            foreach (var module in assembly.Modules)
            {
                module.Write(Name + ".dll");
            }
        }
    }
}
