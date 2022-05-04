namespace Smart.Data.Accessor.Generator;

using Smart.Data.Accessor.Generator.Helpers;

using Xunit;

public class ParameterNamesTest
{
    [Fact]
    public void TestGetParameterNames()
    {
        Assert.Equal("p0", ParameterNames.GetParameterName(0));
        Assert.Equal("p255", ParameterNames.GetParameterName(255));
        Assert.Equal("p256", ParameterNames.GetParameterName(256));
    }
}
