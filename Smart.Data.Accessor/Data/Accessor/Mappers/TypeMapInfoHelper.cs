namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;

    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Selectors;

    internal static class TypeMapInfoHelper
    {
        public static void BuildConverterMap(
            TypeMapInfo typeMap,
            IResultMapperCreateContext context,
            ColumnInfo[] columns,
            Dictionary<int, Func<object, object>> map)
        {
            if (typeMap.Constructor is not null)
            {
                foreach (var parameterMap in typeMap.Constructor.Parameters)
                {
                    var converter = context.GetConverter(columns[parameterMap.Index].Type, parameterMap.Info.ParameterType, parameterMap.Info);
                    if (converter is not null)
                    {
                        map[parameterMap.Index] = converter;
                    }
                }
            }

            foreach (var propertyMap in typeMap.Properties)
            {
                var converter = context.GetConverter(columns[propertyMap.Index].Type, propertyMap.Info.PropertyType, propertyMap.Info);
                if (converter is not null)
                {
                    map[propertyMap.Index] = converter;
                }
            }
        }

        public static IEnumerable<Type> EnumerateTypes(TypeMapInfo typeMap)
        {
            if (typeMap.Constructor is not null)
            {
                foreach (var parameterMap in typeMap.Constructor.Parameters)
                {
                    yield return parameterMap.Info.ParameterType;
                }
            }

            foreach (var propertyMap in typeMap.Properties)
            {
                yield return propertyMap.Info.PropertyType;
            }
        }
    }
}
