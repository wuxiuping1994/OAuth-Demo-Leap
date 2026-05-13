using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuthDemoLeap.Models;
using OAuthDemoLeap.Services.PkceService;
using OAuthDemoLeap.Services.TokenExchangeService;
using OAuthDemoLeap.Services.TokenValidationService;

namespace OAuthDemoLeap.Controllers
{
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly IPkceService _pkceService;
        private readonly ITokenExchangeService _tokenExchangeService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly OAuthConfiguration _config;

        public OAuthController(IPkceService pkceService, ITokenExchangeService tokenExchangeService, ITokenValidationService tokenValidationService, IOptions<OAuthConfiguration> options)
        {
            _pkceService = pkceService;
            _tokenExchangeService = tokenExchangeService;
            _tokenValidationService = tokenValidationService;
            _config = options.Value;
        }

        [HttpGet("/login")]
        public IActionResult Login()
        {
            (string codeVerifier, string codeChallenge) = _pkceService.GeneratePkce();
            var state = _pkceService.GenerateState();

            HttpContext.Session.SetString("oauth_state", state);
            HttpContext.Session.SetString("pkce_code_verifier", codeVerifier);

            var url = $"{_config.AuthorizationEndpoint}?response_type=code&client_id={_config.ClientId}&redirect_uri={Uri.EscapeDataString(_config.RedirectUri!)}&scope={_config.Scope}&code_challenge={codeChallenge}&state={state}&code_challenge_method=S256";
            return Redirect(url);
        }

        [HttpGet("/callback")]
        public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
        {
            if (error != null)
                return BadRequest($"Authorization failed: {error}");
            if (code == null || state == null)
                return BadRequest("Missing code or state");
            if (state != HttpContext.Session.GetString("oauth_state"))
                return BadRequest("Invalid state");

            var codeVerifier = HttpContext.Session.GetString("pkce_code_verifier");
            if (codeVerifier == null)
                return BadRequest("Session expired");
            
            try
            {  
                var tokenResponse = await _tokenExchangeService.ExchangeAsync(code, codeVerifier);
                var idTokenValidated = await _tokenValidationService.ValidateToken(tokenResponse.IdToken!);

                if (!idTokenValidated)
                    return Unauthorized("Invalid id_token");

                HttpContext.Session.SetString("access_token", tokenResponse.AccessToken!);
                HttpContext.Session.SetString("id_token", tokenResponse.IdToken!);
            }
            catch (HttpRequestException)
            {
                return BadRequest("Token exchange failed");
            }

            return Ok();
        }

        [HttpGet("/me")]
        public IActionResult Me()
        {
            var idToken = HttpContext.Session.GetString("id_token");
            if (idToken == null)
                return Unauthorized("Not logged in");

            var claims = _tokenValidationService.GetClaims(idToken);
            return Ok(claims);
        }

        [HttpGet("/api/data")]
        public async Task<IActionResult> GetData()
        {
            var storedAccessToken = HttpContext.Session.GetString("access_token");
            if (storedAccessToken == null)
                return Unauthorized("You're unauthorized to accesss the resources");

            var isValid = await _tokenValidationService.ValidateToken(storedAccessToken);
            if (!isValid)
                return Unauthorized("Access token expired or invalid");
            return Ok("You're authorized");
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            var idToken = HttpContext.Session.GetString("id_token");
            HttpContext.Session.Clear();
            var logoutUrl = $"{_config.EndSessionEndpoint}?id_token_hint={idToken}&post_logout_redirect_uri={Uri.EscapeDataString(_config.RedirectUri!.Replace("/callback", "/login"))}";
            
            return Redirect(logoutUrl);
        }
    }
}
