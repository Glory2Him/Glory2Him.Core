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

using System.Buffers.Text;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Glory2Him.WebApp.Infrastructure
{
    // Cookie-authenticated JSON endpoints consumed by the React SPA
    // (Glory2Him.WebApp.React). These are the JSON equivalents of the Blazor
    // passkey pages (Manage/Passkeys, Manage/RenamePasskey, the PasskeySubmit
    // WebAuthn component) plus the external-login data the Blazor
    // ExternalLoginPicker and Manage/ExternalLogins pages surfaced. The
    // WebAuthn ceremony itself (navigator.credentials.create/get) runs in the
    // browser; these endpoints supply the options JSON and consume the
    // resulting credential JSON exactly as the Blazor pages did through
    // SignInManager/UserManager.
    public static class PasskeyApiEndpoints
    {
        private const int MaxPasskeyCount = 100;

        public sealed record RegisterPasskeyRequest(string CredentialJson);

        public sealed record PasskeyLoginRequest(string CredentialJson);

        public sealed record RenamePasskeyRequest(string Name);

        public sealed record RemoveExternalLoginRequest(string LoginProvider, string ProviderKey);

        public static IEndpointRouteBuilder MapPasskeyApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            RouteGroupBuilder passkeysGroup = endpoints.MapGroup("/api/passkeys");

            // Mirrors /Account/PasskeyCreationOptions: options for registering a
            // new passkey for the signed-in user.
            passkeysGroup.MapPost("/creation-options", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                string userId = await userManager.GetUserIdAsync(user);
                string userName = await userManager.GetUserNameAsync(user) ?? "User";

                string optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new()
                {
                    Id = userId,
                    Name = userName,
                    DisplayName = userName
                });

                return Results.Content(optionsJson, contentType: "application/json");
            })
            .RequireAuthorization();

            // Mirrors /Account/PasskeyRequestOptions: options for signing in with
            // a passkey; anonymous, optionally scoped to a username.
            passkeysGroup.MapPost("/request-options", async (
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager,
                string? username) =>
            {
                AppUser? user = string.IsNullOrEmpty(username)
                    ? null
                    : await userManager.FindByNameAsync(username);

                string optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);

                return Results.Content(optionsJson, contentType: "application/json");
            });

            // Mirrors the Blazor Manage/Passkeys AddPasskey flow: attest the
            // credential the browser created and attach it to the account.
            // Returns the Base64Url credential id so the SPA can immediately
            // prompt for a name, as the Blazor page did via RenamePasskey.
            passkeysGroup.MapPost("/register", async (
                RegisterPasskeyRequest registerPasskeyRequest,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                if (string.IsNullOrEmpty(registerPasskeyRequest.CredentialJson))
                {
                    return BadRequestMessage("The browser did not provide a passkey.");
                }

                IList<UserPasskeyInfo> currentPasskeys = await userManager.GetPasskeysAsync(user);

                if (currentPasskeys.Count >= MaxPasskeyCount)
                {
                    return BadRequestMessage(
                        "You have reached the maximum number of allowed passkeys.");
                }

                var attestationResult = await signInManager.PerformPasskeyAttestationAsync(
                    registerPasskeyRequest.CredentialJson);

                if (!attestationResult.Succeeded)
                {
                    return BadRequestMessage(
                        $"Could not add the passkey: {attestationResult.Failure.Message}");
                }

                IdentityResult addPasskeyResult = await userManager.AddOrUpdatePasskeyAsync(
                    user, attestationResult.Passkey);

                if (!addPasskeyResult.Succeeded)
                {
                    return BadRequestMessage("The passkey could not be added to your account.");
                }

                string credentialId = Base64Url.EncodeToString(
                    attestationResult.Passkey.CredentialId);

                return Results.Ok(new { CredentialId = credentialId });
            })
            .RequireAuthorization();

            // Mirrors the Blazor Login page's passkey branch: sign in with the
            // assertion the browser produced.
            passkeysGroup.MapPost("/login", async (
                PasskeyLoginRequest passkeyLoginRequest,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager) =>
            {
                if (string.IsNullOrEmpty(passkeyLoginRequest.CredentialJson))
                {
                    return Results.Unauthorized();
                }

                // Resolve the account from the assertion's credential id so a
                // disabled user is rejected before a cookie is issued, and the
                // SPA gets the same user payload the password login returns.
                AppUser? user;

                try
                {
                    using var credentialDocument =
                        System.Text.Json.JsonDocument.Parse(passkeyLoginRequest.CredentialJson);

                    string? credentialIdBase64Url = credentialDocument.RootElement
                        .GetProperty("id").GetString();

                    byte[] credentialIdBytes = Base64Url.DecodeFromChars(credentialIdBase64Url);
                    user = await userManager.FindByPasskeyIdAsync(credentialIdBytes);
                }
                catch (Exception exception)
                    when (exception is System.Text.Json.JsonException
                        or KeyNotFoundException
                        or InvalidOperationException
                        or FormatException
                        or ArgumentNullException)
                {
                    return Results.Unauthorized();
                }

                if (user is null || user.IsDisabled)
                {
                    return Results.Unauthorized();
                }

                SignInResult signInResult = await signInManager.PasskeySignInAsync(
                    passkeyLoginRequest.CredentialJson);

                if (!signInResult.Succeeded)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(await ToCurrentUserAsync(user, userManager));
            });

            // Mirrors the Blazor Manage/Passkeys list: name and creation date per
            // passkey, keyed by the Base64Url credential id.
            passkeysGroup.MapGet("/", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                IList<UserPasskeyInfo> currentPasskeys = await userManager.GetPasskeysAsync(user);

                var passkeys = currentPasskeys.Select(passkey => new
                {
                    CredentialId = Base64Url.EncodeToString(passkey.CredentialId),
                    passkey.Name,
                    passkey.CreatedAt
                });

                return Results.Ok(passkeys);
            })
            .RequireAuthorization();

            // Mirrors the Blazor Manage/RenamePasskey page.
            passkeysGroup.MapPut("/{credentialId}", async (
                string credentialId,
                RenamePasskeyRequest renamePasskeyRequest,
                HttpContext httpContext,
                UserManager<AppUser> userManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                if (string.IsNullOrWhiteSpace(renamePasskeyRequest.Name))
                {
                    return BadRequestMessage("The Name field is required.");
                }

                if (renamePasskeyRequest.Name.Length > 200)
                {
                    return BadRequestMessage(
                        "Passkey names must be no longer than 200 characters.");
                }

                byte[] credentialIdBytes;

                try
                {
                    credentialIdBytes = Base64Url.DecodeFromChars(credentialId);
                }
                catch (FormatException)
                {
                    return BadRequestMessage("The specified passkey ID had an invalid format.");
                }

                UserPasskeyInfo? passkey = await userManager.GetPasskeyAsync(user, credentialIdBytes);

                if (passkey is null)
                {
                    return Results.NotFound(new
                    {
                        Message = "The specified passkey could not be found.",
                        Errors = new[] { "The specified passkey could not be found." },
                    });
                }

                passkey.Name = renamePasskeyRequest.Name;
                IdentityResult renameResult = await userManager.AddOrUpdatePasskeyAsync(user, passkey);

                if (!renameResult.Succeeded)
                {
                    return BadRequestMessage("The passkey could not be updated.");
                }

                return Results.Ok();
            })
            .RequireAuthorization();

            // Mirrors the Blazor Manage/Passkeys delete action.
            passkeysGroup.MapDelete("/{credentialId}", async (
                string credentialId,
                HttpContext httpContext,
                UserManager<AppUser> userManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                byte[] credentialIdBytes;

                try
                {
                    credentialIdBytes = Base64Url.DecodeFromChars(credentialId);
                }
                catch (FormatException)
                {
                    return BadRequestMessage("The specified passkey ID had an invalid format.");
                }

                IdentityResult removeResult = await userManager.RemovePasskeyAsync(
                    user, credentialIdBytes);

                if (!removeResult.Succeeded)
                {
                    return BadRequestMessage("The passkey could not be deleted.");
                }

                return Results.Ok();
            })
            .RequireAuthorization();

            RouteGroupBuilder accountsGroup = endpoints.MapGroup("/api/accounts");

            // Mirrors the Blazor ExternalLoginPicker: the configured external
            // authentication schemes. The demo configures none, so this returns
            // an empty list and the SPA renders the same "no external
            // authentication services configured" copy Blazor showed.
            accountsGroup.MapGet("/external-providers", async (
                SignInManager<AppUser> signInManager) =>
            {
                IEnumerable<AuthenticationScheme> schemes =
                    await signInManager.GetExternalAuthenticationSchemesAsync();

                var providers = schemes.Select(scheme => new
                {
                    scheme.Name,
                    scheme.DisplayName
                });

                return Results.Ok(providers);
            });

            // Mirrors the Blazor Manage/ExternalLogins page data: the user's
            // linked external logins, the remaining linkable providers and
            // whether removal is allowed (a password exists or more than one
            // login remains).
            accountsGroup.MapGet("/external-logins", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                IList<UserLoginInfo> currentLogins = await userManager.GetLoginsAsync(user);

                var otherLogins = (await signInManager.GetExternalAuthenticationSchemesAsync())
                    .Where(scheme => currentLogins.All(login => scheme.Name != login.LoginProvider))
                    .Select(scheme => new { scheme.Name, scheme.DisplayName })
                    .ToList();

                bool hasPassword = await userManager.HasPasswordAsync(user);
                bool showRemoveButton = hasPassword || currentLogins.Count > 1;

                return Results.Ok(new
                {
                    CurrentLogins = currentLogins.Select(login => new
                    {
                        login.LoginProvider,
                        login.ProviderDisplayName,
                        login.ProviderKey
                    }),

                    OtherLogins = otherLogins,
                    ShowRemoveButton = showRemoveButton
                });
            })
            .RequireAuthorization();

            // Mirrors the Blazor Manage/ExternalLogins remove action.
            accountsGroup.MapPost("/external-logins/remove", async (
                RemoveExternalLoginRequest removeExternalLoginRequest,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                IdentityResult removeResult = await userManager.RemoveLoginAsync(
                    user,
                    removeExternalLoginRequest.LoginProvider,
                    removeExternalLoginRequest.ProviderKey);

                if (!removeResult.Succeeded)
                {
                    return BadRequestMessage("The external login was not removed.");
                }

                await signInManager.RefreshSignInAsync(user);

                return Results.Ok();
            })
            .RequireAuthorization();

            return endpoints;
        }

        private static IResult BadRequestMessage(string message) =>
            Results.BadRequest(new
            {
                Message = message,
                Errors = new[] { message },
            });

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
