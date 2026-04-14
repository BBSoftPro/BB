namespace BasisBank.Identity.Api.Entities {
    public class AuthTicket {
        public Guid Id { get; set; }
        public string SourceTokenId { get; set; } = null!;
        public int UserId { get; set; }
        public string HashedOtp { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int AttemptsCount { get; set; }
        public bool IsVerified { get; set; }
    }
}