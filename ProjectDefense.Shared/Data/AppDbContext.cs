using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Shared.Entities;

namespace ProjectDefense.Shared.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<InstructorAvailability> Availabilities => Set<InstructorAvailability>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<BlockedPeriod> BlockedPeriods => Set<BlockedPeriod>();
    public DbSet<StudentBan> StudentBans => Set<StudentBan>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Room>().HasIndex(r => new { r.Name, r.Number }).IsUnique(false);

        builder.Entity<InstructorAvailability>()
            .HasOne(a => a.Room)
            .WithMany()
            .HasForeignKey(a => a.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Reservation>()
            .HasOne(r => r.Availability)
            .WithMany()
            .HasForeignKey(r => r.AvailabilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentBan>()
            .HasIndex(b => b.StudentId)
            .IsUnique();
    }
}
