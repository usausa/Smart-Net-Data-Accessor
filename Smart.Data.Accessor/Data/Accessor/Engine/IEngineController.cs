namespace Smart.Data.Accessor.Engine
{
    public interface IEngineController
    {
        int CountResultMapperCache { get; }

        void ClearResultMapperCache();

        int CountDynamicSetupCache { get; }

        void ClearDynamicSetupCache();
    }
}
