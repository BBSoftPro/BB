namespace BasisBank.Identity.Api.DTOs.Requests {
    public class VerifyOtpReq {
        public required Guid OtpId { get; set; }
        public required string Code { get; set; }
    }
}
