namespace Smart.Data.Accessor.Generator;

public class Reference
{
    public string Name { get; }

    public string FilePath { get; }

    public Reference(string filePath)
    {
        Name = Path.GetFileNameWithoutExtension(filePath);
        FilePath = filePath;
    }
}
