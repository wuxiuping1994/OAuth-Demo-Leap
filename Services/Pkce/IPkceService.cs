namespace OAuthDemoLeap.Services;

public interface IPkceService
{
    (string CodeVerifier, string CodeChallenge) GeneratePkce();
    string GenerateState();
    string GenerateRedirectUri(string codeChallenge, string state);
}
