namespace One77.Api.WebApi48
{
    /// <summary>Shared HTTP error body — same shape as One77.Api.DevHost's ErrorResponse, for contract parity.</summary>
    public sealed class ErrorResponse
    {
        public string Error { get; set; } = "";
    }
}
