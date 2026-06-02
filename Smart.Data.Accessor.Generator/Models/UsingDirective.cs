namespace Smart.Data.Accessor.Generator.Models;

// spec §1.4 F12 / §6.3: /*!helper Foo.Bar */ → using static Foo.Bar;
// /*!using Foo */ → using Foo; Aggregated per-Accessor at file header emission.
internal sealed record UsingDirective(
    bool IsStatic,
    string Name);
