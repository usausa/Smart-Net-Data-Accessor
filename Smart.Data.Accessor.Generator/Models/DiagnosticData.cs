namespace Smart.Data.Accessor.Generator.Models;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

// spec §7.11 (1-C): equatable diagnostic carrier. The symbol-analysis stage (FAWMN transform) and
// the SQL-parse stage cannot call SourceProductionContext.ReportDiagnostic, so diagnostics are
// collected as value data and replayed at the output stage. SourceGenerateHelper's DiagnosticInfo
// holds only a single message arg; this domain-specific carrier preserves multi-arg diagnostics.
internal sealed record DiagnosticData(
    DiagnosticDescriptor Descriptor,
    SourceLocationInfo? Location,
    EquatableArray<string> Args)
{
    public static DiagnosticData Create(DiagnosticDescriptor descriptor, Location? location, params object?[] args)
        => new(
            descriptor,
            SourceLocationInfo.From(location),
            new EquatableArray<string>(args.Select(static a => a?.ToString() ?? string.Empty).ToArray()));

    // Overload for the SQL / output stage, which holds an already-equatable SourceLocationInfo (from the model).
    public static DiagnosticData Create(DiagnosticDescriptor descriptor, SourceLocationInfo? location, params object?[] args)
        => new(
            descriptor,
            location,
            new EquatableArray<string>(args.Select(static a => a?.ToString() ?? string.Empty).ToArray()));

    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(Descriptor, Location?.ToLocation(), Args.AsArray());
}

// Equatable representation of a Roslyn Location (which is not value-equatable). Reconstructed into a
// Location at the output stage for Diagnostic.Create. Null when the source location has no syntax tree.
internal sealed record SourceLocationInfo(
    string FilePath,
    TextSpan TextSpan,
    LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static SourceLocationInfo? From(Location? location)
    {
        if (location?.SourceTree is null)
        {
            return null;
        }
        return new SourceLocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
}
