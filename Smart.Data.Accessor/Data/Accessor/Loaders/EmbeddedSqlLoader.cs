namespace Smart.Data.Accessor.Loaders
{
    using System;
    using System.Reflection;
    using System.Text;

    using Smart.IO;

    public sealed class EmbeddedSqlLoader : ISqlLoader
    {
        private readonly Encoding encoding;

        private readonly string folder;

        private readonly string extension;

        public EmbeddedSqlLoader(Encoding encoding, string folder, string extension)
        {
            this.encoding = encoding ?? Encoding.UTF8;
            this.folder = folder ?? "Sql";
            this.extension = extension ?? "sql";
        }

        public string Load(MethodInfo mi)
        {
            var name = new StringBuilder();
            name.Append(mi.DeclaringType.Namespace);
            name.Append(".");
            if (!String.IsNullOrEmpty(folder))
            {
                name.Append(folder);
                name.Append(".");
            }
            name.Append(mi.DeclaringType.Name);
            name.Append("_");
            name.Append(mi.Name);
            if (!String.IsNullOrEmpty(extension))
            {
                name.Append(".");
                name.Append(extension);
            }

            using (var stream = mi.DeclaringType.Assembly.GetManifestResourceStream(name.ToString()))
            {
                return encoding.GetString(stream.ReadAllBytes());
            }
        }
    }
}
