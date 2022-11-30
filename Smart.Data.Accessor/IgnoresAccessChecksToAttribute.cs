// ReSharper disable CheckNamespace
namespace System.Runtime.CompilerServices;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class IgnoresAccessChecksToAttribute : Attribute
{
    public string AssemblyName { get; }

    public IgnoresAccessChecksToAttribute(string assemblyName)
    {
        AssemblyName = assemblyName;
    }
}
