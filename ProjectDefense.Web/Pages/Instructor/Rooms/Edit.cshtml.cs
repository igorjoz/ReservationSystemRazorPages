using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;

namespace ProjectDefense.Web.Pages.Instructor.Rooms;

[Authorize(Roles = "Instructor")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Guid Id { get; set; }

    public class EditInput
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Number { get; set; }
    }

    [BindProperty]
    public EditInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();
        Id = id;
        Input = new EditInput { Name = room.Name, Number = room.Number };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var room = await _db.Rooms.FindAsync(Id);
        if (room == null) return NotFound();
        room.Name = Input.Name.Trim();
        room.Number = string.IsNullOrWhiteSpace(Input.Number) ? null : Input.Number!.Trim();
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
