using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WDockerClient;
using WDockerClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL Base de la propia aplicación.
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Cliente HTTP apuntando a la API REST de WebDocker.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7210/";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// Servicio que envuelve las llamadas a la API y guarda el token JWT en memoria.
builder.Services.AddScoped<ApiService>();


await builder.Build().RunAsync();
