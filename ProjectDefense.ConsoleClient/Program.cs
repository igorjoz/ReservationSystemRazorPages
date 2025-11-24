using System.Net.Http.Json;
using ProjectDefense.Shared.DTOs;

Console.WriteLine("Hello from ProjectDefense.ConsoleClient!");
Console.Write("Enter API URL (default: http://localhost:8080): ");
var url = Console.ReadLine();
if (string.IsNullOrWhiteSpace(url)) url = "http://localhost:8080";

using var client = new HttpClient { BaseAddress = new Uri(url) };

Console.Write("Enter your API Token: ");
var token = Console.ReadLine();

while (true)
{
    Console.WriteLine("Menu:");
    Console.WriteLine("1. List Available Slots");
    Console.WriteLine("2. Book a Slot");
    Console.WriteLine("3. Exit");
    Console.Write("Select option: ");
    var key = Console.ReadKey().KeyChar;
    Console.WriteLine();

    try
    {
        switch (key)
        {
            case '1':
                await ListSlots(client);
                break;
            case '2':
                await BookSlot(client, token);
                break;
            case '3':
                return;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static async Task ListSlots(HttpClient client)
{
    var slots = await client.GetFromJsonAsync<List<SlotDto>>("/api/slots/available");
    if (slots == null || slots.Count == 0)
    {
        Console.WriteLine("No available slots.");
        return;
    }

    Console.WriteLine("Available Slots:");
    foreach (var slot in slots)
    {
        Console.WriteLine($"ID: {slot.Id} | Room: {slot.RoomLabel} | Time: {slot.StartUtc.ToLocalTime():g} - {slot.EndUtc.ToLocalTime():t}");
    }
}

static async Task BookSlot(HttpClient client, string? token)
{
    if (string.IsNullOrWhiteSpace(token))
    {
        Console.WriteLine("Token is required.");
        return;
    }

    Console.Write("Enter Slot ID: ");
    var idStr = Console.ReadLine();
    if (!Guid.TryParse(idStr, out var id))
    {
        Console.WriteLine("Invalid ID.");
        return;
    }

    var response = await client.PostAsJsonAsync($"/api/slots/{id}/book", new BookSlotRequest(token));
    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine("Booking successful!");
    }
    else
    {
        var msg = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Booking failed: {msg}");
    }
}
