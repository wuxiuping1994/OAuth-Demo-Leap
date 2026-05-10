using Microsoft.AspNetCore.Mvc;
using OAuthDemoLeap.Models;
using OAuthDemoLeap.Services;
using Microsoft.Extensions.Options;

namespace OAuthDemoLeap.Controllers
{
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly OAuthConfiguration _config;
        private readonly PkceService _pkceService;
        private readonly TokenExchangeService _tokenExchangeService;

        public OAuthController(IOptions<OAuthConfiguration> options, PkceService pkceService, TokenExchangeService tokenExchangeService)
        {
            _config = options.Value;
            _pkceService = pkceService;
            _tokenExchangeService = tokenExchangeService;
        }

        [HttpGet("/login")]
        public IActionResult Login()
        {
            (string CodeVerifier, string CodeChallenge) = _pkceService.GeneratePkce();
            var state = _pkceService.GenerateState();

            HttpContext.Session.SetString("oauth_state", state);
            HttpContext.Session.SetString("pkce_code_verifier", CodeVerifier);

            var url = $"{_config.AuthorizationEndpoint}?response_type=code&client_id={_config.ClientId}&redirect_uri={Uri.EscapeDataString(_config.RedirectUri!)}&scope={_config.Scope}&code_challenge={CodeChallenge}&state={state}&code_challenge_method=S256";
            
            return Redirect(url);
        }

        [HttpGet("/callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
        {
            if (error != null)
            {
                return BadRequest($"Authorization failed: {error}");
            }
            if (code == null || state == null)
            {
                return BadRequest("Missing code or state");
            }
            if (state != HttpContext.Session.GetString("oauth_state"))
            {
                return BadRequest("Invalid state");
            }

            var codeVerifier = HttpContext.Session.GetString("pkce_code_verifier");
            if (codeVerifier == null)
            {
                return BadRequest("Session expired");
            }
            try
            {  
                var tokenResponse = await _tokenExchangeService.ExchangeAsync(code, codeVerifier);
                HttpContext.Session.SetString("access_token", tokenResponse.AccessToken!);
                HttpContext.Session.SetString("id_token", tokenResponse.IdToken!);
            }
            catch (HttpRequestException)
            {
                return BadRequest("Token exchange failed");
            }

            return Ok();
        }
    }
}
