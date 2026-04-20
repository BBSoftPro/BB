using BasisBank.Identity.Api.DTOs.Requests;
using BasisBank.Identity.Api.DTOs.Responses;
using System.Security.Claims;

namespace BasisBank.Identity.Api.Interfaces {
    public interface IAuthService {
        Task<AuthRes> RegisterUserAsync(SignUpReq req);
        Task<LoginRes> LoginAsync(SignInReq? model, ClaimsPrincipal? currentUser);
        Task<OtpRes> SendOtpAsync(SendOtpReq sendOtpReq, ClaimsPrincipal? currentUser);
        Task<VerifyOtpRes> VerifyOtpAndGetIdAsync(VerifyOtpReq verifyOtpReq, ClaimsPrincipal? currentUser);
        Task<LoginRes> RefreshTokenAsync(RefreshTokenReq? refreshTokenReq);
        Task LogoutAsync(RefreshTokenReq? req);
#if DEBUG
        Task<string> GenerateDevToken(string userName, string password);
#endif
    }
}