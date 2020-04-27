namespace Smart.Data.Accessor.Configs
{
    using Smart.Data.Accessor.Attributes;

    using Xunit;

    public class ConfigHelperTest
    {
        //--------------------------------------------------------------------------------
        // Table
        //--------------------------------------------------------------------------------

        public class DataEntity
        {
        }

        public class DataSuffixNoMatch
        {
        }

        [Name("T_DATA")]
        public class DataWithNameEntity
        {
        }

        [EntitySuffix("Data")]
        public class DataWithSuffixData
        {
        }

        [Fact]
        public void TestNamings()
        {
            var mi = GetType().GetMethod(nameof(TestNamings));

            Assert.Equal("Data", ConfigHelper.GetMethodTableName(mi, typeof(DataEntity)));
            Assert.Equal("DataSuffixNoMatch", ConfigHelper.GetMethodTableName(mi, typeof(DataSuffixNoMatch)));
            Assert.Equal("T_DATA", ConfigHelper.GetMethodTableName(mi, typeof(DataWithNameEntity)));
            Assert.Equal("DataWithSuffix", ConfigHelper.GetMethodTableName(mi, typeof(DataWithSuffixData)));
        }
    }
}
