namespace Smart.Data.Accessor.Engine
{
    using System;

    public struct ColumnInfo : IEquatable<ColumnInfo>
    {
        public string Name { get; }

        public Type Type { get; }

        public ColumnInfo(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1008:OpeningParenthesisMustBeSpacedCorrectly", Justification = "Ignore")]
        public override int GetHashCode() => (Name, Type).GetHashCode();

        public override bool Equals(object obj) => obj is ColumnInfo other && Equals(other);

        public bool Equals(ColumnInfo other) => Name == other.Name && Type == other.Type;

        public static bool operator ==(ColumnInfo x, ColumnInfo y) => x.Equals(y);

        public static bool operator !=(ColumnInfo x, ColumnInfo y) => !x.Equals(y);
    }
}
