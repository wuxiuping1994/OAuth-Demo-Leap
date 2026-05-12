using OAuthDemoLeap.Models;

namespace OAuthDemoLeap.Services.TokenExchangeService;

public interface ITokenExchangeService
{
    Task<TokenResponse> ExchangeAsync(string code, string codeVerifier);
}
