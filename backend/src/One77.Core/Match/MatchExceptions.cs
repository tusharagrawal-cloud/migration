using System;

namespace One77.Core.Match
{
    /// <summary>Maps to HTTP 404 at the API layer.</summary>
    public sealed class MatchNotFoundException : Exception
    {
        public MatchNotFoundException(string message) : base(message) { }
    }

    /// <summary>Maps to HTTP 400 at the API layer.</summary>
    public sealed class MatchValidationException : Exception
    {
        public MatchValidationException(string message) : base(message) { }
    }
}
