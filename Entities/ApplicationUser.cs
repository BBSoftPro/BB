using Microsoft.AspNetCore.Identity;

namespace BasisBank.Identity.Api.Entities {
    public class ApplicationUser : IdentityUser<int> {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? IdentificationId { get; set; }
        public string? Passport { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}