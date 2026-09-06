using Carter;
using DotNetEnv;
using Scalar.AspNetCore;
using ZKTecoGatway.ZKTeco;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<IZKemSessionFactory, ZkemSessionFactory>();
builder.Services.AddScoped<ZKTecoAttendanceMachineReader>();

Env.Load(Path.Combine(AppContext.BaseDirectory, ".env"));

var port = Environment.GetEnvironmentVariable("PORT") ?? "8000";

builder.Services.AddCarter();
builder.WebHost.UseUrls(
    $"http://0.0.0.0:{port}"
);
var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.MapCarter();


app.Run();
