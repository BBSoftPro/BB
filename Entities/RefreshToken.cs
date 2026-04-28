namespace BasisBank.Identity.Api.Entities {
    public class RefreshToken {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
