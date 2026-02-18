using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using EasySave.Core;
using EasySave.LogServer.Models;
using EasySave.LogServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var logFormatStr = builder.Configuration.GetValue<string>("LogFormat") ?? "Json";
var logFormat = Enum.Parse<LogFormat>(logFormatStr, ignoreCase: true);
var logDirectory = builder.Configuration.GetValue<string>("LogDirectory") ?? "/app/logs";
var clientsFilePath = builder.Configuration.GetValue<string>("ClientsFile") ?? "/app/data/clients.json";

var pathProvider = new ServerPathProvider(logDirectory);

builder.Services.AddSingleton<IClientRegistry>(new JsonClientRegistry(clientsFilePath));
builder.Services.AddSingleton(new ServerLogWriter(pathProvider, logFormat));

var macAddressRegex = new Regex(@"^([0-9A-Fa-f]{2}[:\-]){5}[0-9A-Fa-f]{2}$", RegexOptions.Compiled);

var app = builder.Build();

// POST /api/logs - Receive a log entry from an EasySave client
app.MapPost("/api/logs", (LogEntryRequest request, ServerLogWriter writer, IClientRegistry clients) =>
{
    if (string.IsNullOrWhiteSpace(request.MacAddress))
        return Results.BadRequest(new { error = "MacAddress is required." });
    if (!macAddressRegex.IsMatch(request.MacAddress))
        return Results.BadRequest(new { error = "MacAddress must be a valid MAC address (e.g. AA:BB:CC:DD:EE:FF)." });
    if (string.IsNullOrWhiteSpace(request.BackupName))
        return Results.BadRequest(new { error = "BackupName is required." });
    if (string.IsNullOrWhiteSpace(request.EventType))
        return Results.BadRequest(new { error = "EventType is required." });

    clients.EnsureRegistered(request.MacAddress);
    var clientName = clients.GetFriendlyName(request.MacAddress);

    var enriched = new EnrichedLogEntry(
        request.Timestamp,
        request.BackupId,
        request.BackupName,
        request.EventType,
        request.SourcePathUNC,
        request.DestinationPathUNC,
        request.FileSizeBytes,
        request.TransferTimeMs,
        request.EncryptionTimeMs,
        request.MacAddress,
        clientName);

    LogFormat? clientFormat = null;
    if (!string.IsNullOrWhiteSpace(request.LogFormat)
        && Enum.TryParse<LogFormat>(request.LogFormat, ignoreCase: true, out var parsed))
    {
        clientFormat = parsed;
    }

    try
    {
        writer.Write(enriched, clientFormat);
    }
    catch (TimeoutException)
    {
        return Results.StatusCode(503);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to write log entry: {ex.Message}");
        return Results.StatusCode(500);
    }

    return Results.Created();
});

// GET /api/config - Return server log format
app.MapGet("/api/config", () => Results.Ok(new { logFormat = logFormat.ToString() }));

// GET /api/clients - List all known clients
app.MapGet("/api/clients", (IClientRegistry clients) => Results.Ok(clients.GetAll()));

// PUT /api/clients/{macAddress} - Set a client's friendly name
app.MapPut("/api/clients/{macAddress}", (string macAddress, UpdateClientNameRequest body, IClientRegistry clients) =>
{
    if (string.IsNullOrWhiteSpace(macAddress) || !macAddressRegex.IsMatch(macAddress))
        return Results.BadRequest(new { error = "macAddress must be a valid MAC address (e.g. AA:BB:CC:DD:EE:FF)." });
    if (string.IsNullOrWhiteSpace(body.FriendlyName))
        return Results.BadRequest(new { error = "FriendlyName is required." });

    clients.SetFriendlyName(macAddress, body.FriendlyName);
    return Results.NoContent();
});

app.Run();
