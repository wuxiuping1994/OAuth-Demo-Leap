using Microsoft.AspNetCore.Mvc;
using OAuthDemoLeap.Models;
using OAuthDemoLeap.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace OAuthDemoLeap.Controllers
{
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly OAuthConfiguration _config;
        private readonly PkceService _pkceService;
        private readonly TokenExchangeService _tokenExchangeService;
        private readonly TokenValidationService _tokenValidationService;

        public OAuthController(IOptions<OAuthConfiguration> options, PkceService pkceService, TokenExchangeService tokenExchangeService, TokenValidationService tokenValidationService)
        {
            _config = options.Value;
            _pkceService = pkceService;
            _tokenExchangeService = tokenExchangeService;
            _tokenValidationService = tokenValidationService;
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

            var handler = new JsonWebTokenHandler();
            var jwt = handler.ReadJsonWebToken(idToken);
            var claims = jwt.Claims.ToDictionary(c => c.Type, c => c.Value);
            
            return Ok(claims);
        }
    }
}
