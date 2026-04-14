namespace BasisBank.Identity.Api.DTOs.Requests {
    public class SignUpReq {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? IdentificationId { get; set; }
        public string? Passport { get; set; }
    }
}
