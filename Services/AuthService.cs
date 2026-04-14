using BasisBank.Identity.Api.Data;
using BasisBank.Identity.Api.DTOs.Requests;
using BasisBank.Identity.Api.DTOs.Responses;
using BasisBank.Identity.Api.Entities;
using BasisBank.Identity.Api.Enums;
using BasisBank.Identity.Api.Exceptions;
using BasisBank.Identity.Api.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace BasisBank.Identity.Api.Services {
    public class AuthService : IAuthService {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ApplicationDbContext context,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor) // დაემატა ინჟექცია
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogoutAsync(RefreshTokenReq? req) {
            if (req == null || string.IsNullOrEmpty(req.RefreshToken)) {
                return;
            }

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == req.RefreshToken);

            if (storedToken != null) {
                storedToken.IsRevoked = true;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<LoginRes> RefreshTokenAsync(RefreshTokenReq? req) {
            // 1. ვალიდაცია
            if (req == null || string.IsNullOrEmpty(req.RefreshToken)) {
                throw new ApiException(ApiErrorCode.BadRequest, "მოთხოვნა არავალიდურია.", 400);
            }

            // 2. მოძებნა ბაზაში
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == req.RefreshToken);

            // 3. შემოწმება
            if (storedToken == null || storedToken.IsUsed || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow) {
                throw new ApiException(ApiErrorCode.TokenExpired, "სესია ამოწურულია. გთხოვთ გაიაროთ ავტორიზაცია.", 401);
            }

            // 4. იუზერის ამოღება
            var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
            if (user == null) {
                throw new ApiException(ApiErrorCode.Unauthorized, "მომხმარებელი ვერ მოიძებნა.", 401);
            }

            // 5. ძველი ტოკენის დახურვა
            storedToken.IsUsed = true;
            _context.RefreshTokens.Update(storedToken);

            // 6. ახალი წყვილის გაცემა
            var roles = await _userManager.GetRolesAsync(user);
            return await GenerateFullAuth(user, roles, true);
        }
        public async Task<AuthRes> RegisterUserAsync(SignUpReq req) {
            // 1. IdentificationId ვალიდაცია (ზუსტად 11 ციფრი)
            if (!string.IsNullOrWhiteSpace(req.IdentificationId)) {
                if (!System.Text.RegularExpressions.Regex.IsMatch(req.IdentificationId, @"^\d{11}$")) {
                    return new AuthRes { Success = false, Message = "IdentificationId უნდა შედგებოდეს ზუსტად 11 ციფრისგან." };
                }
            }

            // 2. Passport ვალიდაცია (9 სიმბოლო: ლათინური ასოები ან ციფრები)
            if (!string.IsNullOrWhiteSpace(req.Passport)) {
                if (!System.Text.RegularExpressions.Regex.IsMatch(req.Passport, @"^[a-zA-Z0-9]{9}$")) {
                    return new AuthRes { Success = false, Message = "Passport უნდა შედგებოდეს 9 ლათინური სიმბოლოსგან ან ციფრისგან." };
                }
            }

            // 3. მინიმუმ ერთ-ერთი აუცილებელია
            if (string.IsNullOrWhiteSpace(req.IdentificationId) && string.IsNullOrWhiteSpace(req.Passport)) {
                return new AuthRes { Success = false, Message = "IdentificationId ან Passport აუცილებელია." };
            }

            var user = new ApplicationUser {
                UserName = req.UserName,
                Email = req.Email,
                FirstName = req.FirstName,
                LastName = req.LastName,
                IdentificationId = req.IdentificationId,
                Passport = req.Passport
            };

            var result = await _userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded) {
                return new AuthRes { Success = false, Message = string.Join(", ", result.Errors.Select(e => e.Description)) };
            }

            return new AuthRes { Success = true, Message = "მომხმარებელი წარმატებით დარეგისტრირდა." };
        }

        public async Task<LoginRes> LoginAsync(SignInReq? signInReq, ClaimsPrincipal? currentUser) {
            if (signInReq == null) {
                throw new ApiException(ApiErrorCode.BadRequest, "BadRequest", 400);
            }
            ApplicationUser? user;

            if (signInReq.VerificationId.HasValue) {
                var jti = currentUser?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                var userIdClaim = currentUser?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (currentUser == null || string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) {
                    throw new ApiException(ApiErrorCode.Unauthorized, "Unauthorized", 401);
                }

                var ticket = await _context.AuthTickets.FirstOrDefaultAsync(t =>
                    t.Id == signInReq.VerificationId.Value &&
                    t.SourceTokenId == jti &&
                    t.UserId == userId &&
                    t.IsVerified == true &&
                    t.ExpiresAt > DateTime.UtcNow
                );

                if (ticket == null) {
                    throw new ApiException(ApiErrorCode.TokenExpired, "სესია არავალიდურია ან ვადაგასულია.", 401);
                }
                user = await _userManager.FindByIdAsync(userIdClaim);

                _context.AuthTickets.Remove(ticket);
                await _context.SaveChangesAsync();
            }
            else {
                if (string.IsNullOrEmpty(signInReq.UserName)) {
                    throw new ApiException(ApiErrorCode.ValidationError, "გთხოვთ შეიყვანეთ მომხმარებლის სახელი", 400);
                }

                if (string.IsNullOrEmpty(signInReq.Password)) {
                    throw new ApiException(ApiErrorCode.ValidationError, "გთხოვთ შეიყვანეთ მომხმარებლის პაროლი.", 400);
                }

                user = await _userManager.FindByNameAsync(signInReq.UserName);

                if (user == null || !await _userManager.CheckPasswordAsync(user, signInReq.Password)) {
                    throw new ApiException(ApiErrorCode.InvalidCredentials, "მომხმარებელი ან პაროლი არასწორია.", 401);
                }
            }

            if (user == null) {
                throw new ApiException(ApiErrorCode.Unauthorized, "Unauthorized", 401);
            }

            var roles = await _userManager.GetRolesAsync(user);
            bool isFullyVerified = !user.TwoFactorEnabled || signInReq.VerificationId.HasValue;

            return await GenerateFullAuth(user, roles, isFullyVerified);
        }

        public async Task<OtpRes> SendOtpAsync(SendOtpReq sendOtpReq, ClaimsPrincipal? currentUser) {

            if (!Enum.IsDefined(typeof(SendOtpType), sendOtpReq.Type)) {
                throw new ApiException(ApiErrorCode.BadRequest, "BadRequest1.", 400);
            }

            var jti = currentUser?.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var userIdClaim = currentUser?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUser == null || string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) {
                throw new ApiException(ApiErrorCode.Unauthorized, "Unauthorized", 401);
            }

            var oldTickets = await _context.AuthTickets
                .Where(t => t.UserId == userId)
                .ToListAsync();

            if (oldTickets.Any()) {
                _context.AuthTickets.RemoveRange(oldTickets);
            }
            var user = await _userManager.FindByIdAsync(userIdClaim);

            if (user == null) {
                throw new ApiException(ApiErrorCode.Unauthorized, "Unauthorized", 401);
            }

            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999);
            var otpId = Guid.NewGuid();

            var secretKey = _configuration["OtpSettings:SecretKey"];
            var rawData = $"{otpCode}|{otpId}|{user.SecurityStamp}";
            using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey!));
            var hashedOtp = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData)));

            var ticket = new AuthTicket {
                Id = otpId,
                SourceTokenId = jti,
                UserId = userId,
                HashedOtp = hashedOtp,
                Type = sendOtpReq.Type.ToString(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(2),
                AttemptsCount = 0,
                IsVerified = false
            };

            _context.AuthTickets.Add(ticket);
            await _context.SaveChangesAsync();

            /*
             * ეს არის ლოკალური პროექტის ჭრილში დასაშვები, რეალურ გარემოში მხოლოდ უნდა გაეგზავნოს მომხმარებელს სმს ტელეფონზე.
             * შენიშვნა: არ უნდა მოხდეს ამის შენახვა არც ცხრილში და არც ლოგებში.
            */
            Console.WriteLine($"SMS SEND TO {user.UserName}: {otpCode}");

            return new OtpRes { OtpId = ticket.Id };
        }

        public async Task<VerifyOtpRes> VerifyOtpAndGetIdAsync(VerifyOtpReq verifyOtpReq, ClaimsPrincipal? currentUser) {

            var jti = currentUser?.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var userIdClaim = currentUser?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUser == null || string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) {
                throw new ApiException(ApiErrorCode.Unauthorized, "Unauthorized", 401);
            }

            var ticket = await _context.AuthTickets.FirstOrDefaultAsync(t =>
                t.Id == verifyOtpReq.OtpId &&
                t.UserId == userId &&
                t.SourceTokenId == jti);

            if (ticket == null || ticket.IsVerified || ticket.ExpiresAt < DateTime.UtcNow) {
                throw new ApiException(ApiErrorCode.Unauthorized, "Unauthorized", 401);
            }

            var user = await _userManager.FindByIdAsync(userIdClaim);
            if (user == null) {
                throw new ApiException(ApiErrorCode.Unauthorized, "Unauthorized", 401);
            }

            var secretKey = _configuration["OtpSettings:SecretKey"];
            var rawOtpData = $"{verifyOtpReq.Code}|{verifyOtpReq.OtpId}|{user.SecurityStamp}";

            using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey!));
            var computedOtpHash = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawOtpData)));

            if (ticket.HashedOtp != computedOtpHash) {
                ticket.AttemptsCount++;
                await _context.SaveChangesAsync();
                throw new ApiException(ApiErrorCode.InvalidCredentials, "კოდი არასწორია.", 401);
            }

            var verificationId = Guid.NewGuid();

            var rawVerificationData = $"{verificationId}|{user.SecurityStamp}";
            var verificationHash = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawVerificationData)));

            _context.AuthTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            var verifiedTicket = new AuthTicket {
                Id = verificationId,
                SourceTokenId = ticket.SourceTokenId,
                UserId = userId,
                HashedOtp = verificationHash,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Type = ticket.Type,
                AttemptsCount = 0
            };

            _context.AuthTickets.Add(verifiedTicket);
            await _context.SaveChangesAsync();

            return new VerifyOtpRes {
                VerificationId = verificationId
            };
        }

        private string GenerateRefreshToken() {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        private async Task<LoginRes> GenerateFullAuth(ApplicationUser user, IList<string> roles, bool isFullyVerified) {
            // აგენერირებს Access Token-ს (შენი არსებული სერვისით)
            var accessToken = _tokenService.CreateToken(user, roles, isFullyVerified);
            string? refreshToken = null;

            // Refresh Token-ს ვაძლევთ მხოლოდ მაშინ, როცა MFA გავლილია (isFullyVerified == true)
            if (isFullyVerified) {
                var activeTokens = await _context.RefreshTokens
                    .Where(t => t.UserId == user.Id && !t.IsRevoked && !t.IsUsed)
                    .ToListAsync();

                foreach (var token in activeTokens) {
                    token.IsRevoked = true;
                }
                refreshToken = GenerateRefreshToken();

                var refreshTokenEntity = new RefreshToken {
                    Token = refreshToken,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddMinutes(10), // შენი 10 წუთიანი ლიმიტი
                    IsUsed = false,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();
            }

            return new LoginRes {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RequiresMfa = user.TwoFactorEnabled && !isFullyVerified,
                UserName = user.UserName
            };
        }
    }
}