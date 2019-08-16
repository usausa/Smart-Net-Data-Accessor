namespace Smart.Data.Accessor.Generator
{
    using System;

    public interface ISourceWriter
    {
        void Write(Type type, string source);
    }
}
