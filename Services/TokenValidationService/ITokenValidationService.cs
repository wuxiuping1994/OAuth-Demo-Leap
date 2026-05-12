namespace OAuthDemoLeap.Services.TokenValidationService;

public interface ITokenValidationService
{
    Task<bool> ValidateToken(string token);
    Dictionary<string, string> GetClaims(string token);
}
