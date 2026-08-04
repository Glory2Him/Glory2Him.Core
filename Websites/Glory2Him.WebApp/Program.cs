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

using Glory2Him.WebApp.Components;
using Glory2Him.WebApp.Data;
using Glory2Him.WebApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddPortalIdentity(builder.Configuration);
builder.Services.AddPortalBrokers();
builder.Services.AddPortalViewServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios,
    // see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Not-Found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Cookie-authenticated JSON endpoints consumed by the React SPA (Glory2Him.WebApp.React).
app.MapAccountApiEndpoints();
app.MapRegistrationApiEndpoints();
app.MapPostApiEndpoints();
app.MapProductApiEndpoints();
app.MapUserAdminApiEndpoints();
app.MapProfileApiEndpoints();

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
