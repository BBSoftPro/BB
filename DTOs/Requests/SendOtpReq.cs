using BasisBank.Identity.Api.Enums;

namespace BasisBank.Identity.Api.DTOs.Requests {
    public class SendOtpReq {
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public required SendOtpType Type { get; set; }
    }
}
