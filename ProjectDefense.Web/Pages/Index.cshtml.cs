using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjectDefense.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Student"))
            {
                return RedirectToPage("/Student/Slots/Index");
            }
            if (User.IsInRole("Instructor"))
            {
                return RedirectToPage("/Instructor/Dashboard");
            }
        }

        return Page();
    }
}
