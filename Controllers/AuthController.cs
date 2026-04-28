using BasisBank.Identity.Api.DTOs.Requests;
using BasisBank.Identity.Api.DTOs.Responses;
using BasisBank.Identity.Api.Exceptions;
using BasisBank.Identity.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BasisBank.Identity.Api.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        public AuthController(IAuthService authService, ILogger<AuthController> logger) {
            _authService = authService;
            _logger = logger;
        }
        [HttpGet("dev-token")]
#if DEBUG
        public IActionResult GetDevToken(string userName, string password) {
            var token = _authService.GenerateDevToken(userName, password);
            return Ok(new { AccessToken = token });
        }
#endif
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenReq? req) {
            await _authService.LogoutAsync(req);
            return Ok(new { message = "სისტემიდან გამოსვლა წარმატებულია." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenReq? req) {
            var result = await _authService.RefreshTokenAsync(req);
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] SignUpReq model) {
            var result = await _authService.RegisterUserAsync(model);

            if (!result.Success) {
                return BadRequest(result.Message);
            }

            return Ok(result.Message);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SignInReq? req) {
            var res = await _authService.LoginAsync(req, User);
            return Ok(new ApiResponse<LoginRes>(res));
        }

        [Authorize]
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpReq sendOtpReq) {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new ApiException(ApiErrorCode.Unauthorized, "Unauthorized.", 401);

            var result = await _authService.SendOtpAsync(sendOtpReq, User);
            if (result == null)
                return BadRequest(new { message = "Invalid OTP or expired" });
            return Ok(new { TicketId = result.OtpId });
        }

        [Authorize]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpReq verifyOtpReq) {
            var result = await _authService.VerifyOtpAndGetIdAsync(verifyOtpReq, User);
            if (result == null)
                return BadRequest(new { message = "Invalid OTP or expired" });

            return Ok(new { verificationId = result.VerificationId });
        }
    }
}