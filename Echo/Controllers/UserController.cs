using System.Security.Claims;

using Echo.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Echo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var idStr = User.FindFirstValue("sub");
        var username = User.FindFirstValue("name");
        if (!Guid.TryParse(idStr, out var userId)) return Unauthorized();

        var rooms = await (from m in db.RoomMembers.AsNoTracking()
                           join r in db.Rooms.AsNoTracking() on m.RoomId equals r.Id
                           where m.UserId == userId
                           select new
                           {
                               id = r.Id,
                               name = r.Name,
                               isModerator = m.IsModerator
                           }).ToListAsync(cancellationToken);

        return Ok(new
        {
            id = idStr,
            username,
            rooms
        });
    }
}
