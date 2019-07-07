namespace Smart.Data.Accessor.Generator
{
    public sealed class BuildError
    {
        public string Id { get; }

        public int Start { get; }

        public int End { get; }

        public string Message { get; }

        public BuildError(string id, int start, int end, string message)
        {
            Id = id;
            Start = start;
            End = end;
            Message = message;
        }
    }
}
