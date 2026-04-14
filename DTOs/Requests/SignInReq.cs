namespace BasisBank.Identity.Api.DTOs.Requests {
    public class SignInReq {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public Guid? VerificationId { get; set; }
    }
}
