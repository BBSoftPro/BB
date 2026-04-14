using BasisBank.Identity.Api.Entities;

namespace BasisBank.Identity.Api.Interfaces {
    public interface ITokenService {
        string CreateToken(ApplicationUser user, IList<string> roles, bool isMfaVerified = false);
    }
}