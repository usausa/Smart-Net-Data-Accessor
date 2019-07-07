namespace Smart.Data.Accessor.Generator
{
    public interface IGeneratorDebugger
    {
        void Log(bool success, DaoSource source, BuildError[] errors);
    }
}
