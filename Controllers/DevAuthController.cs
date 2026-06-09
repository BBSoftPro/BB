#if DEBUG
using BasisBank.Identity.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BasisBank.Identity.Api.Controllers {
    [ApiController]
    [Route("api/dev")]
    public class DevAuthController : ControllerBase {
        private readonly IAuthService _authService;
        private readonly ILogger<DevAuthController> _logger;

        public DevAuthController(IAuthService authService, ILogger<DevAuthController> logger) {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("token")]
        public async Task<IActionResult> GenerateDevToken([FromBody] DevTokenRequest req) {
            if (string.IsNullOrEmpty(req.UserName) || string.IsNullOrEmpty(req.Password))
                return BadRequest("userName and password required");

            try {
                var token = await _authService.GenerateDevToken(req.UserName, req.Password);
                return Ok(new { accessToken = token });
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Dev token generation failed");
                return BadRequest(new { error = ex.Message });
            }
        }

        public class DevTokenRequest {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
#endif
