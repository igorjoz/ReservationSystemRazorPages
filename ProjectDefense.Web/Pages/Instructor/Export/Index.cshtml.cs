using System.ComponentModel.DataAnnotations;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProjectDefense.Web.Pages.Instructor.Export;

[Authorize(Roles = "Instructor")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    public IndexModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    { _db = db; _userManager = userManager; }

    public SelectList RoomOptions { get; set; } = default!;

    public class ExportInput
    {
        [Required]
        public Guid RoomId { get; set; }
        [Required]
        public DateTime FromLocal { get; set; } = DateTime.Today;
        [Required]
        public DateTime ToLocal { get; set; } = DateTime.Today.AddDays(1);
        [Required]
        public string Format { get; set; } = "txt"; // txt|xlsx|pdf
    }

    [BindProperty]
    public ExportInput Input { get; set; } = new();

    public async Task OnGet()
    {
        RoomOptions = new SelectList(await _db.Rooms.OrderBy(r => r.Name).ToListAsync(), nameof(Room.Id), nameof(Room.Name));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await OnGet();
        if (!ModelState.IsValid) return Page();
        if (Input.FromLocal >= Input.ToLocal)
        {
            ModelState.AddModelError(string.Empty, "From must be earlier than To");
            return Page();
        }
        var instructorId = _userManager.GetUserId(User)!;
        var fromUtc = DateTime.SpecifyKind(Input.FromLocal, DateTimeKind.Local).ToUniversalTime();
        var toUtc = DateTime.SpecifyKind(Input.ToLocal, DateTimeKind.Local).ToUniversalTime();

        var data = await _db.Reservations.Include(r => r.Availability)!.ThenInclude(a => a!.Room)
            .Where(r => r.Availability!.InstructorId == instructorId && r.Availability.RoomId == Input.RoomId
                        && r.StartUtc >= fromUtc && r.EndUtc <= toUtc)
            .OrderBy(r => r.StartUtc).ToListAsync();
        var users = await _db.Users.ToDictionaryAsync(u => u.Id, u => u.Email);

        var rows = data.Select(r => new
        {
            Room = (r.Availability!.Room!.Name + (string.IsNullOrWhiteSpace(r.Availability.Room.Number) ? string.Empty : $" {r.Availability.Room.Number}")).Trim(),
            Start = r.StartUtc.ToLocalTime().ToString("g"),
            End = r.EndUtc.ToLocalTime().ToString("t"),
            Student = r.StudentId != null && users.ContainsKey(r.StudentId) ? users[r.StudentId] : "(free)",
            Status = r.IsBlocked ? "BLOCKED" : (r.StudentId == null ? "FREE" : "BOOKED")
        }).ToList();

        switch (Input.Format)
        {
            case "txt":
                var txt = string.Join(Environment.NewLine, rows.Select(x => $"{x.Room}\t{x.Start}-{x.End}\t{x.Student}\t{x.Status}"));
                var txtBytes = System.Text.Encoding.UTF8.GetBytes(txt);
                return File(txtBytes, "text/plain", "reservations.txt");
            case "xlsx":
                using (var wb = new XLWorkbook())
                {
                    var ws = wb.AddWorksheet("Reservations");
                    ws.Cell(1, 1).Value = "Room";
                    ws.Cell(1, 2).Value = "Start";
                    ws.Cell(1, 3).Value = "End";
                    ws.Cell(1, 4).Value = "Student";
                    ws.Cell(1, 5).Value = "Status";
                    var r = 2;
                    foreach (var x in rows)
                    {
                        ws.Cell(r, 1).Value = x.Room;
                        ws.Cell(r, 2).Value = x.Start;
                        ws.Cell(r, 3).Value = x.End;
                        ws.Cell(r, 4).Value = x.Student;
                        ws.Cell(r, 5).Value = x.Status;
                        r++;
                    }
                    using var ms = new MemoryStream();
                    wb.SaveAs(ms);
                    return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reservations.xlsx");
                }
            case "pdf":
                QuestPDF.Settings.License = LicenseType.Community;
                var doc = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);
                        page.Header().Text("Reservations").SemiBold().FontSize(16).AlignCenter();
                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Text("Room");
                                h.Cell().Text("Start");
                                h.Cell().Text("End");
                                h.Cell().Text("Student");
                                h.Cell().Text("Status");
                            });
                            foreach (var x in rows)
                            {
                                table.Cell().Text(x.Room);
                                table.Cell().Text(x.Start);
                                table.Cell().Text(x.End);
                                table.Cell().Text(x.Student);
                                table.Cell().Text(x.Status);
                            }
                        });
                    });
                });
                var pdfBytes = doc.GeneratePdf();
                return File(pdfBytes, "application/pdf", "reservations.pdf");
            default:
                ModelState.AddModelError(string.Empty, "Unsupported format.");
                return Page();
        }
    }
}

