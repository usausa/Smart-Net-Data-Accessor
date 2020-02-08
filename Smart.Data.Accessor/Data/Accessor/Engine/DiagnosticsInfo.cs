namespace Smart.Data.Accessor.Engine
{
    public sealed class DiagnosticsInfo
    {
        public int ResultMapperCacheCount { get; }

        public int ResultMapperCacheWidth { get; }

        public int ResultMapperCacheDepth { get; }

        public int DynamicSetupCacheCount { get; }

        public int DynamicSetupCacheWidth { get; }

        public int DynamicSetupCacheDepth { get; }

        public DiagnosticsInfo(
            int resultMapperCacheCount,
            int resultMapperCacheWidth,
            int resultMapperCacheDepth,
            int dynamicSetupCacheCount,
            int dynamicSetupCacheWidth,
            int dynamicSetupCacheDepth)
        {
            ResultMapperCacheCount = resultMapperCacheCount;
            ResultMapperCacheWidth = resultMapperCacheWidth;
            ResultMapperCacheDepth = resultMapperCacheDepth;
            DynamicSetupCacheCount = dynamicSetupCacheCount;
            DynamicSetupCacheWidth = dynamicSetupCacheWidth;
            DynamicSetupCacheDepth = dynamicSetupCacheDepth;
        }
    }
}
