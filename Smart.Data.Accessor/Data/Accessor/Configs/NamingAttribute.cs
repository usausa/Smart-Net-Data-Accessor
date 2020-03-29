namespace Smart.Data.Accessor.Configs
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter)]

    public abstract class NamingAttribute : Attribute
    {
        public abstract Func<string, string> GetNaming();
    }

    public abstract class SnakeNamingAttribute : NamingAttribute
    {
        public override Func<string, string> GetNaming() => Naming.Snake;
    }

    public abstract class UpperSnakeNamingAttribute : NamingAttribute
    {
        public override Func<string, string> GetNaming() => Naming.UpperSnake;
    }

    public abstract class CamelNamingAttribute : NamingAttribute
    {
        public override Func<string, string> GetNaming() => Naming.Camel;
    }

    public abstract class DefaultNamingAttribute : NamingAttribute
    {
        public override Func<string, string> GetNaming() => Naming.Default;
    }
}
