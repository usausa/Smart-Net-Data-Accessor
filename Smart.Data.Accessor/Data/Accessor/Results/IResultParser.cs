namespace Smart.Data.Accessor.Results;

using System;

public interface IResultParser
{
    object Parse(Type type, object value);
}
