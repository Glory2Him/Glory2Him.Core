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
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Glory2Him.WebApp.Infrastructure
{
    // Cookie-authenticated JSON endpoints consumed by the React SPA
    // (Glory2Him.WebApp.React). The SPA reads the current user's identity and
    // roles from /api/accounts/me; the security components (SecuredRoute,
    // SecuredComponent, SecuredLink) gate routes and UI on those roles, while
    // the server remains the authority on every [Authorize] endpoint.
    public static class AccountApiEndpoints
    {
        public sealed record LoginRequest(string UserName, string Password, bool RememberMe);

        public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);

        public sealed record ForgotPasswordRequest(string Email);

        public sealed record ResetPasswordRequest(string Email, string Code, string Password);

        public static IEndpointRouteBuilder MapAccountApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            RouteGroupBuilder accountsGroup = endpoints.MapGroup("/api/accounts");

            // Always returns 200 so an anonymous visitor is a normal state,
            // not an error, on the SPA side.
            accountsGroup.MapGet("/me", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager) =>
            {
                if (httpContext.User.Identity?.IsAuthenticated != true)
                {
                    return Results.Ok(AnonymousUser());
                }

                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null || user.IsDisabled)
                {
                    return Results.Ok(AnonymousUser());
                }

                return Results.Ok(await ToCurrentUserAsync(user, userManager));
            });

            accountsGroup.MapPost("/login", async (
                LoginRequest loginRequest,
                SignInManager<AppUser> signInManager,
                UserManager<AppUser> userManager) =>
            {
                AppUser? user = await userManager.FindByNameAsync(loginRequest.UserName)
                    ?? await userManager.FindByEmailAsync(loginRequest.UserName);

                if (user is null || user.IsDisabled)
                {
                    return Results.Unauthorized();
                }

                SignInResult signInResult = await signInManager.PasswordSignInAsync(
                    user,
                    loginRequest.Password,
                    isPersistent: loginRequest.RememberMe,
                    lockoutOnFailure: true);

                if (!signInResult.Succeeded)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await ToCurrentUserAsync(user, userManager));
            });

            accountsGroup.MapPost("/logout", async (SignInManager<AppUser> signInManager) =>
            {
                await signInManager.SignOutAsync();

                return Results.Ok();
            });

            // Mirrors the Blazor Account/Manage ChangePassword page: verify the old
            // password, set the new one and refresh the sign-in cookie.
            accountsGroup.MapPost("/change-password", async (
                ChangePasswordRequest changePasswordRequest,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                IdentityResult changePasswordResult = await userManager.ChangePasswordAsync(
                    user,
                    changePasswordRequest.OldPassword,
                    changePasswordRequest.NewPassword);

                if (!changePasswordResult.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        Message = string.Join(
                            ",",
                            changePasswordResult.Errors.Select(error => error.Description)),

                        Errors = changePasswordResult.Errors.Select(error => error.Description),
                    });
                }

                await signInManager.RefreshSignInAsync(user);

                return Results.Ok();
            })
            .RequireAuthorization();

            // Mirrors the Blazor ForgotPassword page: always 200 so an unknown or
            // unconfirmed email is never revealed; the reset link goes through
            // IEmailSender, which is a no-op in this demo.
            accountsGroup.MapPost("/forgot-password", async (
                ForgotPasswordRequest forgotPasswordRequest,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                IEmailSender<AppUser> emailSender) =>
            {
                AppUser? user = await userManager.FindByEmailAsync(forgotPasswordRequest.Email);

                if (user is null || !await userManager.IsEmailConfirmedAsync(user))
                {
                    return Results.Ok();
                }

                string token = await userManager.GeneratePasswordResetTokenAsync(user);
                string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                string callbackUrl = QueryHelpers.AddQueryString(
                    $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Account/ResetPassword",
                    "code",
                    code);

                await emailSender.SendPasswordResetLinkAsync(
                    user, forgotPasswordRequest.Email, callbackUrl);

                return Results.Ok();
            });

            // Mirrors the Blazor ResetPassword page: the code is the Base64Url-encoded
            // token from the reset link; an unknown email still returns 200 so
            // account existence is never revealed.
            accountsGroup.MapPost("/reset-password", async (
                ResetPasswordRequest resetPasswordRequest,
                UserManager<AppUser> userManager) =>
            {
                AppUser? user = await userManager.FindByEmailAsync(resetPasswordRequest.Email);

                if (user is null)
                {
                    return Results.Ok();
                }

                string token;

                try
                {
                    token = Encoding.UTF8.GetString(
                        WebEncoders.Base64UrlDecode(resetPasswordRequest.Code));
                }
                catch (FormatException)
                {
                    return Results.BadRequest(new
                    {
                        Message = "The password reset code is invalid.",
                        Errors = new[] { "The password reset code is invalid." },
                    });
                }

                IdentityResult resetResult = await userManager.ResetPasswordAsync(
                    user, token, resetPasswordRequest.Password);

                if (!resetResult.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        Message = string.Join(
                            " ",
                            resetResult.Errors.Select(error => error.Description)),

                        Errors = resetResult.Errors.Select(error => error.Description),
                    });
                }

                return Results.Ok();
            });

            return endpoints;
        }

        private static object AnonymousUser() =>
            new
            {
                IsAuthenticated = false,
                UserId = (string?)null,
                UserName = (string?)null,
                Email = (string?)null,
                DisplayName = (string?)null,
                Roles = Array.Empty<string>()
            };

        private static async Task<object> ToCurrentUserAsync(
            AppUser user,
            UserManager<AppUser> userManager)
        {
            IList<string> roles = await userManager.GetRolesAsync(user);

            return new
            {
                IsAuthenticated = true,
                UserId = user.Id.ToString(),
                UserName = user.UserName,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Roles = roles
            };
        }
    }
}
