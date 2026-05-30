namespace Smart.Data.Accessor.Converters;

public interface IValueConverter<TDb, TClr>
{
    static abstract TClr FromDb(TDb dbValue);

    static abstract TDb ToDb(TClr clrValue);
}
