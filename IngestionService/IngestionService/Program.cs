using IngestionService;
using IngestionService.Assignments;
using IngestionService.Plausibility;
using IngestionService.Influx;
using IngestionService.RabbitMq;
using IngestionService.Trip;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

var grpcPort = builder.Configuration.GetValue<int?>("Grpc:Port") ?? 5010;

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Registration as singleton
builder.Services.AddSingleton<MessageHandler>();
builder.Services.AddSingleton<InfluxWriter>();
builder.Services.AddHostedService<InfluxWriteWorker>();
builder.Services.AddSingleton<MqttClientService>();
builder.Services.AddSingleton<TripClient>();
builder.Services.AddSingleton<AssignmentClient>();
builder.Services.AddSingleton<AssignmentCache>();
builder.Services.AddSingleton<GpsPlausibilityFilter>();
builder.Services.AddSingleton<PendingTripEndQueue>();
builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddSingleton<TripReactor>();
builder.Services.AddHostedService(
    sp => sp.GetRequiredService<TripReactor>());

// Background services
builder.Services.AddHostedService<PendingTripEndRetryService>();
builder.Services.AddHostedService<Worker>();

// Enable gRPC server functionality
builder.Services.AddGrpc();

var app = builder.Build();

// Server endpoint through which FleetControlApi pushes assignment changes at runtime
app.MapGrpcService<AssignmentUpdateGrpcService>();

app.Run();
