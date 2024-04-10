using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Mvc;

using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using NServiceBus.Pipeline;

using System.IO;
using System.Text.Json;
var json = File.ReadAllText("appsettings.json");
var jsonObj = JsonDocument.Parse(json);

// load API key from appsettings.json
string? APIKey = null;
if (jsonObj.RootElement.TryGetProperty("APIKey", out JsonElement apiKeyElement))
{
    APIKey = apiKeyElement.GetString();
    // Now you can use APIKey in your application
}
else
{
    Console.WriteLine("APIKey not found in appsettings.json");
    return;
}

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "roll-dice";

builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName))
        .AddOtlpExporter(opt =>
          {
            //IF APM endpoint here
            opt.Endpoint = new Uri("https://otlp.immersivefusion.com");
            //API Key here
            opt.Headers = $"API-Key={APIKey}";
            //opt.Protocol = OtlpExportProtocol
          })
        .AddConsoleExporter();
});
builder.Services.AddOpenTelemetry()
      .ConfigureResource(resource => resource.AddService(serviceName))
      .WithTracing(tracing => tracing
          .AddAspNetCoreInstrumentation()
          .AddSource(NServiceBusActivitySource.ActivitySource.Name)
          .AddConsoleExporter()
          .AddOtlpExporter(opt =>
          {
            //IF APM endpoint here
            opt.Endpoint = new Uri("https://otlp.immersivefusion.com");
            //API Key here
            opt.Headers = $"API-Key={APIKey}";
            //opt.Protocol = OtlpExportProtocol
          }))
      .WithMetrics(metrics => metrics
          .AddAspNetCoreInstrumentation()
          .AddConsoleExporter());

var app = builder.Build();


string HandleRollDice([FromServices]ILogger<Program> logger, string? player)
{
using (CustomActivitySource.ActivitySource.CreateActivity("Test nested", ActivityKind.Server))
{
    var result = RollDice();

    //levels of nested activity delay
    using (CustomActivitySource.ActivitySource.StartActivity("Test further nested - really slow", ActivityKind.Server))
    {
        //something here
        Task.Delay(TimeSpan.FromMilliseconds(1000)).GetAwaiter().GetResult();
    }

    using (CustomActivitySource.ActivitySource.StartActivity("Test further nested - kind of slow", ActivityKind.Server))
    {
        //something here
        Task.Delay(TimeSpan.FromMilliseconds(500)).GetAwaiter().GetResult();
    }

    if (string.IsNullOrEmpty(player))
    {
        logger.LogInformation("Anonymous player is rolling the dice: {result}", result);
    }
    else
    {
        logger.LogInformation("{player} is rolling the dice: {result}", player, result);
    }


    return result.ToString(CultureInfo.InvariantCulture);
}
}

int RollDice()
{
    return Random.Shared.Next(1, 7);
}

app.MapGet("/rolldice/{player?}", HandleRollDice);

app.Run();

// internal activity test to enrich IF APM traces
internal static class CustomActivitySource
{
    private static readonly AssemblyName AssemblyName 
        = typeof(CustomActivitySource).Assembly.GetName();
    internal static readonly ActivitySource ActivitySource 
        = new ActivitySource(AssemblyName.Name, AssemblyName.Version.ToString());
}