using Microsoft.AspNetCore.Mvc;
using OAuthDemoLeap.Services;

namespace OAuthDemoLeap.Controllers
{
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly PkceService _pkceService;
        private readonly TokenExchangeService _tokenExchangeService;
        private readonly TokenValidationService _tokenValidationService;

        public OAuthController(PkceService pkceService, TokenExchangeService tokenExchangeService, TokenValidationService tokenValidationService)
        {
            _pkceService = pkceService;
            _tokenExchangeService = tokenExchangeService;
            _tokenValidationService = tokenValidationService;
        }

        [HttpGet("/login")]
        public IActionResult Login()
        {
            (string codeVerifier, string codeChallenge) = _pkceService.GeneratePkce();
            var state = _pkceService.GenerateState();

            HttpContext.Session.SetString("oauth_state", state);
            HttpContext.Session.SetString("pkce_code_verifier", codeVerifier);

            var url = _pkceService.GenerateRedirectUri(codeChallenge, state);
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
        public IActionResult GetData()
        {
            var storedAccessToken = HttpContext.Session.GetString("access_token");
            if (storedAccessToken == null)
                return Unauthorized();

            return Ok("You're authorized");
        }
    }
}
