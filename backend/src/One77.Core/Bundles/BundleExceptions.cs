using System;

namespace One77.Core.Bundles
{
    /// <summary>Maps to HTTP 404 at the API layer.</summary>
    public sealed class BundleNotFoundException : Exception
    {
        public BundleNotFoundException(string message) : base(message) { }
    }

    /// <summary>Maps to HTTP 400 at the API layer.</summary>
    public sealed class BundleValidationException : Exception
    {
        public BundleValidationException(string message) : base(message) { }
    }
}
