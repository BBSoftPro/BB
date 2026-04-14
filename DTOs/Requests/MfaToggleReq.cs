namespace BasisBank.Identity.Api.DTOs.Requests {
    public class MfaToggleReq {
        public bool Enable { get; set; }
        public required Guid VerificationId { get; set; }
    }
}
