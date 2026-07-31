using IngestionService;
using IngestionService.Influx;
using IngestionService.Trip;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<MessageHandler>();
builder.Services.AddSingleton<InfluxWriter>();
builder.Services.AddHostedService<InfluxWriteWorker>();
builder.Services.AddSingleton<MqttClientService>();
builder.Services.AddSingleton<TripClient>();
builder.Services.AddSingleton<PendingTripEndQueue>();

builder.Services.AddSingleton<TripReactor>();
builder.Services.AddHostedService(
    sp => sp.GetRequiredService<TripReactor>());

builder.Services.AddHostedService<PendingTripEndRetryService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();