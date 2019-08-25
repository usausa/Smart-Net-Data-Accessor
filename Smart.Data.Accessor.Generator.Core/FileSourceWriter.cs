namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class FileSourceWriter : ISourceWriter
    {
        private readonly string outputDirectory;

        private readonly List<string> newFiles = new List<string>();

        public FileSourceWriter(string outputDirectory)
        {
            this.outputDirectory = outputDirectory;

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void Write(Type type, string source)
        {
            var filename = type.FullName.Replace('.', '_').Replace('+', '_') + ".g.cs";
            var path = Path.Combine(outputDirectory, filename);

            newFiles.Add(filename);

            if (File.Exists(path))
            {
                var currentSource = File.ReadAllText(path);
                if (currentSource == source)
                {
                    return;
                }
            }

            File.WriteAllText(path, source);
        }

        public void PostProcess()
        {
            var currentFiles = Directory.GetFiles(outputDirectory).Select(Path.GetFileName);
            foreach (var file in currentFiles.Except(newFiles))
            {
                File.Delete(Path.Combine(outputDirectory, file));
            }
        }
    }
}
