using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

//builder.Services.AddHttpClient();

//builder.Services.AddFluentUIComponents(options =>
//{
//    options.ValidateClassNames = false;
//    //options.HostingModel = BlazorHostingModel.Server;
//    //options.IconConfiguration = ConfigurationGenerator.GetIconConfiguration();
//    //options.EmojiConfiguration = ConfigurationGenerator.GetEmojiConfiguration();
//});

await builder.Build().RunAsync();
