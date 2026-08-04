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

using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Models.Views.Profiles;
using Glory2Him.WebApp.Models.Views.Profiles.Exceptions;
using Glory2Him.WebApp.Services.Views.Profiles;
using Microsoft.AspNetCore.Identity;

namespace Glory2Him.WebApp.Infrastructure
{
    // Cookie-authenticated JSON endpoints consumed by the React SPA
    // (Glory2Him.WebApp.React). Mirrors the Blazor Account/Manage profile page:
    // personal details go through UserManager exactly as Manage/Index does, and the
    // avatar goes through IProfileViewService exactly as ProfileImageManager does.
    // Everything here acts only on the signed-in user.
    public static class ProfileApiEndpoints
    {
        public sealed record UpdateProfileRequest(
            string Name,
            string Surname,
            string? PreferredName,
            DateOnly? DateOfBirth,
            string? PhoneNumber);

        public static IEndpointRouteBuilder MapProfileApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            RouteGroupBuilder profileGroup = endpoints.MapGroup("/api/profile")
                .RequireAuthorization();

            profileGroup.MapGet("/", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                IProfileViewService profileViewService) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                ProfileView profile = await profileViewService.RetrieveProfileByIdAsync(user.Id);

                return Results.Ok(new
                {
                    Id = user.Id.ToString(),
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Name = user.Name,
                    Surname = user.Surname,
                    PreferredName = user.PreferredName,
                    DateOfBirth = user.DateOfBirth,
                    HasProfileImage = profile.HasProfileImage,
                    ImageVersion = profile.ImageVersion,
                    ImageUrl = profile.ImageUrl,
                });
            });

            // Same field set (and same steps) as the Blazor Account/Manage Index page:
            // phone number via SetPhoneNumberAsync, the rest via UpdateAsync, then a
            // sign-in refresh so the cookie claims stay current.
            profileGroup.MapPut("/", async (
                UpdateProfileRequest request,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                if (request.PhoneNumber != user.PhoneNumber)
                {
                    IdentityResult setPhoneResult =
                        await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);

                    if (!setPhoneResult.Succeeded)
                    {
                        return Results.BadRequest(new
                        {
                            Message = "Failed to set phone number.",
                            Errors = setPhoneResult.Errors.Select(error => error.Description),
                        });
                    }
                }

                user.Name = request.Name ?? string.Empty;
                user.Surname = request.Surname ?? string.Empty;

                user.PreferredName = string.IsNullOrWhiteSpace(request.PreferredName)
                    ? null
                    : request.PreferredName;

                user.DateOfBirth = request.DateOfBirth;

                IdentityResult updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        Message = "Failed to update your profile.",
                        Errors = updateResult.Errors.Select(error => error.Description),
                    });
                }

                await signInManager.RefreshSignInAsync(user);

                return Results.Ok();
            });

            // Multipart upload; the view service validates size and content type,
            // resizes to a square WebP avatar and persists it — exactly what
            // ProfileImageManager does. Antiforgery is disabled because the SPA
            // authenticates with the identity cookie and carries no Razor form token.
            profileGroup.MapPost("/image", async (
                IFormFile file,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                IProfileViewService profileViewService) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                if (file.Length > ProfileViewService.MaxUploadBytes)
                {
                    return Results.BadRequest(new
                    {
                        Message = "The image is too large. Please choose a file up to 5 MB.",
                    });
                }

                try
                {
                    await using Stream stream = file.OpenReadStream();

                    await profileViewService.SetProfileImageAsync(
                        user.Id, stream, file.Length, file.ContentType);

                    return Results.Ok();
                }
                catch (ProfileViewValidationException profileViewValidationException)
                {
                    return Results.BadRequest(new
                    {
                        Message = profileViewValidationException.Message,
                    });
                }
            })
            .DisableAntiforgery();

            profileGroup.MapDelete("/image", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                IProfileViewService profileViewService) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                await profileViewService.RemoveProfileImageAsync(user.Id);

                return Results.Ok();
            });

            return endpoints;
        }
    }
}
