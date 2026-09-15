using System;

namespace One77.Core.Learn
{
    /// <summary>Maps to HTTP 404 at the API layer.</summary>
    public sealed class LearnNotFoundException : Exception
    {
        public LearnNotFoundException(string message) : base(message) { }
    }

    /// <summary>Maps to HTTP 400 at the API layer.</summary>
    public sealed class LearnValidationException : Exception
    {
        public LearnValidationException(string message) : base(message) { }
    }
}
