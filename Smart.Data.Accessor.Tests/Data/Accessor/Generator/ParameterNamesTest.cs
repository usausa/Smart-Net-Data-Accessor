namespace Smart.Data.Accessor.Generator
{
    using Smart.Data.Accessor.Generator.Helpers;

    using Xunit;

    public class ParameterNamesTest
    {
        [Fact]
        public void TestGetParameterNames()
        {
            Assert.Equal("_p0", ParameterNames.GetParameterName(0));
            Assert.Equal("_p255", ParameterNames.GetParameterName(255));
            Assert.Equal("_p256", ParameterNames.GetParameterName(256));
        }
    }
}
