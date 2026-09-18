// // ============================================================================
// // TEMPORARY DEVELOPMENT / VALIDATION HOST — NOT THE PRODUCTION HOST.
// //
// // This ASP.NET Core (net8.0) project exists solely so the reusable One77.Core
// // class library (netstandard2.0) can be exercised end-to-end against a real
// // SQL Server instance in this development sandbox for Milestones 1-8.
// //
// // It intentionally contains almost no logic of its own: business rules, data
// // access, and domain code all live in One77.Core so they remain consumable by
// // whichever production runtime (.NET Framework 4.8 or .NET Core 5) is
// // approved at the Phase 3 Section 11 checkpoint. Replacing this host later
// // must not require touching One77.Core.
// // ============================================================================

// using System.Text.Json;
// using Microsoft.AspNetCore.Diagnostics;
// using One77.Api.DevHost.Admin;
// using One77.Api.DevHost.Bundles;
// using One77.Api.DevHost.Enrichment;
// using One77.Api.DevHost.Homepage;
// using One77.Api.DevHost.Learn;
// using One77.Api.DevHost.Match;
// using One77.Api.DevHost.Webinar;
// using One77.Core.Admin;
// using One77.Core.Bundles;
// using One77.Core.Data;
// using One77.Core.Diagnostics;
// using One77.Core.Enrichment;
// using One77.Core.Homepage;
// using One77.Core.Learn;
// using One77.Core.Match;
// using One77.Core.Webinar;
// using One77.Api.DevHost;

// var builder = WebApplication.CreateBuilder(args);

// // JSON casing decision for this milestone: snake_case, matching the field
// // names spelled out explicitly in the Milestone 3 API spec (sort_priority,
// // entry_count, category_id, category_name, ...). Applied via the framework's
// // own built-in policy — no custom serializer.
// builder.Services.ConfigureHttpJsonOptions(options =>
// {
//     options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
// });

// builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
// {
//     var connectionString = builder.Configuration.GetConnectionString("Default")
//         ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
//     return new SqlConnectionFactory(connectionString);
// });
// builder.Services.AddSingleton<DatabaseConnectivityChecker>();

// builder.Services.AddSingleton<LearnRepository>();
// builder.Services.AddSingleton<LearnCategoryService>();
// builder.Services.AddSingleton<LearnEntryService>();

// builder.Services.AddSingleton<WebinarRepository>();
// builder.Services.AddSingleton<WebinarEventService>();
// builder.Services.AddSingleton<WebinarRegistrationService>();

// builder.Services.AddSingleton<MatchRepository>();
// builder.Services.AddSingleton<MatchAdminService>();
// builder.Services.AddSingleton<MatchPublicResolver>();

// builder.Services.AddSingleton<EnrichmentRepository>();
// builder.Services.AddSingleton<EnrichmentService>();

// builder.Services.AddSingleton<BundleRepository>();
// builder.Services.AddSingleton<BundleService>();

// // Media root: not IIS, so there is no "site physical root" — defaults to a
// // "media" folder next to this project's own source (gitignored; never a
// // deployment artifact). Configurable the same way as production, so the
// // same HomepageMediaStorage class is exercised identically by both hosts.
// var homepageMediaRoot = builder.Configuration["Homepage:MediaPhysicalRoot"];
// if (string.IsNullOrWhiteSpace(homepageMediaRoot))
// {
//     homepageMediaRoot = Path.Combine(builder.Environment.ContentRootPath, "media");
// }
// var homepageMediaMaxBytes = builder.Configuration.GetValue("Homepage:MediaMaxBytes", 8 * 1024 * 1024L);

// builder.Services.AddSingleton<HomepageConfigRepository>();
// builder.Services.AddSingleton(new HomepageMediaStorage(homepageMediaRoot, "/media", homepageMediaMaxBytes));
// builder.Services.AddSingleton<HomepageConfigService>();

// builder.Services.AddSingleton<AdminUserRepository>();
// builder.Services.AddSingleton<AdminAuthService>(sp =>
//     new AdminAuthService(sp.GetRequiredService<AdminUserRepository>(), new BCryptPasswordHasher()));
// builder.Services.AddSingleton<JwtTokenService>(sp =>
// {
//     var config = sp.GetRequiredService<IConfiguration>();
//     var signingSecret = config["Jwt:SigningSecret"]
//         ?? throw new InvalidOperationException("Jwt:SigningSecret is not configured.");
//     return new JwtTokenService(new JwtOptions
//     {
//         SigningSecret = signingSecret,
//         Issuer = config["Jwt:Issuer"] ?? "one77-api",
//         Audience = config["Jwt:Audience"] ?? "one77-admin",
//         ExpiresAfter = TimeSpan.FromMinutes(config.GetValue("Jwt:ExpiryMinutes", 720))
//     });
// });

// // Dev-only CORS so the Angular dev server (localhost:4200) can call this
// // host (localhost:5299) directly during local development — browsers block
// // cross-origin requests by default, even though curl/server-to-server
// // calls are unaffected. Not applicable to a real production deployment,
// // where the Angular app and API would typically share an origin or sit
// // behind a reverse proxy.
// const string DevCorsPolicy = "DevCors";
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(DevCorsPolicy, policy =>
//         policy.WithOrigins("http://localhost:4200")
//               .AllowAnyHeader()
//               .AllowAnyMethod());
// });

// var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
//     app.UseCors(DevCorsPolicy);
// }

// // Single, tiny, shared error mapping for the whole host: 404 for "not
// // found", 400 for business validation, 500 (no internal detail) for
// // anything else. Deliberately not a framework — just one middleware.
// app.UseExceptionHandler(errorApp =>
// {
//     errorApp.Run(async context =>
//     {
//         var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
//         if (error is LearnNotFoundException or WebinarNotFoundException or MatchNotFoundException or EnrichmentNotFoundException or BundleNotFoundException)
//         {
//             context.Response.StatusCode = StatusCodes.Status404NotFound;
//             await context.Response.WriteAsJsonAsync(new ErrorResponse { Error = error.Message });
//         }
//         else if (error is LearnValidationException or WebinarValidationException or MatchValidationException or EnrichmentValidationException or BundleValidationException or HomepageValidationException)
//         {
//             context.Response.StatusCode = StatusCodes.Status400BadRequest;
//             await context.Response.WriteAsJsonAsync(new ErrorResponse { Error = error.Message });
//         }
//         else if (error is AdminAuthenticationException)
//         {
//             context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//             await context.Response.WriteAsJsonAsync(new ErrorResponse { Error = error.Message });
//         }
//         else
//         {
//             context.Response.StatusCode = StatusCodes.Status500InternalServerError;
//             await context.Response.WriteAsJsonAsync(new ErrorResponse { Error = "An unexpected error occurred." });
//         }
//     });
// });

// // Serves the uploaded homepage hero photograph at /media/<file>, the same
// // same-origin static-file mechanism production IIS uses for it — the one
// // deliberate parity point between the two hosts' otherwise very different
// // static-file setups (DevHost has no other static content of its own; the
// // Angular dev server serves everything else on its own port).
// if (!Directory.Exists(homepageMediaRoot))
// {
//     Directory.CreateDirectory(homepageMediaRoot);
// }
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(homepageMediaRoot),
//     RequestPath = "/media",
// });

// app.UseAdminJwtAuth();

// app.MapLearnEndpoints();
// app.MapWebinarEndpoints();
// app.MapMatchEndpoints();
// app.MapEnrichmentEndpoints();
// app.MapBundleEndpoints();
// app.MapAdminAuthEndpoints();
// app.MapHomepageEndpoints();

// // First-admin seed: safe to run on every start (no-op once any admin
// // exists), only acts when both values are configured — same pattern as
// // One77.Api.WebApi48's Global.asax. Dev-only credentials belong in the
// // gitignored appsettings.Development.json, never in the committed template.
// using (var scope = app.Services.CreateScope())
// {
//     var initialEmail = app.Configuration["InitialAdmin:Email"];
//     var initialPassword = app.Configuration["InitialAdmin:Password"];
//     if (!string.IsNullOrWhiteSpace(initialEmail) && !string.IsNullOrWhiteSpace(initialPassword))
//     {
//         var authService = scope.ServiceProvider.GetRequiredService<AdminAuthService>();
//         await authService.EnsureSeedAdminAsync(initialEmail, initialPassword, app.Configuration["InitialAdmin:Name"] ?? "Admin");
//     }
// }

// app.MapGet("/health", () => Results.Ok(new { status = "ok", host = "dev-only" }));

// app.MapGet("/health/db", async (DatabaseConnectivityChecker checker) =>
// {
//     try
//     {
//         var connected = await checker.CanConnectAsync();
//         return connected
//             ? Results.Ok(new { status = "ok", database = "reachable" })
//             : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
//     }
//     catch (Exception ex)
//     {
//         return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unreachable");
//     }
// });

// app.Run();







// ============================================================================
// TEMPORARY DEVELOPMENT / VALIDATION HOST — NOT THE PRODUCTION HOST.
//
// This ASP.NET Core (net8.0) project exists solely so the reusable One77.Core
// class library (netstandard2.0) can be exercised end-to-end against a real
// SQL Server instance in this development sandbox for Milestones 1-8.
//
// It intentionally contains almost no logic of its own: business rules, data
// access, and domain code all live in One77.Core so they remain consumable by
// whichever production runtime (.NET Framework 4.8 or .NET Core 5) is
// approved at the Phase 3 Section 11 checkpoint. Replacing this host later
// must not require touching One77.Core.
// ============================================================================

using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using One77.Api.DevHost.Admin;
using One77.Api.DevHost.Bundles;
using One77.Api.DevHost.Enrichment;
using One77.Api.DevHost.Homepage;
using One77.Api.DevHost.Learn;
using One77.Api.DevHost.Match;
using One77.Api.DevHost.Orders;
using One77.Api.DevHost.Webinar;
using One77.Core.Admin;
using One77.Core.Bundles;
using One77.Core.Data;
using One77.Core.Diagnostics;
using One77.Core.Enrichment;
using One77.Core.Homepage;
using One77.Core.Learn;
using One77.Core.Match;
using One77.Core.Notifications;
using One77.Core.Webinar;
using One77.Api.DevHost;

var builder = WebApplication.CreateBuilder(args);

// JSON casing decision for this milestone: snake_case, matching the field
// names spelled out explicitly in the Milestone 3 API spec (sort_priority,
// entry_count, category_id, category_name, ...). Applied via the framework's
// own built-in policy — no custom serializer.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
    return new SqlConnectionFactory(connectionString);
});
builder.Services.AddSingleton<DatabaseConnectivityChecker>();

builder.Services.AddSingleton<LearnRepository>();
builder.Services.AddSingleton<LearnCategoryService>();
builder.Services.AddSingleton<LearnEntryService>();

builder.Services.AddSingleton<WebinarRepository>();
builder.Services.AddSingleton<WebinarEventService>();
builder.Services.AddSingleton<WebinarRegistrationService>();

builder.Services.AddSingleton<MatchRepository>();
builder.Services.AddSingleton<MatchAdminService>();
builder.Services.AddSingleton<MatchPublicResolver>();

builder.Services.AddSingleton<EnrichmentRepository>();
builder.Services.AddSingleton<EnrichmentService>();

builder.Services.AddSingleton<BundleRepository>();
builder.Services.AddSingleton<BundleService>();

// Order confirmation notifications (WhatsApp via Meta Cloud API + SMS via
// MSG91) — triggered by the Shopify orders/paid webhook, not by anything in
// this host's own request flow. See One77.Core/Notifications and
// One77.Api.DevHost/Orders. Both provider clients get their own named
// HttpClient (AddHttpClient<T>) so connection pooling/DNS refresh is handled
// by the framework rather than each sender owning its own HttpClient.
var notificationOptions = new OrderNotificationOptions();
builder.Configuration.GetSection("Notifications").Bind(notificationOptions);
builder.Services.AddSingleton(notificationOptions);

builder.Services.AddHttpClient<IWhatsAppSender, MetaWhatsAppCloudApiSender>()
    .Services.AddSingleton(notificationOptions.WhatsApp);
builder.Services.AddHttpClient<ISmsSender, LoadCrmSmsSender>()
    .Services.AddSingleton(notificationOptions.Sms);

// Scoped, not Singleton — IWhatsAppSender/ISmsSender are typed HttpClients
// (AddHttpClient<T>), which are meant to be resolved per-request/per-scope
// rather than captured once for the app's whole lifetime.
builder.Services.AddScoped(sp =>
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("OrderNotificationService");
    return new OrderNotificationService(
        sp.GetRequiredService<IWhatsAppSender>(),
        sp.GetRequiredService<ISmsSender>(),
        notificationOptions,
        (message, ex) => logger.LogError(ex, "{Message}", message));
});

// Media root: not IIS, so there is no "site physical root" — defaults to a
// "media" folder next to this project's own source (gitignored; never a
// deployment artifact). Configurable the same way as production, so the
// same HomepageMediaStorage class is exercised identically by both hosts.
var homepageMediaRoot = builder.Configuration["Homepage:MediaPhysicalRoot"];
if (string.IsNullOrWhiteSpace(homepageMediaRoot))
{
    homepageMediaRoot = Path.Combine(builder.Environment.ContentRootPath, "media");
}
var homepageMediaMaxBytes = builder.Configuration.GetValue("Homepage:MediaMaxBytes", 8 * 1024 * 1024L);

builder.Services.AddSingleton<HomepageConfigRepository>();
builder.Services.AddSingleton(new HomepageMediaStorage(homepageMediaRoot, "/media", homepageMediaMaxBytes));
builder.Services.AddSingleton<HomepageConfigService>();

builder.Services.AddSingleton<AdminUserRepository>();
builder.Services.AddSingleton<AdminAuthService>(sp =>
    new AdminAuthService(sp.GetRequiredService<AdminUserRepository>(), new BCryptPasswordHasher()));
builder.Services.AddSingleton<JwtTokenService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var signingSecret = config["Jwt:SigningSecret"]
        ?? throw new InvalidOperationException("Jwt:SigningSecret is not configured.");
    return new JwtTokenService(new JwtOptions
    {
        SigningSecret = signingSecret,
        Issuer = config["Jwt:Issuer"] ?? "one77-api",
        Audience = config["Jwt:Audience"] ?? "one77-admin",
        ExpiresAfter = TimeSpan.FromMinutes(config.GetValue("Jwt:ExpiryMinutes", 720))
    });
});

// Dev-only CORS so the Angular dev server (localhost:4200) can call this
// host (localhost:5299) directly during local development — browsers block
// cross-origin requests by default, even though curl/server-to-server
// calls are unaffected. Not applicable to a real production deployment,
// where the Angular app and API would typically share an origin or sit
// behind a reverse proxy.
const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicy);
}

// Single, tiny, shared error mapping for the whole host: 404 for "not
// found", 400 for business validation, 500 (no internal detail) for
// anything else. Deliberately not a framework — just one middleware.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (error is LearnNotFoundException or WebinarNotFoundException or MatchNotFoundException or EnrichmentNotFoundException or BundleNotFoundException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Error = error.Message });
        }
        else if (error is LearnValidationException or WebinarValidationException or MatchValidationException or EnrichmentValidationException or BundleValidationException or HomepageValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Error = error.Message });
        }
        else if (error is AdminAuthenticationException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Error = error.Message });
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Error = "An unexpected error occurred." });
        }
    });
});

// Serves the uploaded homepage hero photograph at /media/<file>, the same
// same-origin static-file mechanism production IIS uses for it — the one
// deliberate parity point between the two hosts' otherwise very different
// static-file setups (DevHost has no other static content of its own; the
// Angular dev server serves everything else on its own port).
if (!Directory.Exists(homepageMediaRoot))
{
    Directory.CreateDirectory(homepageMediaRoot);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(homepageMediaRoot),
    RequestPath = "/media",
});

app.UseAdminJwtAuth();

app.MapLearnEndpoints();
app.MapWebinarEndpoints();
app.MapMatchEndpoints();
app.MapEnrichmentEndpoints();
app.MapBundleEndpoints();
app.MapAdminAuthEndpoints();
app.MapHomepageEndpoints();
app.MapOrderWebhookEndpoints();

// First-admin seed: safe to run on every start (no-op once any admin
// exists), only acts when both values are configured — same pattern as
// One77.Api.WebApi48's Global.asax. Dev-only credentials belong in the
// gitignored appsettings.Development.json, never in the committed template.
using (var scope = app.Services.CreateScope())
{
    var initialEmail = app.Configuration["InitialAdmin:Email"];
    var initialPassword = app.Configuration["InitialAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(initialEmail) && !string.IsNullOrWhiteSpace(initialPassword))
    {
        var authService = scope.ServiceProvider.GetRequiredService<AdminAuthService>();
        await authService.EnsureSeedAdminAsync(initialEmail, initialPassword, app.Configuration["InitialAdmin:Name"] ?? "Admin");
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", host = "dev-only" }));

app.MapGet("/health/db", async (DatabaseConnectivityChecker checker) =>
{
    try
    {
        var connected = await checker.CanConnectAsync();
        return connected
            ? Results.Ok(new { status = "ok", database = "reachable" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unreachable");
    }
});

app.Run();
