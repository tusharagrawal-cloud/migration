using System;

namespace One77.Core.Homepage
{
    /// <summary>Maps to HTTP 400 at the API layer — an upload that failed validation (not an image, wrong format, too large).</summary>
    public sealed class HomepageValidationException : Exception
    {
        public HomepageValidationException(string message) : base(message) { }
    }
}
