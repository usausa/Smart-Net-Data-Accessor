namespace Smart.Data.Accessor.Engine
{
    public interface IEngineController
    {
        int CountResultMapperCache { get; }

        void ClearResultMapperCache();
    }
}
