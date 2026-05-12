using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OAuthDemoLeap.Models;

namespace OAuthDemoLeap.Services.TokenValidationService;

public class TokenValidationService : ITokenValidationService
{
    private readonly OAuthConfiguration _config;
    private readonly HttpClient _httpClient;
    public TokenValidationService(IOptions<OAuthConfiguration> options, HttpClient httpClient)
    {
        _config = options.Value;
        _httpClient = httpClient;
    }

    public async Task<bool> ValidateToken(string idToken)
    {
        var response = await _httpClient.GetAsync(_config.JwksUri);
        var jwksJson = await response.Content.ReadAsStringAsync();

        var jwks = new JsonWebKeySet(jwksJson);
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = _config.Issuer,
            ValidAudience = _config.ClientId,
            IssuerSigningKeys = jwks.GetSigningKeys(),
            ValidateIssuer = true,  //checks iss
            ValidateAudience = true,  //checks aud
            ValidateLifetime = true,  //checks exp
            ValidateIssuerSigningKey = true
        };

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(idToken, validationParameters);

        return result.IsValid;
    }

    public Dictionary<string, string> GetClaims(string idToken)
    {
        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(idToken);
        var claims = jwt.Claims.ToDictionary(c => c.Type, c => c.Value);

        return claims;
    }
}
