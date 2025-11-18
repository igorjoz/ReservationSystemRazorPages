namespace ProjectDefense.Shared.Entities;

public class Room
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Number { get; set; }
}
