namespace Smart.Data.Accessor.Builders.Generator.Models;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

// spec §7.11 (P4): equatable diagnostic carrier for the Builder generators. The FAWMN transform
// (symbol stage) cannot call SourceProductionContext.ReportDiagnostic, so diagnostics are collected
// as value data on the model and replayed at the output stage. Mirrors the core generator's
// DiagnosticData (the Builder is a separate assembly, so it needs its own copy).
internal sealed record BuilderDiagnosticData(
    DiagnosticDescriptor Descriptor,
    BuilderLocationInfo? Location,
    EquatableArray<string> Args)
{
    public static BuilderDiagnosticData Create(DiagnosticDescriptor descriptor, Location? location, params object?[] args)
        => new(
            descriptor,
            BuilderLocationInfo.From(location),
            new EquatableArray<string>(args.Select(static a => a?.ToString() ?? string.Empty).ToArray()));

    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(Descriptor, Location?.ToLocation(), Args);
}

// Equatable representation of a Roslyn Location (not value-equatable), reconstructed at the output stage.
internal sealed record BuilderLocationInfo(
    string FilePath,
    TextSpan TextSpan,
    LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static BuilderLocationInfo? From(Location? location)
    {
        if (location?.SourceTree is null)
        {
            return null;
        }
        return new BuilderLocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
}
