namespace ProjectDefense.Shared.DTOs;

public record RoomDto(Guid Id, string Name, string? Number);

public record SlotDto(
    Guid Id,
    Guid RoomId,
    string RoomLabel,
    DateTime StartUtc,
    DateTime EndUtc);

public record BookSlotRequest(string ApiToken);
