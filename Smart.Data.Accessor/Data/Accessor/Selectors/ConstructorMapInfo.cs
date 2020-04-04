namespace Smart.Data.Accessor.Selectors
{
    using System.Reflection;

    public class ConstructorMapInfo
    {
        public ConstructorInfo Info { get; }

        public int[] Indexes { get; }

        public ConstructorMapInfo(ConstructorInfo ci, int[] indexes)
        {
            Info = ci;
            Indexes = indexes;
        }
    }
}
