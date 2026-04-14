namespace BasisBank.Identity.Api.DTOs.Responses {
    public class AuthRes {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public bool RequiresMfa { get; set; }
    }
}
