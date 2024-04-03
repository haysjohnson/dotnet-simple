using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

const string serviceName = "roll-dice";

builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName))
        .AddConsoleExporter();
});
builder.Services.AddOpenTelemetry()
      .ConfigureResource(resource => resource.AddService(serviceName))
      .WithTracing(tracing => tracing
          .AddAspNetCoreInstrumentation()
          .AddConsoleExporter()
          .AddOtlpExporter(opt =>{
            opt.Endpoint = new Uri(
                //enter endpoint below
                "https://otlp.immersivefusion.com"
            );
            opt.Headers = $"API-Key={this.Configuration[
                //enter API key below
                "if-5e986d1a-1406-4510-8cd0-311dbf75131c"
            ]}";
            //opt.Protocol = OtlpExportProtocol
          }))
      .WithMetrics(metrics => metrics
          .AddAspNetCoreInstrumentation()
          .AddConsoleExporter());

var app = builder.Build();


string HandleRollDice([FromServices]ILogger<Program> logger, string? player)
{
    var result = RollDice();

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

int RollDice()
{
    return Random.Shared.Next(1, 7);
}

app.MapGet("/rolldice/{player?}", HandleRollDice);

app.Run();
