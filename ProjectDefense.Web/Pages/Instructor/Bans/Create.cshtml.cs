using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace ProjectDefense.Web.Pages.Instructor.Bans;

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

    public List<SelectListItem> StudentOptions { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;
        public DateTime? UntilLocal { get; set; }
        public string? Reason { get; set; }
    }

    public async Task OnGetAsync()
    {
        var students = await _userManager.GetUsersInRoleAsync("Student");
        StudentOptions = students.Select(s => new SelectListItem
        {
            Value = s.Id,
            Text = s.Email
        }).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var ban = new StudentBan
        {
            StudentId = Input.StudentId,
            Reason = Input.Reason,
            UntilUtc = Input.UntilLocal?.ToUniversalTime()
        };

        // Check if already banned
        var existing = await _db.StudentBans.FirstOrDefaultAsync(b => b.StudentId == Input.StudentId);
        if (existing != null)
        {
            existing.Reason = Input.Reason;
            existing.UntilUtc = Input.UntilLocal?.ToUniversalTime();
        }
        else
        {
            _db.StudentBans.Add(ban);
        }

        // Cancel future reservations for this student
        var reservations = await _db.Reservations
            .Where(r => r.StudentId == Input.StudentId && r.EndUtc > DateTime.UtcNow && !r.IsCanceled)
            .ToListAsync();

        foreach (var res in reservations)
        {
            res.StudentId = null;
            // res.IsCanceled = true; // Or just free the slot? Requirement says "uniemożliwiając mu logowanie lub rezerwację". Usually implies freeing up slots.
        }

        await _db.SaveChangesAsync();

        return RedirectToPage("/Instructor/Dashboard");
    }
}
