using MemoCardGame.Api;
using MemoCardGame.Application;
using MemoCardGame.Infrastructure;
using MemoCardGame.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Ensure we listen on known ports when no launchSettings is used (e.g. dotnet run from CLI)
if (string.IsNullOrEmpty(builder.WebHost.GetSetting("urls")))
    builder.WebHost.UseUrls("http://localhost:5000;https://localhost:5001");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration["SqlitePath"] ?? "memo.db");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        // For production, replace with policy.WithOrigins("https://your-frontend.com") etc.
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapApi();
app.MapFallbackToFile("index.html");

await app.RunAsync();
