using System;

namespace One77.Core.Enrichment
{
    /// <summary>Maps to HTTP 404 at the API layer.</summary>
    public sealed class EnrichmentNotFoundException : Exception
    {
        public EnrichmentNotFoundException(string message) : base(message) { }
    }

    /// <summary>Maps to HTTP 400 at the API layer.</summary>
    public sealed class EnrichmentValidationException : Exception
    {
        public EnrichmentValidationException(string message) : base(message) { }
    }
}
