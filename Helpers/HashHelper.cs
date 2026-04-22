using BasisBank.Identity.Api.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace BasisBank.Identity.Api.Helpers {
    public class HashHelper {
        private readonly byte[] _keyBytes;

        public HashHelper(IConfiguration configuration) {
            var secret = configuration["OtpSettings:SecretKey"];

            if (string.IsNullOrWhiteSpace(secret)) {
                throw new ApiException(
                    ApiErrorCode.InternalServerError,
                    "OtpSettings:SecretKey missing",
                    500
                );
            }

            _keyBytes = Encoding.UTF8.GetBytes(secret);
        }

        public string GetSecureHash(string otp, Guid otpId, string securityStamp) {
            var rawData = $"{otp}|{otpId}|{securityStamp}";
            var dataBytes = Encoding.UTF8.GetBytes(rawData);

            using var hmac = new HMACSHA256(_keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);

            return Convert.ToBase64String(hashBytes);
        }
    }
}
