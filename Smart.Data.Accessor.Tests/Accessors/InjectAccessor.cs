namespace Smart.Data.Accessor.Tests.Accessors;

using System.Collections.Generic;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

internal interface ICounter
{
    int Next();
}

// [Inject] adds a constructor parameter + same-named field. The generated field
// `counter` is referenced by the user-written partial method below.
[DataAccessor]
[Inject(typeof(ICounter), "counter")]
internal sealed partial class InjectAccessor
{
    [Query]
    public partial IReadOnlyList<DataEntity> QueryAll(DbConnection con);

    public int UseInjected() => counter.Next();
}
