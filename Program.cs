using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GreenhouseGuard.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GreenhouseSimulator>();

var app = builder.Build();

app.UseWebSockets();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

app.MapGet("/", () => "Greenhouse Guard Server is running");

app.MapGet("/api/snapshot", (GreenhouseSimulator simulator) =>
{
    return Results.Json(simulator.GetSnapshot(), jsonOptions);
});

app.Map("/ws", async (HttpContext context, GreenhouseSimulator simulator) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    Console.WriteLine("Client connected");

    while (socket.State == WebSocketState.Open)
    {
        var messages = simulator.GetNextMessages();

        foreach (var message in messages)
        {
            var json = JsonSerializer.Serialize(message, jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        await Task.Delay(1000);
    }

    Console.WriteLine("Client disconnected");
});

app.Run("http://0.0.0.0:5050");
