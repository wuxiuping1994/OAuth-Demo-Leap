using Microsoft.AspNetCore.Mvc;

namespace OAuthDemoLeap.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OAuthController : ControllerBase
    {
        private readonly ILogger<OAuthController> _logger;

        public OAuthController(ILogger<OAuthController> logger)
        {
            _logger = logger;
        }
    }
}
