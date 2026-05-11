using OAuthDemoLeap.Models;

namespace OAuthDemoLeap.Services;

public interface ITokenExchangeService
{
    Task<TokenResponse> ExchangeAsync(string code, string codeVerifier);
}
