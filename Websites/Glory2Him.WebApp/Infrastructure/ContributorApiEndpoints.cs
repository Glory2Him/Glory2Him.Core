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

using Glory2Him.WebApp.Models.Views.Profiles;
using Glory2Him.WebApp.Services.Views.Profiles;

namespace Glory2Him.WebApp.Infrastructure
{
    // WHO SUBMITTED A PUBLISHED CONTRIBUTION, for the byline on /posts/{id}.
    //
    // A ContentItem records its contributor as CreatedBy — an ACCOUNT ID, because two accounts
    // can share a display name — so a reader is handed a guid and nothing to render. The only
    // endpoint that resolved an id to a name was GET /api/admin/users/{id}, which is gated on
    // "Administrators" and returns the whole account: email, phone, roles, lockout state. A
    // public page could not use it, and should not have been able to.
    //
    // ANONYMOUS, AND DELIBERATELY THIN. Two members leave here — the friendly name and the
    // avatar url — and nothing else. That is what a byline needs, and every wider field on the
    // account is a field this endpoint would be handing to the whole internet. Both members are
    // already public by construction: the avatar is served anonymously by /Profile-Image/{id},
    // and a display name is what the site puts under a contribution.
    //
    // It answers for ANY account, including one that has been disabled. A disabled account is
    // hidden from normal use, but its published contributions stay on the site, and dropping the
    // name would leave that content attributed to nobody rather than removing it — a takedown is
    // the surface for that, not a byline lookup. Worth a product ruling; the least destructive
    // reading is the one taken here.
    public static class ContributorApiEndpoints
    {
        public static IEndpointRouteBuilder MapContributorApiEndpoints(
            this IEndpointRouteBuilder endpoints)
        {
            // No RequireAuthorization: a signed-out reader reads the journal, and the byline is
            // part of what they read.
            RouteGroupBuilder contributorsGroup = endpoints.MapGroup("/api/contributors");

            contributorsGroup.MapGet("/{userId:guid}", async (
                Guid userId,
                HttpContext httpContext,
                IProfileViewService profileViewService) =>
            {
                ProfileView profile = await profileViewService.RetrieveProfileByIdAsync(userId);

                // RetrieveProfileByIdAsync answers with an EMPTY ProfileView for an id that
                // matches no account rather than throwing, so the default Id is what "no such
                // contributor" looks like. A 404 lets the client fall back to no byline instead
                // of rendering a nameless one.
                if (profile.Id == Guid.Empty)
                {
                    return Results.NotFound();
                }

                // A display name and an avatar change rarely and matter little when a minute
                // stale, so a short shared cache keeps a page of contributions from asking for
                // the same person once per item. The avatar URL carries its own content hash, so
                // the image behind it is still busted immediately when it changes.
                httpContext.Response.Headers.CacheControl = "public, max-age=60";

                return Results.Ok(new
                {
                    UserId = profile.Id.ToString(),
                    DisplayName = profile.DisplayName,
                    ImageUrl = profile.ImageUrl,
                });
            });

            return endpoints;
        }
    }
}
