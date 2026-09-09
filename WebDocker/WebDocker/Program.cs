using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using WebDocker.Client.Pages;
using WebDocker.Components;
using WebDocker.Components.Account;
using WebDocker.Data;
using WebDocker.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();


// DataBase
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    // SqlServer
    //options.UseSqlServer(connectionString));
    // SqlLite
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();


// Authentication - Authorization

// Identity
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;

    // Opciones de configuración de la contraseña
    options.Password.RequireDigit = false;              // Requiere al menos un número (0-9)
    options.Password.RequireLowercase = false;          // Requiere al menos una minúscula (a-z)
    options.Password.RequireNonAlphanumeric = false;    // Requiere al menos un carácter especial (ej: @, !, *)
    options.Password.RequireUppercase = false;          // Requiere al menos una mayúscula (A-Z)
    options.Password.RequiredLength = 4;                // Longitud mínima requerida
    options.Password.RequiredUniqueChars = 1;           // Número de caracteres únicos (generalmente 1)

    // Otras opciones de Identity, como Lockout, User, etc., se configuran aquí.
    options.SignIn.RequireConfirmedAccount = true;
})
    .AddRoles<IdentityRole>()   //Servicio de roles
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();


builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"]!,
            ValidAudience = builder.Configuration["Jwt:Audience"]!,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization();


// ------------------------------------------------------------------------------------------------
builder.Services.AddSingleton<IEmailSender<AppUser>, IdentityNoOpEmailSender>();
builder.Services.AddHttpClient();

// Docker services
builder.Services.AddScoped<ServerService>();
builder.Services.AddScoped<DockerService>();
builder.Services.AddScoped<JwtTokenService>();

// Claims personalizas con los permisos RBAC, por usuario, se añaden a la cookie de Autorizacion
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, PermissionClaimsTransformacion>();

// Auditoría de acciones (AuditLogs)
builder.Services.AddScoped<AuditLogService>();

// API REST controllers
builder.Services.AddControllers();

// CORS: permite que el cliente WebAssembly (WDockerClient), que se sirve desde
// otro origen, pueda llamar a la API REST. Usa Bearer (no cookies), así que no
// hace falta AllowCredentials. Los orígenes permitidos se leen de configuración
// (appsettings.json -> Cors:AllowedOrigins). En Azure se pueden sobrescribir con
// Application settings: Cors__AllowedOrigins__0, Cors__AllowedOrigins__1, ...
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["https://localhost:7163", "http://localhost:5072"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("WasmClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.AddFluentUIComponents(options =>
{
    options.ValidateClassNames = false;
    //options.HostingModel = BlazorHostingModel.Server;
    //options.IconConfiguration = ConfigurationGenerator.GetIconConfiguration();
    //options.EmojiConfiguration = ConfigurationGenerator.GetEmojiConfiguration();
});


// ------------------------------------------------------------------------------------------------
// SeriLog -------------------------------------------------------------------------------------------------------
Serilog.Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    //.MinimumLevel.Information()
    //.WriteTo.Console()
    //.WriteTo.File("Logs/App_.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit:5)
    .CreateLogger();

builder.Host.UseSerilog();



// ------------------------------------------------------------------------------------------------
// ------------------------------------------------------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

// .NET 9/10: sirve wwwroot, los assets de las librerías y los scripts del framework
// (incluido _framework/blazor.web.js). Sustituye al antiguo app.UseStaticFiles() de .NET 8.
app.MapStaticAssets();


// CORS debe ir antes de la autenticación/autorización para que el cliente WASM
// pueda llamar a la API REST desde su propio origen.
app.UseCors("WasmClient");

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();


// Crear un scope para resolver los servicios de Identity
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Crear la base de datos y aplicar TODAS las migraciones pendientes.
        // Sin esto, fuera de Visual Studio no existirían ni el fichero SQLite
        // ni las tablas, y el login no funcionaría.
        var db = services.GetRequiredService<AppDbContext>();

        // La BD SQLite esta en la carpeta DataBase. Las carpetas vacías no se
        // publican (Azure App Service), así que la creamos si no existe
        // antes de migrar, de lo contrario SQLite no puede crear el fichero .db.
        Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "DataBase"));

        // Crear BBDD y migraciones pendientes
        await db.Database.MigrateAsync();

        // Seed roles
        await AppDbDataSeed.SeedRoles(services);

        // Seed usuarios con roles
        await AppDbDataSeed.SeedUsers(services);

        // Seed permisos RBAC y asignación a los roles
        await AppDbDataSeed.SeedPermissions(services);

        // Seed servidores Docker
        await AppDbDataSeed.SeedDockerServers(services);

    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al sembrar los roles en la base de datos.");
    }
}


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(WebDocker.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// API REST endpoints
app.MapControllers();

app.Run();
