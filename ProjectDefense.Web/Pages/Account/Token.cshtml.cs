using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectDefense.Shared.Entities;

namespace ProjectDefense.Web.Pages.Account;

[Authorize]
public class TokenModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    public TokenModel(UserManager<ApplicationUser> userManager) { _userManager = userManager; }

    public string Email { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;

    public async Task OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        Email = user?.Email ?? string.Empty;
        ApiToken = user?.ApiToken ?? "";
    }
}
