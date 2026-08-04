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

using System.Text;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Glory2Him.WebApp.Services.Views.Users;
using Microsoft.AspNetCore.WebUtilities;

namespace Glory2Him.WebApp.Infrastructure
{
    // Cookie-authenticated JSON endpoints consumed by the React SPA
    // (Glory2Him.WebApp.React). Mirrors exactly what the Blazor Admin/Users pages
    // do through IUsersViewService — no more, no less. The whole group is
    // Administrators-only; the view service adds its own guards on top (e.g. the
    // last administrator cannot be removed, disabled, or deleted).
    public static class UserAdminApiEndpoints
    {
        public sealed record UpdateUserRequest(
            string UserName,
            string Email,
            string PhoneNumber,
            string Name,
            string Surname,
            string? PreferredName,
            DateOnly? DateOfBirth);

        public sealed record SetUserRoleRequest(string RoleName, bool IsInRole);

        public sealed record SetLockedOutRequest(bool IsLockedOut);

        public sealed record SetTwoFactorRequest(bool IsEnabled);

        public sealed record SetDisabledRequest(bool IsDisabled);

        public static IEndpointRouteBuilder MapUserAdminApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            RouteGroupBuilder usersGroup = endpoints.MapGroup("/api/admin/users")
                .RequireAuthorization(policy => policy.RequireRole("Administrators"));

            usersGroup.MapGet("/", async (IUsersViewService usersViewService) =>
            {
                List<UserView> users = await usersViewService.RetrieveAllUsersAsync();

                return Results.Ok(users);
            });

            usersGroup.MapGet("/roles", async (IUsersViewService usersViewService) =>
            {
                List<string> roleNames = await usersViewService.RetrieveAllRoleNamesAsync();

                return Results.Ok(roleNames);
            });

            usersGroup.MapGet("/{userId:guid}", async (
                Guid userId,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    UserView user = await usersViewService.RetrieveUserByIdAsync(userId);

                    return Results.Ok(user);
                }));

            usersGroup.MapPut("/{userId:guid}", async (
                Guid userId,
                UpdateUserRequest request,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    var user = new UserView
                    {
                        Id = userId,
                        UserName = request.UserName,
                        Email = request.Email,
                        PhoneNumber = request.PhoneNumber,
                        Name = request.Name,
                        Surname = request.Surname,
                        PreferredName = request.PreferredName,
                        DateOfBirth = request.DateOfBirth,
                    };

                    await usersViewService.ModifyUserAsync(user);

                    return Results.Ok();
                }));

            usersGroup.MapPost("/{userId:guid}/roles", async (
                Guid userId,
                SetUserRoleRequest request,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    await usersViewService.SetUserRoleAsync(userId, request.RoleName, request.IsInRole);

                    return Results.Ok();
                }));

            usersGroup.MapPost("/{userId:guid}/confirm-email", async (
                Guid userId,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    await usersViewService.ConfirmUserEmailAsync(userId);

                    return Results.Ok();
                }));

            usersGroup.MapPost("/{userId:guid}/locked-out", async (
                Guid userId,
                SetLockedOutRequest request,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    await usersViewService.SetUserLockedOutAsync(userId, request.IsLockedOut);

                    return Results.Ok();
                }));

            usersGroup.MapPost("/{userId:guid}/reset-failed-count", async (
                Guid userId,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    await usersViewService.ResetAccessFailedCountAsync(userId);

                    return Results.Ok();
                }));

            usersGroup.MapPost("/{userId:guid}/two-factor", async (
                Guid userId,
                SetTwoFactorRequest request,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    await usersViewService.SetTwoFactorEnabledAsync(userId, request.IsEnabled);

                    return Results.Ok();
                }));

            usersGroup.MapPost("/{userId:guid}/disabled", async (
                Guid userId,
                SetDisabledRequest request,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    await usersViewService.SetUserDisabledAsync(userId, request.IsDisabled);

                    return Results.Ok();
                }));

            // Same link the Blazor admin page builds: the raw Identity token is
            // Base64Url-encoded into ?code=, targeting the Account/ConfirmEmail page.
            usersGroup.MapPost("/{userId:guid}/confirmation-link", async (
                Guid userId,
                HttpContext httpContext,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    string token = await usersViewService.GenerateEmailConfirmationTokenAsync(userId);

                    string link = BuildAccountLink(
                        httpContext,
                        path: "Account/ConfirmEmail",
                        token,
                        userId: userId);

                    return Results.Ok(new { Link = link });
                }));

            usersGroup.MapPost("/{userId:guid}/password-reset-link", async (
                Guid userId,
                HttpContext httpContext,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    string token = await usersViewService.GeneratePasswordResetTokenAsync(userId);

                    string link = BuildAccountLink(
                        httpContext,
                        path: "Account/ResetPassword",
                        token,
                        userId: null);

                    return Results.Ok(new { Link = link });
                }));

            usersGroup.MapDelete("/{userId:guid}", async (
                Guid userId,
                IUsersViewService usersViewService) =>
                await RunAsync(async () =>
                {
                    await usersViewService.DeleteUserAsync(userId);

                    return Results.Ok();
                }));

            return endpoints;
        }

        // The view service reports business-rule breaches (unknown user, last admin
        // protection) as validation exceptions; the SPA shows the message verbatim.
        private static async Task<IResult> RunAsync(Func<Task<IResult>> action)
        {
            try
            {
                return await action();
            }
            catch (UsersViewValidationException usersViewValidationException)
            {
                return Results.BadRequest(new { Message = usersViewValidationException.Message });
            }
        }

        private static string BuildAccountLink(
            HttpContext httpContext,
            string path,
            string token,
            Guid? userId)
        {
            string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var queryParameters = new Dictionary<string, string?> { ["code"] = code };

            if (userId is not null)
            {
                queryParameters["userId"] = userId.Value.ToString();
            }

            string baseUrl =
                $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{path}";

            return QueryHelpers.AddQueryString(baseUrl, queryParameters);
        }
    }
}
