namespace ProjectDefense.Shared.Entities;

public class StudentBan
{
    public Guid Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime? UntilUtc { get; set; }
}
