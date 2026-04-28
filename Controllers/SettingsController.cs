using BasisBank.Identity.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BasisBank.Identity.Api.Controllers {
    [Authorize(Policy = "MfaRequired")] // წვდომა მხოლოდ "ოქროს გასაღებით" (MFA)
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase {
        private readonly UserManager<ApplicationUser> _userManager;

        public SettingsController(UserManager<ApplicationUser> userManager) {
            _userManager = userManager;
        }

        [HttpGet("mfa-status")]
        [Authorize(Policy = "MfaRequired")]
        public async Task<IActionResult> GetMfaStatus() {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) {
                return Unauthorized();
            }

            return Ok(new {
                isTwoFactorEnabled = user.TwoFactorEnabled
            });
        }
    }
}