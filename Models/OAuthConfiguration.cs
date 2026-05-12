namespace OAuthDemoLeap.Models;

public class OAuthConfiguration
{
    public string? ClientId { get; set; }
    public string? RedirectUri { get; set; }
    public string? Scope { get; set; }
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? UserinfoEndpoint { get; set; }
    public string? JwksUri { get; set; }
    public string? Issuer { get; set; }
    public string? EndSessionEndpoint { get; set; }
}
