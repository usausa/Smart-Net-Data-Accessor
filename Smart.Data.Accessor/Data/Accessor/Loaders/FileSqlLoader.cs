namespace Smart.Data.Accessor.Loaders
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;

    using Smart.Data.Accessor.Helpers;

    public sealed class FileSqlLoader : ISqlLoader
    {
        private readonly Encoding encoding;

        private readonly string root;

        private readonly string prefix;

        private readonly string extension;

        public FileSqlLoader(Encoding encoding, string root, string prefix, string extension)
        {
            this.encoding = encoding ?? Encoding.UTF8;
            this.root = root;
            this.prefix = prefix;
            this.extension = extension ?? "sql";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public string Load(MethodInfo mi)
        {
            var path = root;
            if (!String.IsNullOrEmpty(prefix) && mi.DeclaringType.Namespace.StartsWith(prefix))
            {
                var dir = mi.DeclaringType.Namespace.Substring(prefix.Length);
                if (dir.StartsWith("."))
                {
                    dir = dir.Substring(1);
                }

                path = Path.Combine(path, dir.Replace('.', Path.DirectorySeparatorChar));
            }
            else
            {
                path = Path.Combine(path, mi.DeclaringType.Namespace.Replace('.', Path.DirectorySeparatorChar));
            }

            path = Path.Combine(path, $"{TypeHelper.MakeDaoName(mi.DeclaringType)}_{mi.Name}");
            if (!String.IsNullOrEmpty(extension))
            {
                path = Path.ChangeExtension(path, extension);
            }

            return File.ReadAllText(path, encoding);
        }
    }
}