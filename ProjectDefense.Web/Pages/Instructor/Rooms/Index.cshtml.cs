using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;

namespace ProjectDefense.Web.Pages.Instructor.Rooms;

[Authorize(Roles = "Instructor")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }
    public List<Room> Rooms { get; set; } = new();
    public async Task OnGet() => Rooms = await _db.Rooms.OrderBy(r => r.Name).ToListAsync();
}
