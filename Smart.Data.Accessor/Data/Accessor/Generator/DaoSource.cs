namespace Smart.Data.Accessor.Generator
{
    using System;

    public sealed class DaoSource
    {
        public Type TargetType { get; }

        public string ImplementTypeFullName { get; }

        public string Code { get; }

        public DaoSource(Type targetType, string implementTypeFullName, string code)
        {
            TargetType = targetType;
            ImplementTypeFullName = implementTypeFullName;
            Code = code;
        }
    }
}
