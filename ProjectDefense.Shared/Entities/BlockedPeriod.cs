namespace ProjectDefense.Shared.Entities;

public class BlockedPeriod
{
    public Guid Id { get; set; }
    public string InstructorId { get; set; } = string.Empty;
    public Guid? RoomId { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public string? Reason { get; set; }
}
