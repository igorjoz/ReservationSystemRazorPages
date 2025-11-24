using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace ProjectDefense.Web.Pages.Instructor.Blocks;

[Authorize(Roles = "Instructor")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public DateTime StartLocal { get; set; } = DateTime.Now;
        [Required]
        public DateTime EndLocal { get; set; } = DateTime.Now.AddHours(1);
        public string? Reason { get; set; }
    }

    public void OnGet()
    {
        var now = DateTime.Now;
        // Strip seconds
        Input.StartLocal = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
        Input.EndLocal = Input.StartLocal.AddHours(1);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var startUtc = Input.StartLocal.ToUniversalTime();
        var endUtc = Input.EndLocal.ToUniversalTime();

        if (startUtc >= endUtc)
        {
            ModelState.AddModelError(string.Empty, "End time must be after start time.");
            return Page();
        }

        var instructorId = _userManager.GetUserId(User)!;

        var block = new BlockedPeriod
        {
            InstructorId = instructorId,
            FromUtc = startUtc,
            ToUtc = endUtc,
            Reason = Input.Reason
        };

        _db.BlockedPeriods.Add(block);

        // Cancel reservations in this period
        var reservations = await _db.Reservations
            .Include(r => r.Availability)
            .Where(r => r.Availability!.InstructorId == instructorId && !r.IsCanceled &&
                        r.StartUtc < endUtc && r.EndUtc > startUtc)
            .ToListAsync();

        foreach (var res in reservations)
        {
            res.IsCanceled = true;
            res.StudentId = null; // Optionally clear student
            // In a real system, we might want to notify the student
        }

        await _db.SaveChangesAsync();

        return RedirectToPage("/Instructor/Dashboard");
    }
}
