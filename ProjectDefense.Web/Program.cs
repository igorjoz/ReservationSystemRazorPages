using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Data;
using ProjectDefense.Shared.DTOs;
using ProjectDefense.Shared.Entities;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Services;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(ProjectDefense.Web.SharedResource));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("pl-PL") };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// Ensure the database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Minimal API
app.MapGet("/api/rooms", async (AppDbContext db) =>
{
    var rooms = await db.Rooms.ToListAsync();
    return Results.Ok(rooms.Select(r => new RoomDto(r.Id, r.Name, r.Number)));
})
.WithName("GetRooms")
.WithOpenApi();

app.MapGet("/api/slots/available", async (AppDbContext db) =>
{
    var now = DateTime.UtcNow;
    var slots = await db.Reservations
        .Include(r => r.Availability).ThenInclude(a => a!.Room)
        .Where(r => r.StudentId == null && !r.IsCanceled && !r.IsBlocked && r.StartUtc > now)
        .OrderBy(r => r.StartUtc)
        .ToListAsync();

    return Results.Ok(slots.Select(r => new SlotDto(
        r.Id,
        r.Availability!.RoomId,
        $"{r.Availability.Room!.Name} {r.Availability.Room.Number}".Trim(),
        r.StartUtc,
        r.EndUtc
    )));
})
.WithName("GetAvailableSlots")
.WithOpenApi();

app.MapPost("/api/slots/{id}/book", async (Guid id, BookSlotRequest request, AppDbContext db, UserManager<ApplicationUser> userManager) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.ApiToken == request.ApiToken);
    if (user == null) return Results.Unauthorized();

    // Check ban
    var ban = await db.StudentBans.FirstOrDefaultAsync(b => b.StudentId == user.Id);
    if (ban != null && (ban.UntilUtc == null || ban.UntilUtc > DateTime.UtcNow))
    {
        return Results.BadRequest("You are banned.");
    }

    var now = DateTime.UtcNow;
    var slot = await db.Reservations.FirstOrDefaultAsync(r => r.Id == id);

    if (slot == null || slot.StudentId != null || slot.IsCanceled || slot.IsBlocked || slot.StartUtc <= now)
    {
        return Results.BadRequest("Slot not available.");
    }

    // Check existing reservation
    var existing = await db.Reservations.FirstOrDefaultAsync(r => r.StudentId == user.Id && !r.IsCanceled && r.EndUtc > now);
    if (existing != null)
    {
        existing.StudentId = null; // Cancel old one
    }

    slot.StudentId = user.Id;
    await db.SaveChangesAsync();

    return Results.Ok("Booked successfully.");
})
.WithName("BookSlot")
.WithOpenApi();

app.MapGet("/api/test-email", async (string to, IEmailSender emailSender) =>
{
    try
    {
        await emailSender.SendEmailAsync(to, "Test Email", "This is a test email from Project Defense.");
        return Results.Ok($"Email sent to {to}");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("TestEmail")
.WithOpenApi();

app.Run();
