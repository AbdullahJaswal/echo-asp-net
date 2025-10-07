using System.ComponentModel.DataAnnotations.Schema;

namespace Echo.Models;

public class RoomMember
{
    [ForeignKey(nameof(Room))] public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    [ForeignKey(nameof(User))] public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsModerator { get; set; } = false;

    public DateTime JoinedUtc { get; set; } = DateTime.UtcNow;
}
