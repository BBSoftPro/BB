namespace BasisBank.Identity.Api.DTOs.Responses {
    public class VerifyOtpRes {
        public Guid VerificationId { get; set; }
        public string VerificationSecret { get; set; } = null!;
    }
}
