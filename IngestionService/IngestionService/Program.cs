using IngestionService;
using IngestionService.Assignments;
using IngestionService.Influx;
using IngestionService.RabbitMq;
using IngestionService.Trip;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Nur gRPC (kein REST), daher genügt ein einzelner HTTP/2-Port ohne die
// HTTP/1.1-/HTTP/2-ALPN-Problematik, die MainService wegen seiner REST-API
// zusätzlich lösen muss.
var grpcPort = builder.Configuration.GetValue<int?>("Grpc:Port") ?? 5010;

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

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

builder.Services.AddHostedService<PendingTripEndRetryService>();
builder.Services.AddHostedService<Worker>();

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<AssignmentUpdateGrpcService>();

app.Run();
