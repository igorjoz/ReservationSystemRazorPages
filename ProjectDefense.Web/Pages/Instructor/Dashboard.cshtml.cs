using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;

namespace ProjectDefense.Web.Pages.Instructor;

[Authorize(Roles = "Instructor")]
public class DashboardModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    public DashboardModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db; _userManager = userManager;
    }

    public record ResVM(Guid Id, string RoomLabel, DateTime StartLocal, DateTime EndLocal, string? StudentEmail);
    public List<ResVM> Upcoming { get; set; } = new();

    public async Task OnGet()
    {
        var instructorId = _userManager.GetUserId(User)!;
        var now = DateTime.UtcNow;
        var items = await _db.Reservations.Include(r => r.Availability)!.ThenInclude(a => a!.Room)
            .Where(r => r.Availability!.InstructorId == instructorId && !r.IsCanceled && r.EndUtc > now)
            .OrderBy(r => r.StartUtc)
            .ToListAsync();

        var users = await _db.Users.ToDictionaryAsync(u => u.Id, u => u.Email);
        Upcoming = items.Select(r => new ResVM(
            r.Id,
            (r.Availability!.Room!.Name + (string.IsNullOrWhiteSpace(r.Availability.Room.Number) ? string.Empty : $" {r.Availability.Room.Number}")).Trim(),
            r.StartUtc.ToLocalTime(), r.EndUtc.ToLocalTime(),
            r.StudentId != null && users.ContainsKey(r.StudentId) ? users[r.StudentId] : null
        )).ToList();
    }

    public async Task<IActionResult> OnPostAsync(Guid? cancelId)
    {
        if (cancelId.HasValue)
        {
            var instructorId = _userManager.GetUserId(User)!;
            var slot = await _db.Reservations.Include(r => r.Availability).FirstOrDefaultAsync(r => r.Id == cancelId.Value && r.Availability!.InstructorId == instructorId);
            if (slot != null && !slot.IsCanceled && slot.EndUtc > DateTime.UtcNow)
            {
                slot.StudentId = null; // free the slot
                await _db.SaveChangesAsync();
            }
        }
        return RedirectToPage();
    }
}
