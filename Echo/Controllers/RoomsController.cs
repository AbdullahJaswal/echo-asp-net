using System.Security.Claims;

using Echo.Data;
using Echo.DTOs;
using Echo.Models;
using Echo.Services.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Echo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController(AppDbContext db, IPasswordHasher hasher) : ControllerBase
{
    private bool TryGetUserId(out Guid userId)
    {
        var sub = User.FindFirstValue("sub");
        return Guid.TryParse(sub, out userId);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var room = await db.Rooms.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room == null) return NotFound();

        var membership = await db.RoomMembers.AsNoTracking()
            .SingleOrDefaultAsync(m => m.RoomId == id && m.UserId == userId, cancellationToken);

        return Ok(new RoomResponse
        {
            Id = room.Id,
            Name = room.Name,
            IsActive = room.IsActive,
            CreatedUtc = room.CreatedUtc,
            IsMember = membership != null,
            IsModerator = membership?.IsModerator == true
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var query = from m in db.RoomMembers.AsNoTracking()
            join r in db.Rooms.AsNoTracking() on m.RoomId equals r.Id
            where m.UserId == userId
            select new RoomResponse
            {
                Id = r.Id,
                Name = r.Name,
                IsActive = r.IsActive,
                CreatedUtc = r.CreatedUtc,
                IsMember = true,
                IsModerator = m.IsModerator
            };

        var list = await query.ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var memberships = await db.RoomMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .ToDictionaryAsync(m => m.RoomId, m => m.IsModerator, cancellationToken);

        var rooms = await db.Rooms.AsNoTracking().ToListAsync(cancellationToken);

        var list = rooms.Select(r => new RoomResponse
        {
            Id = r.Id,
            Name = r.Name,
            IsActive = r.IsActive,
            CreatedUtc = r.CreatedUtc,
            IsMember = memberships.ContainsKey(r.Id),
            IsModerator = memberships.TryGetValue(r.Id, out var isMod) && isMod
        }).ToList();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> Create([FromBody] RoomCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var name = request.Name.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || string.IsNullOrWhiteSpace(password))
            return BadRequest();

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = name,
            PasswordHash = hasher.Hash(password),
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };

        var membership = new RoomMember
        {
            RoomId = room.Id,
            UserId = userId,
            IsModerator = true,
            JoinedUtc = DateTime.UtcNow
        };

        db.Rooms.Add(room);
        db.RoomMembers.Add(membership);
        await db.SaveChangesAsync(cancellationToken);

        var response = new RoomResponse
        {
            Id = room.Id,
            Name = room.Name,
            IsActive = room.IsActive,
            CreatedUtc = room.CreatedUtc,
            IsMember = true,
            IsModerator = true
        };

        return CreatedAtAction(nameof(GetById), new { id = room.Id }, response);
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id, [FromBody] RoomJoinRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var room = await db.Rooms.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room == null) return NotFound();

        if (await db.RoomMembers.AnyAsync(m => m.RoomId == id && m.UserId == userId, cancellationToken))
            return NoContent();

        if (!hasher.Verify(request.Password, room.PasswordHash)) return Unauthorized();

        db.RoomMembers.Add(new RoomMember
        {
            RoomId = id,
            UserId = userId,
            IsModerator = false,
            JoinedUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RoomUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var membership = await db.RoomMembers.AsNoTracking()
            .SingleOrDefaultAsync(m => m.RoomId == id && m.UserId == userId, cancellationToken);
        if (membership == null || !membership.IsModerator) return Forbid();

        var room = await db.Rooms.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room == null) return NotFound();

        if (request.Name != null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100) return BadRequest();
            room.Name = name;
        }

        if (request.IsActive.HasValue)
        {
            room.IsActive = request.IsActive.Value;
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var membership = await db.RoomMembers.AsNoTracking()
            .SingleOrDefaultAsync(m => m.RoomId == id && m.UserId == userId, cancellationToken);
        if (membership == null || !membership.IsModerator) return Forbid();

        var room = await db.Rooms.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room == null) return NotFound();

        db.Rooms.Remove(room);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
