namespace Echo.DTOs;

public sealed class RoomCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RoomUpdateRequest
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class RoomJoinRequest
{
    public string Password { get; set; } = string.Empty;
}

public sealed class RoomResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public bool IsMember { get; set; }
    public bool IsModerator { get; set; }
}
