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
            var path = $"{Environment.CurrentDirectory}\\{Name}.il";
            // Create or empty file at `Name + ".dll"`
            File.Create(path).Close();

            // Empty the file
            File.WriteAllText(path, string.Empty);

            var stream = new StreamWriter(path, true);

            foreach (var module in assembly.Modules)
            {
                module.Write(stream);
            }

            stream.Close();
            stream.Dispose();
        }
    }
}
