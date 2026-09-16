namespace NinetyBackend.Models
{
    public class OtpVerification
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string CodeHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public int Attempts { get; set; }

        public bool Verified { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}
