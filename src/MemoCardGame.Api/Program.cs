using System.Diagnostics;
using System.Reflection;
using MemoCardGame.Api;
using MemoCardGame.Application;
using MemoCardGame.Infrastructure;
using MemoCardGame.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Ensure we listen on known ports when no launchSettings is used (e.g. dotnet run from CLI)
if (string.IsNullOrEmpty(builder.WebHost.GetSetting("urls")))
    builder.WebHost.UseUrls("http://localhost:5000;https://localhost:5001");

var contentRoot = builder.Environment.ContentRootPath;
var configuredSqlite = builder.Configuration["SqlitePath"];
var relativeDefault = Path.Combine("..", "..", "data", "memo.db");
var sqliteRaw = string.IsNullOrWhiteSpace(configuredSqlite) ? relativeDefault : configuredSqlite.Trim();
var sqlitePath = Path.IsPathRooted(sqliteRaw)
    ? sqliteRaw
    : Path.GetFullPath(Path.Combine(contentRoot, sqliteRaw));
var sqliteDir = Path.GetDirectoryName(sqlitePath);
if (!string.IsNullOrEmpty(sqliteDir))
    Directory.CreateDirectory(sqliteDir);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    sqlitePath,
    builder.Configuration.GetConnectionString("Default"));

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
    .Where(static o => !string.IsNullOrWhiteSpace(o))
    .Select(static o => o.Trim())
    .ToArray() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length > 0)
            policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader();
        else
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors();
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var shouldLog = path.StartsWithSegments("/games") || path.StartsWithSegments("/healthz");
    if (!shouldLog)
    {
        await next();
        return;
    }

    var sw = Stopwatch.StartNew();
    await next();
    app.Logger.LogInformation(
        "Request {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
        context.Request.Method,
        path,
        context.Response.StatusCode,
        sw.ElapsedMilliseconds);
});
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapGet("/healthz", () =>
{
    var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
    return Results.Ok(new
    {
        status = "ok",
        service = "MemoCardGame.Api",
        version
    });
});
app.MapApi();
app.MapFallbackToFile("index.html");

await app.RunAsync();
