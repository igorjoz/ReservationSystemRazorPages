using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace ProjectDefense.Web.Pages.Instructor.Reservations;

[Authorize(Roles = "Instructor")]
public class ReassignModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReassignModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Reservation? Reservation { get; set; }
    public string? CurrentStudentEmail { get; set; }

    [BindProperty]
    [Required]
    public Guid NewSlotId { get; set; }

    public List<SelectListItem> SlotOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var instructorId = _userManager.GetUserId(User);
        Reservation = await _db.Reservations
            .Include(r => r.Availability).ThenInclude(a => a!.Room)
            .FirstOrDefaultAsync(r => r.Id == id && r.Availability!.InstructorId == instructorId);

        if (Reservation == null) return NotFound();

        if (Reservation.StudentId != null)
        {
            var currentStudent = await _userManager.FindByIdAsync(Reservation.StudentId);
            CurrentStudentEmail = currentStudent?.Email;
        }

        // Get other available slots
        var now = DateTime.UtcNow;
        var slots = await _db.Reservations
            .Include(r => r.Availability).ThenInclude(a => a!.Room)
            .Where(r => r.StudentId == null && !r.IsCanceled && !r.IsBlocked && r.StartUtc > now && r.Id != id)
            .OrderBy(r => r.StartUtc)
            .ToListAsync();

        SlotOptions = slots.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.StartUtc.ToLocalTime():g} - {s.EndUtc.ToLocalTime():t} ({s.Availability!.Room!.Name} {s.Availability.Room.Number})"
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var instructorId = _userManager.GetUserId(User);
        var oldReservation = await _db.Reservations
            .Include(r => r.Availability)
            .FirstOrDefaultAsync(r => r.Id == id && r.Availability!.InstructorId == instructorId);

        if (oldReservation == null) return NotFound();

        var studentId = oldReservation.StudentId;
        if (studentId == null)
        {
            ModelState.AddModelError(string.Empty, "Current slot has no student to reassign.");
            await OnGetAsync(id);
            return Page();
        }

        var newReservation = await _db.Reservations
            .FirstOrDefaultAsync(r => r.Id == NewSlotId && r.StudentId == null && !r.IsCanceled && !r.IsBlocked);

        if (newReservation == null)
        {
            ModelState.AddModelError(string.Empty, "Selected slot is not available.");
            await OnGetAsync(id);
            return Page();
        }

        // Move student
        newReservation.StudentId = studentId;
        oldReservation.StudentId = null;

        await _db.SaveChangesAsync();

        return RedirectToPage("/Instructor/Dashboard");
    }
}
