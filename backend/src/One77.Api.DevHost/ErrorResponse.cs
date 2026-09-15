namespace One77.Api.DevHost;

/// <summary>Shared HTTP error body, used by the single global exception-handler middleware in Program.cs.</summary>
public sealed class ErrorResponse
{
    public string Error { get; set; } = "";
}
