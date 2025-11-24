using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;

namespace ProjectDefense.Web.Pages.Instructor.Availability;

[Authorize(Roles = "Instructor")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<InstructorAvailability> Availabilities { get; set; } = new();

    public async Task OnGetAsync()
    {
        var instructorId = _userManager.GetUserId(User);
        Availabilities = await _db.Availabilities
            .Include(a => a.Room)
            .Where(a => a.InstructorId == instructorId)
            .OrderByDescending(a => a.FromDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var instructorId = _userManager.GetUserId(User);

        // Check for past reservations
        bool hasPastReservations = await _db.Reservations.AnyAsync(r => r.AvailabilityId == id && r.StartUtc < DateTime.UtcNow);
        if (hasPastReservations)
        {
            ModelState.AddModelError(string.Empty, "Cannot delete availability with past reservations.");
            Availabilities = await _db.Availabilities
                .Include(a => a.Room)
                .Where(a => a.InstructorId == instructorId)
                .OrderByDescending(a => a.FromDate)
                .ToListAsync();
            return Page();
        }

        var availability = await _db.Availabilities
            .FirstOrDefaultAsync(a => a.Id == id && a.InstructorId == instructorId);

        if (availability != null)
        {
            _db.Availabilities.Remove(availability);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
