using System.ComponentModel.DataAnnotations;

namespace Echo.Models;

public class User
{
    [Key] public Guid Id { get; set; }

    [MaxLength(100)] public string Username { get; set; } = string.Empty;
    [MaxLength(512)] public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RoomMember> RoomMemberships { get; set; } = new HashSet<RoomMember>();
}
