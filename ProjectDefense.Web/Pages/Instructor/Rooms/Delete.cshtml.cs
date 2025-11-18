using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectDefense.Shared.Data;

namespace ProjectDefense.Web.Pages.Instructor.Rooms;

[Authorize(Roles = "Instructor")]
public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    public DeleteModel(AppDbContext db) { _db = db; }

    [BindProperty]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Number { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();
        Id = id;
        Name = room.Name;
        Number = room.Number;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var room = await _db.Rooms.FindAsync(Id);
        if (room == null) return NotFound();
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
