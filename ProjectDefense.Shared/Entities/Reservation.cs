namespace ProjectDefense.Shared.Entities;

public class Reservation
{
    public Guid Id { get; set; }
    public Guid AvailabilityId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? StudentId { get; set; }
    public bool IsCanceled { get; set; }
    public bool IsBlocked { get; set; }

    public InstructorAvailability? Availability { get; set; }
}
