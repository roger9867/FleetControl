using IngestionService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<MessageHandler>();
builder.Services.AddSingleton<InfluxWriter>();
builder.Services.AddSingleton<MqttClientService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();