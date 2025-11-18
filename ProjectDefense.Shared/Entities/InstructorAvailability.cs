namespace ProjectDefense.Shared.Entities;

public class InstructorAvailability
{
    public Guid Id { get; set; }
    public string InstructorId { get; set; } = string.Empty;
    public Guid RoomId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotDurationMinutes { get; set; }

    public Room? Room { get; set; }
}
