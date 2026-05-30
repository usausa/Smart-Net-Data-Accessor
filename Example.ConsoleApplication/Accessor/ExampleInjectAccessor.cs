namespace Example.ConsoleApplication.Accessor;

using System.Collections.Generic;

using Example.ConsoleApplication.Models;

using Smart.Data.Accessor.Attributes;

internal interface IExampleLogger
{
    void Log(string message);

    int Count { get; }
}

[DataAccessor]
[Inject(typeof(IExampleLogger), "logger")]
internal sealed partial class ExampleInjectAccessor
{
    [Query]
    public partial IReadOnlyList<DataEntity> QueryAll();

    public string GetLoggerTypeName() => this.logger.GetType().Name;

    public int CallLoggerAndCount(string message)
    {
        this.logger.Log(message);
        return this.logger.Count;
    }
}
