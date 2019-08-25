namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;

    public class FileSqlLoader : ISqlLoader
    {
        private readonly string rootDirectory;

        private readonly string rootNamespace;

        private readonly string subDirectory;

        public FileSqlLoader(string rootDirectory, string rootNamespace, string subDirectory)
        {
            this.rootDirectory = rootDirectory;
            this.rootNamespace = rootNamespace;
            this.subDirectory = subDirectory;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public string Load(MethodInfo mi)
        {
            var type = mi.DeclaringType;
            var ns = type.Namespace;

            // Dir
            var dir = rootDirectory;
            if (ns.StartsWith(rootNamespace + "."))
            {
                dir = Path.Combine(rootDirectory, ns.Substring(rootNamespace.Length + 1).Replace('.', Path.DirectorySeparatorChar));
            }
            else if (ns != rootNamespace)
            {
                throw new AccessorGeneratorException($"SQL load failed. type=[{type.FullName}], method=[{mi.Name}]");
            }

            if (!String.IsNullOrEmpty(subDirectory))
            {
                dir = Path.Combine(dir, subDirectory);
            }

            // File
            var index = type.FullName.LastIndexOf('.');
            var name = index >= 0 ? type.FullName.Substring(index + 1) : type.FullName;
            var filename = $"{name.Replace('+', '.')}.{mi.Name}.sql";
            var path = Path.Combine(dir, filename);
            if (!File.Exists(path))
            {
                throw new AccessorGeneratorException($"SQL load failed. type=[{type.FullName}], method=[{mi.Name}], path=[{path}]");
            }

            return File.ReadAllText(path, Encoding.UTF8);
        }
    }
}
