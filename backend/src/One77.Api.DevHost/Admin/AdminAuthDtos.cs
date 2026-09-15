namespace One77.Api.DevHost.Admin;

public sealed class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = "";
    public string TokenType { get; set; } = "Bearer";
    public int AdminUserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class AdminMeResponse
{
    public int AdminUserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
}
