namespace OAuthDemoLeap.Services;

public interface ITokenValidationService
{
    Task<bool> ValidateToken(string token);
    Dictionary<string, string> GetClaims(string token);
}
