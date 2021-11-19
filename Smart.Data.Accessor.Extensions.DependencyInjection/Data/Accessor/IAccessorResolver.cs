namespace Smart.Data.Accessor;

public interface IAccessorResolver<out T>
{
    T Accessor { get; }
}
