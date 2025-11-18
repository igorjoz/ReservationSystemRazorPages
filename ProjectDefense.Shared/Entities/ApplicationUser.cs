using Microsoft.AspNetCore.Identity;

namespace ProjectDefense.Shared.Entities;

public class ApplicationUser : IdentityUser
{
    public string? ApiToken { get; set; }
}
