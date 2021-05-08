namespace Smart.Data.Accessor.Configs
{
    using Xunit;

    public class NamingTest
    {
        public interface INamingAccessor
        {
            [SnakeNaming]
            int ExecuteSnake(int abcXyz);

            [UpperSnakeNaming]
            int ExecuteUpperSnake(int abcXyz);

            [CamelNaming]
            int ExecuteCamel(int abcXyz);

            [DefaultNaming]
            int ExecuteDefault(int abcXyz);
        }

        [Fact]
        public void TestNamings()
        {
            var type = typeof(INamingAccessor);

            var miSnake = type.GetMethod(nameof(INamingAccessor.ExecuteSnake))!;
            Assert.Equal("abc_xyz", ConfigHelper.GetMethodParameterColumnName(miSnake, miSnake.GetParameters()[0]));

            var miUpperSnake = type.GetMethod(nameof(INamingAccessor.ExecuteUpperSnake))!;
            Assert.Equal("ABC_XYZ", ConfigHelper.GetMethodParameterColumnName(miUpperSnake, miUpperSnake.GetParameters()[0]));

            var miCamel = type.GetMethod(nameof(INamingAccessor.ExecuteCamel))!;
            Assert.Equal("abcXyz", ConfigHelper.GetMethodParameterColumnName(miCamel, miCamel.GetParameters()[0]));

            var miDefault = type.GetMethod(nameof(INamingAccessor.ExecuteDefault))!;
            Assert.Equal("AbcXyz", ConfigHelper.GetMethodParameterColumnName(miDefault, miDefault.GetParameters()[0]));
        }
    }
}
