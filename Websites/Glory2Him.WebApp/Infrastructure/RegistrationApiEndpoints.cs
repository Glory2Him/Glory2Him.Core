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
using Glory2Him.WebApp.Services.Views.Registrations;
using Microsoft.AspNetCore.Identity;

namespace Glory2Him.WebApp.Infrastructure
{
    // Cookie-authenticated JSON endpoints consumed by the React SPA
    // (Glory2Him.WebApp.React). Mirrors the Blazor Account/Register page: the
    // live availability checks and suggestions come from IRegistrationViewService,
    // and registration itself follows the exact same steps (re-check availability,
    // create with EmailConfirmed = true, add to the Users role). On success the
    // SPA sends the user to the login page — no auto sign-in, same as Blazor.
    public static class RegistrationApiEndpoints
    {
        public sealed record RegisterRequest(
            string UserName,
            string Email,
            string Name,
            string Surname,
            string? PreferredName,
            DateOnly? DateOfBirth,
            string Password);

        public static IEndpointRouteBuilder MapRegistrationApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            RouteGroupBuilder registrationsGroup = endpoints.MapGroup("/api/registrations");

            registrationsGroup.MapGet("/username-available", async (
                string userName,
                IRegistrationViewService registrationViewService) =>
            {
                string candidate = userName?.Trim() ?? string.Empty;

                if (candidate.Length < registrationViewService.MinimumUsernameLength)
                {
                    return Results.Ok(new
                    {
                        IsAvailable = false,
                        IsTooShort = true,
                        IsProhibited = false,
                        ProhibitedReason = (string?)null,
                        MinimumLength = registrationViewService.MinimumUsernameLength,
                    });
                }

                // Reported separately from "taken", because they are different problems and
                // "someone already has that" is a lie about an address nobody may use (§18.3.1).
                if (UserNameRule.IsAllowed(candidate) is false)
                {
                    return Results.Ok(new
                    {
                        IsAvailable = false,
                        IsTooShort = false,
                        IsProhibited = true,
                        ProhibitedReason = (string?)UserNameRule.RejectionMessage,
                        MinimumLength = registrationViewService.MinimumUsernameLength,
                    });
                }

                bool isAvailable =
                    await registrationViewService.IsUsernameAvailableAsync(candidate);

                return Results.Ok(new
                {
                    IsAvailable = isAvailable,
                    IsTooShort = false,
                    IsProhibited = false,
                    ProhibitedReason = (string?)null,
                    MinimumLength = registrationViewService.MinimumUsernameLength,
                });
            });

            registrationsGroup.MapGet("/email-in-use", async (
                string email,
                IRegistrationViewService registrationViewService) =>
            {
                string candidate = email?.Trim() ?? string.Empty;

                bool isInUse = candidate.Contains('@')
                    && await registrationViewService.IsEmailInUseAsync(candidate);

                return Results.Ok(new { IsInUse = isInUse });
            });

            registrationsGroup.MapGet("/username-suggestions", async (
                string? name,
                string? surname,
                string? preferredName,
                IRegistrationViewService registrationViewService) =>
            {
                List<string> suggestions = await registrationViewService.SuggestUsernamesAsync(
                    name ?? string.Empty,
                    surname ?? string.Empty,
                    preferredName);

                return Results.Ok(new { Suggestions = suggestions });
            });

            registrationsGroup.MapPost("/", async (
                RegisterRequest request,
                IRegistrationViewService registrationViewService,
                UserManager<AppUser> userManager,
                IUserStore<AppUser> userStore) =>
            {
                // The rule first, so a rejected name is told why rather than being reported as
                // somebody else's (§18.3.1). IsUsernameAvailableAsync refuses it too — this is
                // the message, not the guard.
                if (UserNameRule.IsAllowed(request.UserName) is false)
                {
                    return Results.BadRequest(new
                    {
                        Message = UserNameRule.RejectionMessage,
                        Errors = new[] { UserNameRule.RejectionMessage },
                    });
                }

                // Re-check on the server so a name taken between typing and submit
                // is still caught — same as the Blazor Register page.
                if (!await registrationViewService.IsUsernameAvailableAsync(request.UserName))
                {
                    return Results.BadRequest(new
                    {
                        Message = "That username is already taken. Please choose another.",
                        Errors = new[] { "That username is already taken. Please choose another." },
                    });
                }

                if (await registrationViewService.IsEmailInUseAsync(request.Email))
                {
                    return Results.BadRequest(new
                    {
                        Message = "An account with this email already exists. Please sign in instead.",

                        Errors = new[]
                        {
                            "An account with this email already exists. Please sign in instead.",
                        },
                    });
                }

                var user = new AppUser
                {
                    Name = request.Name,
                    Surname = request.Surname,

                    PreferredName = string.IsNullOrWhiteSpace(request.PreferredName)
                        ? null
                        : request.PreferredName,

                    DateOfBirth = request.DateOfBirth,
                    EmailConfirmed = true,
                };

                await userStore.SetUserNameAsync(user, request.UserName, CancellationToken.None);
                var emailStore = (IUserEmailStore<AppUser>)userStore;
                await emailStore.SetEmailAsync(user, request.Email, CancellationToken.None);

                IdentityResult result = await userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        Message = string.Join(" ", result.Errors.Select(error => error.Description)),
                        Errors = result.Errors.Select(error => error.Description),
                    });
                }

                await userManager.AddToRoleAsync(user, "Users");

                return Results.Ok(new
                {
                    UserId = user.Id.ToString(),
                    UserName = request.UserName,
                });
            });

            return endpoints;
        }
    }
}
