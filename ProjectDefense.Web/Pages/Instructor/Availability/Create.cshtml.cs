using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;

namespace ProjectDefense.Web.Pages.Instructor.Availability;

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

    public SelectList RoomOptions { get; set; } = default!;

    public class AvailabilityInput
    {
        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public DateOnly FromDate { get; set; }

        [Required]
        public DateOnly ToDate { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        [Range(1, 480)]
        public int SlotDurationMinutes { get; set; } = 15;
    }

    [BindProperty]
    public AvailabilityInput Input { get; set; } = new();

    public async Task OnGet()
    {
        if (Input.FromDate == default) Input.FromDate = DateOnly.FromDateTime(DateTime.Now);
        if (Input.ToDate == default) Input.ToDate = DateOnly.FromDateTime(DateTime.Now);
        RoomOptions = new SelectList(await _db.Rooms.OrderBy(r => r.Name).ToListAsync(), nameof(Room.Id), nameof(Room.Name));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await OnGet();
        if (!ModelState.IsValid) return Page();
        if (Input.FromDate > Input.ToDate) { ModelState.AddModelError(string.Empty, "FromDate must be <= ToDate"); return Page(); }
        if (Input.StartTime >= Input.EndTime) { ModelState.AddModelError(string.Empty, "StartTime must be < EndTime"); return Page(); }

        var instructorId = _userManager.GetUserId(User)!;

        // Check overlapping availability for same room and instructor
        bool overlaps = await _db.Availabilities.AnyAsync(a => a.InstructorId == instructorId && a.RoomId == Input.RoomId &&
            a.ToDate >= Input.FromDate && Input.ToDate >= a.FromDate &&
            a.EndTime > Input.StartTime && Input.EndTime > a.StartTime);
        if (overlaps)
        {
            ModelState.AddModelError(string.Empty, "Overlapping availability exists for this room and time range.");
            return Page();
        }

        var availability = new InstructorAvailability
        {
            Id = Guid.NewGuid(),
            InstructorId = instructorId,
            RoomId = Input.RoomId,
            FromDate = Input.FromDate,
            ToDate = Input.ToDate,
            StartTime = Input.StartTime,
            EndTime = Input.EndTime,
            SlotDurationMinutes = Input.SlotDurationMinutes
        };
        _db.Availabilities.Add(availability);

        // Generate slots
        foreach (var date in EachDate(Input.FromDate, Input.ToDate))
        {
            var start = date.ToDateTime(Input.StartTime, DateTimeKind.Local).ToUniversalTime();
            var end = date.ToDateTime(Input.EndTime, DateTimeKind.Local).ToUniversalTime();
            var cursor = start;
            while (cursor.AddMinutes(Input.SlotDurationMinutes) <= end)
            {
                var slotEnd = cursor.AddMinutes(Input.SlotDurationMinutes);
                // skip blocked periods for this instructor
                bool blocked = await _db.BlockedPeriods.AnyAsync(b => b.InstructorId == instructorId && b.FromUtc < slotEnd && b.ToUtc > cursor && (b.RoomId == null || b.RoomId == Input.RoomId));
                if (!blocked)
                {
                    _db.Reservations.Add(new Reservation
                    {
                        Id = Guid.NewGuid(),
                        AvailabilityId = availability.Id,
                        StartUtc = cursor,
                        EndUtc = slotEnd,
                        IsBlocked = false,
                        IsCanceled = false
                    });
                }
                cursor = slotEnd;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/Instructor/Rooms/Index");
    }

    private static IEnumerable<DateOnly> EachDate(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1)) yield return d;
    }
}

