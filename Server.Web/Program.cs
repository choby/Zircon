using System.Reflection;
using System.Runtime;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Library;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using Server.Envir;
using Server.Web.Auth;
using Server.Web.Components;
using Server.Web.Models;
using Server.Web.Services;

Assembly configAssembly = Assembly.GetAssembly(typeof(Config))!;
ConfigReader.Load(configAssembly);
Config.LoadVersion();
Encryption.SetKey(Config.EncryptionEnabled ? Convert.FromBase64String(Config.EncryptionKey) : null);
GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

System.Net.IPAddress adminAddress = AdminEndpointValidator.ValidateAndResolve();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Listen(adminAddress, Config.AdminWebPort));

string keyDirectory = Path.Combine(AppContext.BaseDirectory, "DataProtectionKeys");
Directory.CreateDirectory(keyDirectory);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
    .SetApplicationName("Zircon.Server.Web");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "zircon.admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.Events.OnValidatePrincipal = context =>
        {
            AdminCredentialService credentials = context.HttpContext.RequestServices.GetRequiredService<AdminCredentialService>();
            string? stamp = context.Principal?.FindFirst("admin_stamp")?.Value;
            if (!string.Equals(stamp, credentials.GetSecurityStamp(), StringComparison.Ordinal))
                context.RejectPrincipal();
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingAdminAuthenticationStateProvider>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddTelerikBlazor();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("admin-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0
        }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(System.Net.IPAddress.Loopback);
    options.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
    options.ForwardLimit = 1;
});

builder.Services.AddSingleton<AdminCredentialService>();
builder.Services.AddSingleton<AdminAuditService>();
builder.Services.AddSingleton<IGameServerController, GameServerController>();
builder.Services.AddHostedService(provider => (GameServerController)provider.GetRequiredService<IGameServerController>());
builder.Services.AddSingleton<ServerMetricsService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ServerMetricsService>());
builder.Services.AddSingleton<LogBufferService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<LogBufferService>());
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<GameDataViewCatalog>();
builder.Services.AddSingleton<GameDataSessionService>();
builder.Services.AddSingleton<PluginRegistry>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<PluginRegistry>());
builder.Services.AddSingleton<MapDataService>();
builder.Services.AddSingleton<MapImageService>();
builder.Services.AddSingleton<RuntimeDataService>();
builder.Services.AddSingleton<OrphanDiagnosticService>();

WebApplication app = builder.Build();

// System.db must be initialized before an administrator can start the game loop.
// Resolving the singleton here applies the same defaults/migrations that the old SMain load path applied.
_ = app.Services.GetRequiredService<GameDataSessionService>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/auth/login", async (HttpContext context, IAntiforgery antiforgery, AdminCredentialService credentials, AdminAuditService audit) =>
{
    await antiforgery.ValidateRequestAsync(context);
    IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted);
    string userName = form["username"].ToString();
    string password = form["password"].ToString();

    if (!credentials.Validate(userName, password))
    {
        audit.Record(userName, "LoginFailed", "Invalid administrator credentials", context.Connection.RemoteIpAddress?.ToString());
        return Results.Redirect("/login?error=1");
    }

    Claim[] claims =
    [
        new(ClaimTypes.Name, credentials.UserName),
        new(ClaimTypes.Role, "Administrator"),
        new("admin_stamp", credentials.GetSecurityStamp())
    ];
    ClaimsPrincipal principal = new(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
    {
        IsPersistent = false,
        AllowRefresh = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
    });
    audit.Record(credentials.UserName, "Login", "Administrator signed in", context.Connection.RemoteIpAddress?.ToString());
    return Results.Redirect("/");
}).RequireRateLimiting("admin-login");

app.MapPost("/auth/logout", async (HttpContext context, IAntiforgery antiforgery, AdminAuditService audit) =>
{
    await antiforgery.ValidateRequestAsync(context);
    audit.Record(context.User.Identity?.Name ?? "unknown", "Logout", "Administrator signed out", context.Connection.RemoteIpAddress?.ToString());
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).RequireAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "live", time = DateTimeOffset.UtcNow }));

app.MapGet("/api/maps/{fileName}", async (string fileName, MapDataService maps, CancellationToken cancellationToken) =>
    Results.Json(await maps.LoadAsync(fileName, cancellationToken))).RequireAuthorization();

app.MapGet("/api/map-assets/status", (MapImageService images, HttpContext context) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Json(images.GetStatus());
    })
    .RequireAuthorization();

app.MapGet("/api/map-assets/{mapFile:int}/{imageIndex:int}",
    (int mapFile, int imageIndex, MapImageService images, HttpContext context) =>
    {
        try
        {
            MapImageLookup lookup = images.GetImage(mapFile, imageIndex);
            if (lookup.State == MapImageState.Empty)
            {
                context.Response.Headers.CacheControl = "private,max-age=31536000,immutable";
                context.Response.Headers["X-Map-Asset-State"] = "empty";
                return Results.NoContent();
            }
            if (lookup.State == MapImageState.MissingLibrary || lookup.Image is null) return Results.NotFound();
            MapImageResult image = lookup.Image;
            context.Response.ContentLength = image.Content.Length;
            context.Response.Headers.ETag = image.ETag;
            context.Response.Headers.CacheControl = "private,max-age=31536000,immutable";
            context.Response.Headers.ContentDisposition = "inline";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Map-Asset-Decoder"] = MapImageService.DecoderVersion;
            return Results.Bytes(image.Content, "image/png");
        }
        catch (Exception ex)
        {
            return Results.Problem($"地图贴图解码失败：{ex.Message}");
        }
    }).RequireAuthorization();

app.MapGet("/plugin-assets/{pluginId}/{**assetPath}", (string pluginId, string assetPath, PluginRegistry plugins) =>
{
    string? path = plugins.ResolveAsset(pluginId, assetPath);
    return path is null ? Results.NotFound() : Results.File(path);
}).RequireAuthorization();

app.MapGet("/api/map-regions/{regionIndex:int}/points", async (
    int regionIndex, int width, GameDataSessionService catalog, CancellationToken cancellationToken) =>
    Results.Json(await catalog.GetMapRegionPointsAsync(regionIndex, width, cancellationToken))).RequireAuthorization();

app.MapPut("/api/map-regions/{regionIndex:int}", async (
    int regionIndex,
    SaveMapRegionRequest request,
    HttpContext context,
    GameDataSessionService catalog,
    CancellationToken cancellationToken) =>
{
    string etag = await catalog.SaveMapRegionAsync(regionIndex, request.Width, request.Height, request.ETag, request.Points,
        context.User.Identity?.Name ?? "unknown", cancellationToken);
    return Results.Json(new { etag });
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStopping.Register(() =>
{
    try { ConfigReader.Save(configAssembly); }
    catch { /* graceful shutdown must continue */ }
});

app.Run();

public sealed record SaveMapRegionRequest(int Width, int Height, string ETag, IReadOnlyList<MapCellPoint> Points);
