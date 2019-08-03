namespace Smart.Data.Accessor.Scripts
{
    using System;
    using System.Collections.Generic;

    using Xunit;

    public class ScriptHelperTest
    {
        [Fact]
        public void CoverageFix()
        {
            Assert.True(ScriptHelper.IsNull(null));
            Assert.False(ScriptHelper.IsNull(string.Empty));

            Assert.False(ScriptHelper.IsNotNull(null));
            Assert.True(ScriptHelper.IsNotNull(string.Empty));

            Assert.True(ScriptHelper.IsEmpty(null));
            Assert.True(ScriptHelper.IsEmpty(string.Empty));
            Assert.False(ScriptHelper.IsEmpty("x"));

            Assert.False(ScriptHelper.IsNotEmpty(null));
            Assert.False(ScriptHelper.IsNotEmpty(string.Empty));
            Assert.True(ScriptHelper.IsNotEmpty("x"));

            Assert.False(ScriptHelper.Any(null));
            Assert.False(ScriptHelper.Any(Array.Empty<int>()));
            Assert.True(ScriptHelper.Any(new[] { 1 }));

            Assert.False(ScriptHelper.Any((List<int>)null));
            Assert.False(ScriptHelper.Any(new List<int>()));
            Assert.True(ScriptHelper.Any(new List<int> { 1 }));
        }
    }
}
