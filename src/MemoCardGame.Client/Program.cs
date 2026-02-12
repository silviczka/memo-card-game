using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MemoCardGame.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBaseUrl"]?.Trim();
var baseAddress = string.IsNullOrEmpty(apiBase)
    ? builder.HostEnvironment.BaseAddress
    : apiBase.TrimEnd('/') + "/";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(baseAddress) });
builder.Services.AddScoped<GameApiClient>();
await builder.Build().RunAsync();
