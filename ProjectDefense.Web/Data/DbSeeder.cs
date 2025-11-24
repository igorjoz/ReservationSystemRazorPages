using Microsoft.AspNetCore.Identity;
using ProjectDefense.Shared.Entities;

namespace ProjectDefense.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        await EnsureRoleAsync(roleManager, "Instructor");
        await EnsureRoleAsync(roleManager, "Student");

        var seedSection = configuration.GetSection("Seed");
        if (seedSection.Exists())
        {
            var instructorConfig = seedSection.GetSection("Instructor");
            if (instructorConfig.Exists())
            {
                await EnsureUserAsync(userManager, instructorConfig["Email"]!, instructorConfig["Password"]!, "Instructor");
            }

            var studentConfig = seedSection.GetSection("Student");
            if (studentConfig.Exists())
            {
                await EnsureUserAsync(userManager, studentConfig["Email"]!, studentConfig["Password"]!, "Student");
                // Add second student
                await EnsureUserAsync(userManager, "student2@example.com", studentConfig["Password"]!, "Student");
            }
        }

        var dbContext = serviceProvider.GetRequiredService<ProjectDefense.Shared.Data.AppDbContext>();
        if (!dbContext.Rooms.Any(r => r.Name == "A" && r.Number == "1"))
        {
            dbContext.Rooms.Add(new Room { Name = "A", Number = "1" });
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
        else
        {
             if (!await userManager.IsInRoleAsync(user, role))
             {
                 await userManager.AddToRoleAsync(user, role);
             }
        }
    }
}
