namespace Smart.Data.Accessor.Engine
{
    public static class ExecuteEngineConfigExtensions
    {
        public static ExecuteEngine ToEngine(this ExecuteEngineConfig config) => new(config);
    }
}
