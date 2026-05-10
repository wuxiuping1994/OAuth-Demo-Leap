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

        public OAuthController(IOptions<OAuthConfiguration> options, PkceService pkceService)
        {
            _config = options.Value;
            _pkceService = pkceService;
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
    }
}
