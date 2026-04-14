using BasisBank.Identity.Api.Entities;
using BasisBank.Identity.Api.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BasisBank.Identity.Api.Services {
    public class TokenService : ITokenService {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration) {
            _configuration = configuration;
        }

        public string CreateToken(ApplicationUser user, IList<string> roles, bool isMfaVerified = false) {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
        new Claim(JwtRegisteredClaimNames.Email, user.Email!),
        new Claim("FirstName", user.FirstName ?? ""),
        new Claim("LastName", user.LastName ?? ""),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        // MFA სტატუსი
        new Claim("amr", isMfaVerified ? "mfa" : "pwd")
    };

            // როლების დამატება
            foreach (var role in roles) {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // ClaimsIdentity-ს შექმნა როლის ტიპის მითითებით
            var claimsIdentity = new ClaimsIdentity(claims, "Jwt", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"] ?? "60")),
                SigningCredentials = creds,
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
