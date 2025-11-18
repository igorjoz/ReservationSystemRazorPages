using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;

namespace ProjectDefense.Web.Pages.Instructor.Rooms;

[Authorize(Roles = "Instructor")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    public CreateModel(AppDbContext db) { _db = db; }

    public class CreateInput
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Number { get; set; }
    }

    [BindProperty]
    public CreateInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        _db.Rooms.Add(new Room { Name = Input.Name.Trim(), Number = string.IsNullOrWhiteSpace(Input.Number) ? null : Input.Number!.Trim() });
        await _db.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
