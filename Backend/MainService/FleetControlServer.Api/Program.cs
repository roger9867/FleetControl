using FleetControlServer.Api.Services;
using FleetControlServer.Data;
using FleetControlServer.Data.Repos;
using FleetControlServer.Infra;
using FleetControlServer.Service;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;



var builder = WebApplication.CreateBuilder(args);

// Kestrel: Ohne TLS kann ein Port nicht gleichzeitig HTTP/1.1 (REST) und
// HTTP/2 (gRPC) bedienen - ohne TLS gibt es kein ALPN, über das Kestrel das
// Protokoll aushandeln könnte, und fällt sonst still auf HTTP/1.1 zurück.
// Daher REST und gRPC auf getrennten Ports mit je einem Protokoll.
// Hinweis: Sobald hier ein Endpoint explizit per Code konfiguriert wird,
// ignoriert Kestrel ASPNETCORE_URLS/launchSettings vollständig - beide Ports
// müssen deshalb explizit angegeben werden.
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



// -----------------------------
// 1. Dependeny Injection
// -----------------------------
builder.Services.AddScoped<IUsbVehicleTelemetryUnit, UsbVehicleTelemetryUnit>();
builder.Services.AddScoped<ITelemetryUnitRepository, TelemetryUnitRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleDriverRepository, VehicleDriverRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<TelemetryUnitService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<PersonService>();
builder.Services.AddScoped<TripService>();

builder.Services.AddControllers();
builder.Services.AddGrpc();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200") // Angular dev server
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FleetControlServer.Api",
        Version = "v1"
    });
});

// Db Context
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


// ----------------------------------
// Middleware Swagger
// ----------------------------------
if (app.Environment.IsDevelopment())
{
    
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FleetControlServer.Api");
    });
}

app.UseHttpsRedirection();
//app.UseAuthorization();

app.UseCors("AllowAngular"); 

// Controller routen
app.MapControllers();

// gRPC Services
app.MapGrpcService<TripGrpcService>();




app.Run();
