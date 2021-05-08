namespace Smart.Data.Accessor.Helpers
{
    using System;

    public static class TypeNaming
    {
        public static string MakeAccessorName(Type type)
        {
            var index = type.FullName!.LastIndexOf('.');
            return (index >= 0 ? type.FullName[(index + 1)..] : type.FullName).Replace('+', '_') + "_Impl";
        }
    }
}
