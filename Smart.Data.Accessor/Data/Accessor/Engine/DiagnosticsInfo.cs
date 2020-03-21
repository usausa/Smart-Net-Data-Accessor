namespace Smart.Data.Accessor.Engine
{
    public sealed class DiagnosticsInfo
    {
        public int DynamicSetupCacheCount { get; }

        public int DynamicSetupCacheWidth { get; }

        public int DynamicSetupCacheDepth { get; }

        public DiagnosticsInfo(
            int dynamicSetupCacheCount,
            int dynamicSetupCacheWidth,
            int dynamicSetupCacheDepth)
        {
            DynamicSetupCacheCount = dynamicSetupCacheCount;
            DynamicSetupCacheWidth = dynamicSetupCacheWidth;
            DynamicSetupCacheDepth = dynamicSetupCacheDepth;
        }
    }
}
