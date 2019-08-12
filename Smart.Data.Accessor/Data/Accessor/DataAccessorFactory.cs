namespace Smart.Data.Accessor
{
    public sealed class DataAccessorFactory
    {
        //public T Create<T>()
        //{
        //    return (T)Create(typeof(T));
        //}

        //public object Create(Type type)
        //{
        //    if (type is null)
        //    {
        //        throw new ArgumentNullException(nameof(type));
        //    }

        //    if (!cache.TryGetValue(type, out var dao))
        //    {
        //        dao = cache.AddIfNotExist(type, CreateInternal);
        //        if (dao == null)
        //        {
        //            throw new AccessorGeneratorException($"Dao generate failed. type=[{type.FullName}]");
        //        }
        //    }

        //    return dao;
        //}
    }
}
