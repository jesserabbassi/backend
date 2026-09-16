namespace NinetyBackend.Models
{
    public class ExternalLogin
    {
        public Guid id { get; set; }
        public Guid UserId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public User User { get; set; } = null!;
    }
}
