namespace NinetyBackend.Models
{
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
