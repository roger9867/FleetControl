using FleetControlServer.Data;
using FleetControlServer.Data.Repos;
using FleetControlServer.Infra;
using FleetControlServer.Service;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;



var builder = WebApplication.CreateBuilder(args);



// -----------------------------
// 1. Dependeny Injection
// -----------------------------
builder.Services.AddScoped<IUsbVehicleTelemetryUnit, UsbVehicleTelemetryUnit>();
builder.Services.AddScoped<ITelemetryUnitRepository, TelemetryUnitRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<TelemetryUnitService>();
builder.Services.AddScoped<VehicleService>();

builder.Services.AddControllers();


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




app.Run();
