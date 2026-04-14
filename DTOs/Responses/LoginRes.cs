namespace BasisBank.Identity.Api.DTOs.Responses {
    public class LoginRes {
        public required string AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public bool RequiresMfa { get; set; }
        public int ExpiresIn { get; set; } = 3600;
        public string? UserName { get; set; }
    }
}
