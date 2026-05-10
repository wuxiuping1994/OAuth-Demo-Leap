using Microsoft.Extensions.Options;
using OAuthDemoLeap.Models;

namespace OAuthDemoLeap.Services;

public class TokenExchangeService
{
    private readonly HttpClient _httpClient;
    private readonly OAuthConfiguration _config;

    public TokenExchangeService(HttpClient httpClient, IOptions<OAuthConfiguration> options)
    {
        _httpClient = httpClient;
        _config = options.Value;
    }
    public async Task<TokenResponse> ExchangeAsync(string code, string codeVerifier)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", _config.RedirectUri! },
            { "client_id", _config.ClientId! },
            { "code_verifier", codeVerifier }
        });
        var response = await _httpClient.PostAsync(_config.TokenEndpoint, body);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (tokenResponse == null)
        {
            throw new Exception("Empty token response");
        }
        
        return tokenResponse;
    }
}