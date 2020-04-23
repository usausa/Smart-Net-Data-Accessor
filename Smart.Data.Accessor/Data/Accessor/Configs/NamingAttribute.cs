namespace Smart.Data.Accessor.Configs
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter)]

    public abstract class NamingAttribute : Attribute
    {
        public abstract Func<string, string> GetNaming();
    }

    public sealed class SnakeNamingAttribute : NamingAttribute
    {
        public override Func<string, string> GetNaming() => Naming.Snake;
    }

    public sealed class UpperSnakeNamingAttribute : NamingAttribute
    {
        public override Func<string, string> GetNaming() => Naming.UpperSnake;
    }

    public sealed class CamelNamingAttribute : NamingAttribute
    {
        public override Func<string, string> GetNaming() => Naming.Camel;
    }

    public sealed class DefaultNamingAttribute : NamingAttribute
    {
        public override Func<string, string> GetNaming() => Naming.Default;
    }
}
