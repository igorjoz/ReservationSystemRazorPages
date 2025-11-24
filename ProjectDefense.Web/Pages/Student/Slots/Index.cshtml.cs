using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;

namespace ProjectDefense.Web.Pages.Student.Slots;

[Authorize(Roles = "Student")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public bool IsBanned { get; set; }
    public Reservation? CurrentReservation { get; set; }
    public List<Reservation> AvailableSlots { get; set; } = new();
    public Dictionary<string, string> InstructorNames { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return;

        // Check ban
        var ban = await _db.StudentBans.FirstOrDefaultAsync(b => b.StudentId == userId);
        if (ban != null && (ban.UntilUtc == null || ban.UntilUtc > DateTime.UtcNow))
        {
            IsBanned = true;
            return;
        }

        var now = DateTime.UtcNow;

        // Get current reservation
        CurrentReservation = await _db.Reservations
            .Include(r => r.Availability)
            .ThenInclude(a => a!.Room)
            .FirstOrDefaultAsync(r => r.StudentId == userId && !r.IsCanceled && r.EndUtc > now);

        // Get available slots
        AvailableSlots = await _db.Reservations
            .Include(r => r.Availability)
            .ThenInclude(a => a!.Room)
            .Where(r => r.StudentId == null && !r.IsCanceled && !r.IsBlocked && r.StartUtc > now)
            .OrderBy(r => r.StartUtc)
            .ToListAsync();

        // Get instructor names
        var instructorIds = AvailableSlots.Select(r => r.Availability!.InstructorId).Distinct().ToList();
        if (CurrentReservation != null)
        {
            instructorIds.Add(CurrentReservation.Availability!.InstructorId);
        }
        
        var instructors = await _db.Users
            .Where(u => instructorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();
            
        InstructorNames = instructors.ToDictionary(u => u.Id, u => u.Email ?? "Unknown");
    }

    public async Task<IActionResult> OnPostReserveAsync(Guid slotId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToPage();

        // Check ban
        var ban = await _db.StudentBans.FirstOrDefaultAsync(b => b.StudentId == userId);
        if (ban != null && (ban.UntilUtc == null || ban.UntilUtc > DateTime.UtcNow))
        {
            return RedirectToPage();
        }

        var now = DateTime.UtcNow;

        // Check existing
        var existing = await _db.Reservations
            .FirstOrDefaultAsync(r => r.StudentId == userId && !r.IsCanceled && r.EndUtc > now);

        // Get target
        var target = await _db.Reservations
            .FirstOrDefaultAsync(r => r.Id == slotId);

        if (target == null || target.StudentId != null || target.IsCanceled || target.IsBlocked || target.StartUtc <= now)
        {
            ModelState.AddModelError(string.Empty, "Slot is no longer available.");
            return RedirectToPage();
        }

        // If changing reservation, free the old one
        if (existing != null)
        {
            existing.StudentId = null;
        }

        target.StudentId = userId;
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid slotId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return RedirectToPage();

        var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == slotId && r.StudentId == userId);
        if (reservation != null && !reservation.IsCanceled && reservation.EndUtc > DateTime.UtcNow)
        {
            reservation.StudentId = null;
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
