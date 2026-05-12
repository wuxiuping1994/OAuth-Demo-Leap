namespace OAuthDemoLeap.Services.PkceService;

public interface IPkceService
{
    (string CodeVerifier, string CodeChallenge) GeneratePkce();
    string GenerateState();
}
