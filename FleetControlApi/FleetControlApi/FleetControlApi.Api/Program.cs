using FleetControlApi.Gateway.Realtime;
using FleetControlApi.Gateway.SignalR;
using FleetControlApi.Persistence;
using FleetControlApi.Persistence.Repos;
using FleetControlApi.Service.Repositories;
using FleetControlApi.Gateway.Grpc.Services;
using FleetControlApi.Service;
using FleetControlApi.Service.Assignments;
using FleetControlApi.Service.Notifications;
using FleetControlApi.Service.Telemetry;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RabbitMQ.Client;



var builder = WebApplication.CreateBuilder(args);

var httpPort = builder.Configuration.GetValue<int?>("Http:Port") ?? 5000;
var grpcPort = builder.Configuration.GetValue<int?>("Grpc:Port") ?? 5001;

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(httpPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });

    options.ListenAnyIP(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});



// Dependency injection

builder.Services.AddScoped<ITelemetryUnitRepository, TelemetryUnitRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleDriverRepository, VehicleDriverRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<TelemetryUnitService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<TripService>();
// gRPC clients: the generated clients are created by the gRPC client factory,
// the Gateway classes receive them via their constructor
// (plain-text HTTP/2 without TLS between the services)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
builder.Services.AddGrpcClient<FleetControlApi.Gateway.Grpc.TelemetryQuery.TelemetryQueryClient>(o =>
    o.Address = new Uri(builder.Configuration["TelemetryDataService:Url"]!));
builder.Services.AddGrpcClient<FleetControlApi.Gateway.Grpc.AssignmentUpdateService.AssignmentUpdateServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["IngestionService:Url"]!));
builder.Services.AddSingleton<ITelemetryQueryClient, TelemetryQueryClient>();
builder.Services.AddSingleton<IIngestionServiceClient, IngestionServiceClient>();

// RabbitMQ connection settings for the live update consumer
builder.Services.AddSingleton<IConnectionFactory>(new ConnectionFactory
{
    HostName = builder.Configuration["RabbitMq:Host"] ?? "localhost",
    Port = int.Parse(builder.Configuration["RabbitMq:Port"] ?? "5672"),
    UserName = builder.Configuration["RabbitMq:Username"] ?? "guest",
    Password = builder.Configuration["RabbitMq:Password"] ?? "guest"
});
builder.Services.AddScoped<ITripNotifier, SignalRTripNotifier>();
builder.Services.AddScoped<IAssignmentPushService, AssignmentPushService>();

// REST controllers, gRPC, SignalR and the RabbitMQ live update consumer as a background service.
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddSignalR();
builder.Services.AddHostedService<RabbitMqTelemetryConsumer>();


// CORS: only allows requests from the known Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://74.248.232.214:4200",
                "https://fleetcontrol.polandcentral.cloudapp.azure.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FleetControlApi.Api",
        Version = "v1"
    });
});

// Db context: registers the EF Core context with the PostgreSQL provider.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

var app = builder.Build();


// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?
            .Error;

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            title = "Internal Server Error",
            status = 500,
            detail = exception?.Message
        });
    });
});


// Middleware Swagger
if (app.Environment.IsDevelopment())
{
    
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FleetControlApi.Api");
    });
}

app.UseHttpsRedirection();
//app.UseAuthorization();

// Enables the CORS policy defined above for all incoming requests.
app.UseCors("AllowAngular");

// Route controllers
app.MapControllers();

// gRPC Services
app.MapGrpcService<TripGrpcService>();
app.MapGrpcService<AssignmentGrpcService>();

// SignalR hub for live updates (vehicle movement state)
app.MapHub<VehicleHub>("/hubs/vehicles");

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.Run();
