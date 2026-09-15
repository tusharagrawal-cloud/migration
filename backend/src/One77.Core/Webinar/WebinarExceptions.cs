using System;

namespace One77.Core.Webinar
{
    /// <summary>Maps to HTTP 404 at the API layer.</summary>
    public sealed class WebinarNotFoundException : Exception
    {
        public WebinarNotFoundException(string message) : base(message) { }
    }

    /// <summary>Maps to HTTP 400 at the API layer.</summary>
    public sealed class WebinarValidationException : Exception
    {
        public WebinarValidationException(string message) : base(message) { }
    }
}
