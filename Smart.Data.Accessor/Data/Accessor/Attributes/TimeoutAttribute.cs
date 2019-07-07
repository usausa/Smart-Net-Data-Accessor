namespace Smart.Data.Accessor.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TimeoutAttribute : Attribute
    {
        public int Timeout { get; }

        public TimeoutAttribute(int timeout)
        {
            Timeout = timeout;
        }
    }
}
