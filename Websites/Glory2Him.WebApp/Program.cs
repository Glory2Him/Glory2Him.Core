// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.WebApp.Data;
using Glory2Him.WebApp.Infrastructure;
using Microsoft.AspNetCore.OData;

var builder = WebApplication.CreateBuilder(args);

// Last configuration source, so anything a test host supplies outranks appsettings and the
// environment. Must run before the first Services call that reads configuration.
Program.ConfigurationOverridesForTesting(builder);

// Add services to the container. The UI is the React SPA (Glory2Him.WebApp.React):
// this host serves its static build output plus the cookie-authenticated JSON APIs.
builder.Services.AddPortalIdentity(builder.Configuration);
builder.Services.AddPortalBrokers();
builder.Services.AddPortalViewServices();

// The Glory2Him.Core slice behind the exposed endpoints.
builder.Services.AddCoreServices();

// Attribute-routed controllers alongside the SPA's minimal-API endpoints. OData is added
// for the [EnableQuery] collection reads.
//
// Page size is a host posture rather than something each exposer restates, so those reads
// carry a bare [EnableQuery] and the convention writes "OData:PageSize" onto them. The
// value is read here, not inside the options callback below: that callback runs lazily, after
// the host is built, whereas this line runs downstream of ConfigurationOverridesForTesting —
// so a test host's override is settled by the time it is taken.
ODataPageSizeConvention oDataPageSizeConvention =
    ODataPageSizeConvention.FromConfiguration(builder.Configuration);

builder.Services
    .AddControllers(options =>
    {
        // MVC otherwise infers [Required] from C# nullability, which turns every non-nullable
        // member of a Core entity into a mandatory field on the wire. That is wrong for entities
        // carrying EF navigations: ApprovalComment.Approval is declared non-nullable because the
        // foreign key guarantees it in storage, but a caller never sends it — and it points back
        // at a graph containing the comment itself, so no caller could. Inferring the attribute
        // rejected every valid POST before the controller was reached.
        //
        // What replaces it is the foundation's own validation, which is where the decision belongs
        // (design §10.12 — the exposer is thin, the service decides). Note that this makes the
        // foundation's rule set the WHOLE input contract: a field no Validate(...) covers is now
        // accepted, so a missing rule there is a hole here. See issue #238 for one such gap
        // (ApprovalComment.Comment). A parameter that must be present to address the operation at
        // all is still stated explicitly with [BindRequired] — that is binding, not validation.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

        options.Conventions.Add(oDataPageSizeConvention);
    })
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .Count()
        .SetMaxTop(null));

var app = builder.Build();

// Core's schema, and the event store's participant and addresses. Both are idempotent and both
// run unconditionally: the acceptance suite boots this same host and depends on them to bring a
// fresh LocalDB up to date. An event published to an unregistered address is refused by
// EventHighway, which would surface as a 500 on every mutating Core endpoint.
await Program.InitializeCoreAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // API errors surface as plain 500s (no HTML error page — the SPA owns the UI).
    app.UseExceptionHandler(exceptionHandler =>
        exceptionHandler.Run(httpContext =>
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));

    // The default HSTS value is 30 days. You may want to change this for production scenarios,
    // see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve the React SPA build (published into wwwroot alongside the Blogzine assets) —
// deep links fall back to index.html so client-side routing owns every non-API path.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Attribute-routed controllers (api/Tags, api/ApprovalComments, ...) — registered before the
// minimal-API endpoints so a controller route wins over the /api fallback below.
app.MapControllers();

// Cookie-authenticated JSON endpoints consumed by the React SPA (Glory2Him.WebApp.React).
app.MapAccountApiEndpoints();
app.MapRegistrationApiEndpoints();
app.MapPasskeyApiEndpoints();
app.MapPostApiEndpoints();
app.MapProductApiEndpoints();
app.MapUserAdminApiEndpoints();
app.MapProfileApiEndpoints();
app.MapManageAccountApiEndpoints();
app.MapFrontendConfigurationApiEndpoints();

// Serves a user's stored profile avatar (or 404 → the UI falls back to an initials avatar).
// The URL carries a content-hash version (?v=), so the image is safely long-cached and busts
// automatically when it changes.
app.MapGet("/Profile-Image/{userId:guid}", async (
    Guid userId,
    HttpContext httpContext,
    Glory2Him.WebApp.Services.Views.Profiles.IProfileViewService profileViewService) =>
{
    Glory2Him.WebApp.Brokers.Images.ProcessedImage? image =
        await profileViewService.RetrieveProfileImageAsync(userId);

    if (image is null)
    {
        return Results.NotFound();
    }

    // The ?v= content hash changes whenever the image changes, so this URL is immutable.
    httpContext.Response.Headers.CacheControl = "private, max-age=86400";

    return Results.Bytes(image.Bytes, image.ContentType);
});

// Client-side routing owns every path no endpoint claimed — except /api, where an
// unknown route must stay a 404 rather than serving the SPA document.
app.MapFallback("/api/{**unmatchedApiPath}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

// A seed failure (e.g. a transient LocalDB cold-start error) is retried a few times so it can
// self-heal in-process; after the final attempt it is logged but must not stop the app from
// serving (the seed is idempotent and also re-runs on the next start).
const int maxSeedAttempts = 5;

for (int seedAttempt = 1; seedAttempt <= maxSeedAttempts; seedAttempt++)
{
    try
    {
        await SeedData.SeedAsync(app.Services);
        break;
    }
    catch (Exception seedException) when (seedAttempt < maxSeedAttempts)
    {
        app.Logger.LogWarning(
            seedException,
            "Identity seed attempt {Attempt}/{MaxAttempts} failed; retrying.",
            seedAttempt,
            maxSeedAttempts);

        await Task.Delay(TimeSpan.FromSeconds(2));
    }
    catch (Exception seedException)
    {
        app.Logger.LogError(
            seedException,
            "Identity seed failed after {MaxAttempts} attempts; continuing.",
            maxSeedAttempts);
    }
}

app.Run();

// Exposed so the acceptance suite's WebApplicationFactory<Program> has a concrete entry point.
public partial class Program { }
