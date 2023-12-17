namespace Smart.Data.Accessor.Generator;

public sealed class FileSourceWriter : ISourceWriter
{
    private readonly string outputDirectory;

    private readonly List<string> newFiles = [];

    public IEnumerable<string> NewFiles => newFiles;

    public FileSourceWriter(string outputDirectory)
    {
        this.outputDirectory = outputDirectory;

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
    }

    public void Write(Type type, string source)
    {
        var filename = type.FullName!.Replace('.', '_').Replace('+', '_') + ".g.cs";
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
}
