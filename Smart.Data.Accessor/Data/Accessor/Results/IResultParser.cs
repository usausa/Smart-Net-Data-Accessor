namespace Smart.Data.Accessor.Results;

public interface IResultParser
{
    object Parse(Type type, object value);
}
