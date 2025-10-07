using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Echo.Models;

public class RefreshToken
{
    [Key] public Guid Id { get; set; }

    [Required] public Guid UserId { get; set; }
    [ForeignKey(nameof(UserId))] public User? User { get; set; }
    [Required][MaxLength(512)] public string Token { get; set; } = string.Empty;

    public bool Revoked { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresUtc { get; set; }
}
