namespace Smart.Data.Accessor.Generator.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Smart.Data.Accessor generator Roslyn symbol extension methods (not candidates for SourceGenerateHelper promotion).
internal static class AccessorSymbolExtensions
{
    // Locates the primary constructor of a record: an instance ctor whose DeclaringSyntaxReferences point to a
    // RecordDeclarationSyntax (i.e. synthesized from the positional record declaration, not a separate ConstructorDeclarationSyntax).
    public static bool TryGetRecordPrimaryConstructor(this INamedTypeSymbol entity, out IMethodSymbol primaryCtor)
    {
        foreach (var ctor in entity.InstanceConstructors)
        {
            if (ctor.Parameters.IsDefaultOrEmpty)
            {
                continue;
            }
            foreach (var declRef in ctor.DeclaringSyntaxReferences)
            {
                if (declRef.GetSyntax() is RecordDeclarationSyntax)
                {
                    primaryCtor = ctor;
                    return true;
                }
            }
        }
        primaryCtor = null!;
        return false;
    }
}
