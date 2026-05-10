using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OAuthDemoLeap.Models;

namespace OAuthDemoLeap.Services;

public class PkceService
{
    private readonly OAuthConfiguration _config;

    public PkceService(IOptions<OAuthConfiguration> options)
    {
        _config = options.Value;
    }

    public string GenerateRedirectUri(string codeChallenge, string state)
    {
        return $"{_config.AuthorizationEndpoint}?response_type=code&client_id={_config.ClientId}&redirect_uri={Uri.EscapeDataString(_config.RedirectUri!)}&scope={_config.Scope}&code_challenge={codeChallenge}&state={state}&code_challenge_method=S256";
    }
    // Generates a cryptographically random code_verifier and its S256 code_challenge
    public (string CodeVerifier, string CodeChallenge) GeneratePkce()
    {
        // 32 random bytes -> 43 base64url characters (within the 43-128 char requirement)
        var verifierBytes = RandomNumberGenerator.GetBytes(32);
        var codeVerifier = Base64UrlEncode(verifierBytes);

        // code_challenge = BASE64URL(SHA256(ASCII(code_verifier)))
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var codeChallenge = Base64UrlEncode(challengeBytes);

        return (codeVerifier, codeChallenge);
    }

    // Generates a cryptographically random state value to prevent CSRF
    public string GenerateState()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    // Base64url encoding (no padding, + -> -, / -> _)
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
