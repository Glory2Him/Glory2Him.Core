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
    // These actions return ActionResult<IQueryable<T>> on ordinary attribute routes rather than
    // through an OData route, so the response is a plain JSON array: no @odata.nextLink, and
    // $count=true adds no total because there is nowhere in that shape to put one. The page size
    // above is therefore a CAP, not a cursor — a caller receiving exactly PageSize rows cannot
    // tell a full collection from a truncated one. $skip does work, so the rest is reachable by a
    // caller that already knows to ask for it. Giving clients a truncation signal means changing
    // the response shape, which is a contract decision for the SPA and not one taken here.
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
app.MapContributorApiEndpoints();

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

// Both steps retry, because the failure they were written for is a transient LocalDB
// cold-start that clears on its own. They differ in what happens after the last attempt, and
// the difference is the point.
const int maxSeedAttempts = 5;

// THE MIGRATION IS A PRECONDITION FOR SERVING, and this is a change from when it sat inside the
// seed's swallow. It was safe there while the seed only ever ADDED rows: a role that failed to
// appear was a missing grant, the site was otherwise correct, and the next start fixed it.
//
// Since #368 a migration also RENAMES the role rows. If it does not run, AspNetRoles still says
// Reviewer / Publisher / Tag-Reviewer while every gate in the deployed code composes Reviewers /
// Publishers / Tag-Reviewers, and the two never meet: no exception, no failed request, just
// every reviewer and publisher on the site silently holding a row nothing asks for. The seed
// cannot repair it either — it aborts at the same statement, so the plural rows are never minted
// and an administrator has no name to grant. Worst of all it is invisible from the one surface
// an administrator would check, because /api/admin is gated on "Administrators", whose row
// predates the migration and still resolves.
//
// A site that cannot speak its own authorization vocabulary must not answer requests. So the
// last attempt's exception is left to propagate and stop the host, where a log line saying
// "continuing" would have been read by nobody until somebody asked why approvals were refusing
// everyone.
for (int migrationAttempt = 1; migrationAttempt <= maxSeedAttempts; migrationAttempt++)
{
    try
    {
        await SeedData.MigrateAsync(app.Services);
        break;
    }
    catch (Exception migrationException) when (migrationAttempt < maxSeedAttempts)
    {
        app.Logger.LogWarning(
            migrationException,
            "Identity migration attempt {Attempt}/{MaxAttempts} failed; retrying.",
            migrationAttempt,
            maxSeedAttempts);

        await Task.Delay(TimeSpan.FromSeconds(2));
    }
    catch (Exception migrationException)
    {
        app.Logger.LogCritical(
            migrationException,
            "Identity migration failed after {MaxAttempts} attempts; refusing to serve, because "
                + "the role rows would not spell the vocabulary this build authorizes against.",
            maxSeedAttempts);

        throw;
    }
}

// The seed itself keeps the original posture, and for the original reason: every step of it is
// idempotent and additive, so a failure leaves the site short a row rather than wrong about one,
// and the next start re-runs it.
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
