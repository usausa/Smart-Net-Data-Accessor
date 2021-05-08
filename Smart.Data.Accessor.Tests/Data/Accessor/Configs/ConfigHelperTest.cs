namespace Smart.Data.Accessor.Configs
{
    using Smart.Data.Accessor.Attributes;

    using Xunit;

    public class ConfigHelperTest
    {
        //--------------------------------------------------------------------------------
        // Table
        //--------------------------------------------------------------------------------

        public class TableEntity
        {
        }

        public class TableSuffixNoMatch
        {
        }

        [Name("T_TABLE")]
        public class TableWithNameEntity
        {
        }

        [EntitySuffix("Table")]
        public class TableWithSuffixTable
        {
        }

        [Fact]
        public void TestTableNamings()
        {
            var mi = GetType().GetMethod(nameof(TestTableNamings))!;

            Assert.Equal("Table", ConfigHelper.GetMethodTableName(mi, typeof(TableEntity)));
            Assert.Equal("TableSuffixNoMatch", ConfigHelper.GetMethodTableName(mi, typeof(TableSuffixNoMatch)));
            Assert.Equal("T_TABLE", ConfigHelper.GetMethodTableName(mi, typeof(TableWithNameEntity)));
            Assert.Equal("TableWithSuffix", ConfigHelper.GetMethodTableName(mi, typeof(TableWithSuffixTable)));
        }

        //--------------------------------------------------------------------------------
        // Column
        //--------------------------------------------------------------------------------

        public class PropertyEntity
        {
            public int Value { get; set; }
        }

        public class PropertyWithNameEntity
        {
            [Name("COL_VALUE")]
            public int Value { get; set; }
        }

        [DefaultNaming]
        public interface IPropertyInterface
        {
            PropertyEntity QueryProperty();

            PropertyWithNameEntity QueryPropertyWithName();

            void ExecuteProperty(PropertyEntity entity);

            void ExecutePropertyWithName(PropertyWithNameEntity entity);

            void ExecuteParameter(int value);

            void ExecuteParameterWithName([Name("COL_VALUE")] int value);
        }

        [Fact]
        public void TestColumnNamings()
        {
            var pi = typeof(PropertyEntity).GetProperty(nameof(PropertyEntity.Value))!;
            var piWithName = typeof(PropertyWithNameEntity).GetProperty(nameof(PropertyWithNameEntity.Value))!;
            var type = typeof(IPropertyInterface);
            var miQProp = type.GetMethod(nameof(IPropertyInterface.QueryProperty))!;
            var miQPropWn = type.GetMethod(nameof(IPropertyInterface.QueryPropertyWithName))!;
            var miEProp = type.GetMethod(nameof(IPropertyInterface.ExecuteProperty))!;
            var miEPropWn = type.GetMethod(nameof(IPropertyInterface.ExecutePropertyWithName))!;
            var miEParam = type.GetMethod(nameof(IPropertyInterface.ExecuteParameter))!;
            var miEParamWn = type.GetMethod(nameof(IPropertyInterface.ExecuteParameterWithName))!;

            Assert.Equal("Value", ConfigHelper.GetMethodPropertyColumnName(miQProp, pi));
            Assert.Equal("COL_VALUE", ConfigHelper.GetMethodPropertyColumnName(miQPropWn, piWithName));

            Assert.Equal("Value", ConfigHelper.GetMethodParameterPropertyColumnName(miEProp, miEProp.GetParameters()[0], pi));
            Assert.Equal("COL_VALUE", ConfigHelper.GetMethodParameterPropertyColumnName(miEPropWn, miEProp.GetParameters()[0], piWithName));

            Assert.Equal("Value", ConfigHelper.GetMethodParameterColumnName(miEProp, miEParam.GetParameters()[0]));
            Assert.Equal("COL_VALUE", ConfigHelper.GetMethodParameterColumnName(miEPropWn, miEParamWn.GetParameters()[0]));
        }
    }
}
