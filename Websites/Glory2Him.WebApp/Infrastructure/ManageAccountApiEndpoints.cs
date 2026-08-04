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

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using QRCoder;

namespace Glory2Him.WebApp.Infrastructure
{
    // Cookie-authenticated JSON endpoints consumed by the React SPA
    // (Glory2Him.WebApp.React). Mirrors the Blazor Account/Manage pages for email
    // management, two-factor authentication and personal data, plus the anonymous
    // email-confirmation flows (ConfirmEmail, ConfirmEmailChange,
    // ResendEmailConfirmation and RegisterConfirmation).
    public static class ManageAccountApiEndpoints
    {
        private const string AuthenticatorUriFormat =
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public sealed record ChangeEmailRequest(string NewEmail);

        public sealed record VerifyAuthenticatorRequest(string Code);

        public sealed record DeletePersonalDataRequest(string? Password);

        public sealed record ResendEmailConfirmationRequest(string Email);

        public sealed record ConfirmEmailRequest(string UserId, string Code);

        public sealed record ConfirmEmailChangeRequest(string UserId, string Email, string Code);

        public static IEndpointRouteBuilder MapManageAccountApiEndpoints(
            this IEndpointRouteBuilder endpoints)
        {
            MapAnonymousEmailConfirmationEndpoints(endpoints);

            RouteGroupBuilder manageGroup = endpoints.MapGroup("/api/manage")
                .RequireAuthorization();

            MapEmailEndpoints(manageGroup);
            MapTwoFactorEndpoints(manageGroup);
            MapPersonalDataEndpoints(manageGroup);

            return endpoints;
        }

        // Mirrors the Blazor Manage/Email page: the current address plus its
        // confirmation state, a change-email confirmation link and a
        // send-verification-email action (IEmailSender is a no-op in this demo).
        private static void MapEmailEndpoints(RouteGroupBuilder manageGroup)
        {
            manageGroup.MapGet("/email", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(new
                {
                    Email = await userManager.GetEmailAsync(user),
                    IsEmailConfirmed = await userManager.IsEmailConfirmedAsync(user),
                });
            });

            manageGroup.MapPost("/email/change", async (
                ChangeEmailRequest changeEmailRequest,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                IEmailSender<AppUser> emailSender) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                string userId = await userManager.GetUserIdAsync(user);

                string token = await userManager.GenerateChangeEmailTokenAsync(
                    user, changeEmailRequest.NewEmail);

                string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                string callbackUrl = QueryHelpers.AddQueryString(
                    $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Account/ConfirmEmailChange",
                    new Dictionary<string, string?>
                    {
                        ["userId"] = userId,
                        ["email"] = changeEmailRequest.NewEmail,
                        ["code"] = code,
                    });

                await emailSender.SendConfirmationLinkAsync(
                    user,
                    changeEmailRequest.NewEmail,
                    HtmlEncoder.Default.Encode(callbackUrl));

                return Results.Ok(new
                {
                    Message = "Confirmation link to change email sent. Please check your email.",
                });
            });

            manageGroup.MapPost("/email/send-verification", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                IEmailSender<AppUser> emailSender) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                string? email = await userManager.GetEmailAsync(user);

                if (email is null)
                {
                    return Results.Ok();
                }

                string userId = await userManager.GetUserIdAsync(user);
                string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                string callbackUrl = QueryHelpers.AddQueryString(
                    $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Account/ConfirmEmail",
                    new Dictionary<string, string?>
                    {
                        ["userId"] = userId,
                        ["code"] = code,
                    });

                await emailSender.SendConfirmationLinkAsync(
                    user, email, HtmlEncoder.Default.Encode(callbackUrl));

                return Results.Ok(new
                {
                    Message = "Verification email sent. Please check your email.",
                });
            });
        }

        // Mirrors the Blazor Manage/TwoFactorAuthentication, EnableAuthenticator,
        // Disable2fa, GenerateRecoveryCodes and ResetAuthenticator pages.
        private static void MapTwoFactorEndpoints(RouteGroupBuilder manageGroup)
        {
            manageGroup.MapGet("/two-factor", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                bool canTrack =
                    httpContext.Features.Get<ITrackingConsentFeature>()?.CanTrack ?? true;

                return Results.Ok(new
                {
                    CanTrack = canTrack,
                    HasAuthenticator = await userManager.GetAuthenticatorKeyAsync(user) is not null,
                    Is2faEnabled = await userManager.GetTwoFactorEnabledAsync(user),
                    IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user),
                    RecoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user),
                });
            });

            // The shared key is formatted exactly like the Blazor page (groups of
            // four, lowercase) and the URI is the same otpauth URI the QR encodes.
            manageGroup.MapGet("/two-factor/authenticator", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                UrlEncoder urlEncoder) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                string unformattedKey = await LoadAuthenticatorKeyAsync(userManager, user);
                string? email = await userManager.GetEmailAsync(user);

                return Results.Ok(new
                {
                    SharedKey = FormatKey(unformattedKey),
                    AuthenticatorUri = GenerateQrCodeUri(urlEncoder, email!, unformattedKey),
                });
            });

            // The same SVG QR code the Blazor EnableAuthenticator page inlined
            // (QRCoder SvgQRCode, 4 pixels per module, black on white).
            manageGroup.MapGet("/two-factor/qr-code", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                UrlEncoder urlEncoder) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                string unformattedKey = await LoadAuthenticatorKeyAsync(userManager, user);
                string? email = await userManager.GetEmailAsync(user);
                string authenticatorUri = GenerateQrCodeUri(urlEncoder, email!, unformattedKey);

                using var qrGenerator = new QRCodeGenerator();

                using QRCodeData qrCodeData =
                    qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);

                var svgQrCode = new SvgQRCode(qrCodeData);

                string qrCodeSvg = svgQrCode.GetGraphic(
                    pixelsPerModule: 4,
                    darkColorHex: "#000000",
                    lightColorHex: "#ffffff",
                    drawQuietZones: true);

                return Results.Text(qrCodeSvg, contentType: "image/svg+xml");
            });

            // Enables 2FA when the code checks out; returns fresh recovery codes
            // only when the user has none left — exactly like the Blazor page.
            manageGroup.MapPost("/two-factor/verify", async (
                VerifyAuthenticatorRequest verifyRequest,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                ILoggerFactory loggerFactory) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                string verificationCode = verifyRequest.Code
                    .Replace(" ", string.Empty)
                    .Replace("-", string.Empty);

                bool is2faTokenValid = await userManager.VerifyTwoFactorTokenAsync(
                    user,
                    userManager.Options.Tokens.AuthenticatorTokenProvider,
                    verificationCode);

                if (!is2faTokenValid)
                {
                    return Results.BadRequest(new
                    {
                        Message = "Error: Verification code is invalid.",
                    });
                }

                await userManager.SetTwoFactorEnabledAsync(user, true);
                string userId = await userManager.GetUserIdAsync(user);

                loggerFactory.CreateLogger("ManageAccountApi").LogInformation(
                    "User with ID '{UserId}' has enabled 2FA with an authenticator app.", userId);

                IEnumerable<string>? recoveryCodes = null;

                if (await userManager.CountRecoveryCodesAsync(user) == 0)
                {
                    recoveryCodes =
                        await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                }

                return Results.Ok(new
                {
                    Message = "Your authenticator app has been verified.",
                    RecoveryCodes = recoveryCodes,
                });
            });

            manageGroup.MapPost("/two-factor/disable", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                ILoggerFactory loggerFactory) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                IdentityResult disable2faResult =
                    await userManager.SetTwoFactorEnabledAsync(user, false);

                if (!disable2faResult.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        Message = "Unexpected error occurred disabling 2FA.",
                    });
                }

                string userId = await userManager.GetUserIdAsync(user);

                loggerFactory.CreateLogger("ManageAccountApi").LogInformation(
                    "User with ID '{UserId}' has disabled 2fa.", userId);

                return Results.Ok();
            });

            manageGroup.MapPost("/two-factor/generate-recovery-codes", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                ILoggerFactory loggerFactory) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                if (!await userManager.GetTwoFactorEnabledAsync(user))
                {
                    return Results.BadRequest(new
                    {
                        Message =
                            "Cannot generate recovery codes for user because they do not have 2FA enabled.",
                    });
                }

                string userId = await userManager.GetUserIdAsync(user);

                IEnumerable<string> recoveryCodes =
                    (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))!;

                loggerFactory.CreateLogger("ManageAccountApi").LogInformation(
                    "User with ID '{UserId}' has generated new 2FA recovery codes.", userId);

                return Results.Ok(new
                {
                    Message = "You have generated new recovery codes.",
                    RecoveryCodes = recoveryCodes,
                });
            });

            manageGroup.MapPost("/two-factor/reset-authenticator", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager,
                ILoggerFactory loggerFactory) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                await userManager.SetTwoFactorEnabledAsync(user, false);
                await userManager.ResetAuthenticatorKeyAsync(user);
                string userId = await userManager.GetUserIdAsync(user);

                loggerFactory.CreateLogger("ManageAccountApi").LogInformation(
                    "User with ID '{UserId}' has reset their authentication app key.", userId);

                await signInManager.RefreshSignInAsync(user);

                return Results.Ok();
            });

            manageGroup.MapPost("/two-factor/forget-browser", async (
                SignInManager<AppUser> signInManager) =>
            {
                await signInManager.ForgetTwoFactorClientAsync();

                return Results.Ok();
            });
        }

        // Mirrors the Blazor Manage/PersonalData and DeletePersonalData pages plus
        // the DownloadPersonalData minimal endpoint.
        private static void MapPersonalDataEndpoints(RouteGroupBuilder manageGroup)
        {
            manageGroup.MapGet("/personal-data", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(new
                {
                    RequirePassword = await userManager.HasPasswordAsync(user),
                });
            });

            manageGroup.MapGet("/personal-data/download", async (
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                ILoggerFactory loggerFactory) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                string userId = await userManager.GetUserIdAsync(user);

                loggerFactory.CreateLogger("ManageAccountApi").LogInformation(
                    "User with ID '{UserId}' asked for their personal data.", userId);

                // Only include personal data for download.
                var personalData = new Dictionary<string, string>();

                IEnumerable<System.Reflection.PropertyInfo> personalDataProps =
                    typeof(AppUser).GetProperties().Where(property =>
                        Attribute.IsDefined(property, typeof(PersonalDataAttribute)));

                foreach (System.Reflection.PropertyInfo property in personalDataProps)
                {
                    personalData.Add(
                        property.Name, property.GetValue(user)?.ToString() ?? "null");
                }

                IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user);

                foreach (UserLoginInfo login in logins)
                {
                    personalData.Add(
                        $"{login.LoginProvider} external login provider key",
                        login.ProviderKey);
                }

                personalData.Add(
                    "Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);

                byte[] fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

                httpContext.Response.Headers.TryAdd(
                    "Content-Disposition", "attachment; filename=PersonalData.json");

                return Results.File(
                    fileBytes,
                    contentType: "application/json",
                    fileDownloadName: "PersonalData.json");
            });

            manageGroup.MapPost("/delete-personal-data", async (
                DeletePersonalDataRequest deleteRequest,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager,
                ILoggerFactory loggerFactory) =>
            {
                AppUser? user = await userManager.GetUserAsync(httpContext.User);

                if (user is null)
                {
                    return Results.Unauthorized();
                }

                bool requirePassword = await userManager.HasPasswordAsync(user);

                if (requirePassword &&
                    !await userManager.CheckPasswordAsync(user, deleteRequest.Password ?? string.Empty))
                {
                    return Results.BadRequest(new
                    {
                        Message = "Error: Incorrect password.",
                    });
                }

                string userId = await userManager.GetUserIdAsync(user);
                IdentityResult deleteResult = await userManager.DeleteAsync(user);

                if (!deleteResult.Succeeded)
                {
                    return Results.BadRequest(new
                    {
                        Message = "Unexpected error occurred deleting user.",
                    });
                }

                await signInManager.SignOutAsync();

                loggerFactory.CreateLogger("ManageAccountApi").LogInformation(
                    "User with ID '{UserId}' deleted themselves.", userId);

                return Results.Ok();
            });
        }

        // Mirrors the anonymous Blazor pages ResendEmailConfirmation, ConfirmEmail,
        // ConfirmEmailChange and RegisterConfirmation.
        private static void MapAnonymousEmailConfirmationEndpoints(
            IEndpointRouteBuilder endpoints)
        {
            RouteGroupBuilder accountsGroup = endpoints.MapGroup("/api/accounts");

            // Always 200 so account existence is never revealed.
            accountsGroup.MapPost("/resend-email-confirmation", async (
                ResendEmailConfirmationRequest resendRequest,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                IEmailSender<AppUser> emailSender) =>
            {
                AppUser? user = await userManager.FindByEmailAsync(resendRequest.Email);

                if (user is null)
                {
                    return Results.Ok();
                }

                string userId = await userManager.GetUserIdAsync(user);
                string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                string callbackUrl = QueryHelpers.AddQueryString(
                    $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Account/ConfirmEmail",
                    new Dictionary<string, string?>
                    {
                        ["userId"] = userId,
                        ["code"] = code,
                    });

                await emailSender.SendConfirmationLinkAsync(
                    user, resendRequest.Email, HtmlEncoder.Default.Encode(callbackUrl));

                return Results.Ok();
            });

            accountsGroup.MapPost("/confirm-email", async (
                ConfirmEmailRequest confirmRequest,
                UserManager<AppUser> userManager) =>
            {
                AppUser? user = await userManager.FindByIdAsync(confirmRequest.UserId);

                if (user is null)
                {
                    return Results.NotFound(new
                    {
                        Message = $"Error loading user with ID {confirmRequest.UserId}",
                    });
                }

                string code;

                try
                {
                    code = Encoding.UTF8.GetString(
                        WebEncoders.Base64UrlDecode(confirmRequest.Code));
                }
                catch (FormatException)
                {
                    return Results.Ok(new { Message = "Error confirming your email." });
                }

                IdentityResult result = await userManager.ConfirmEmailAsync(user, code);

                return Results.Ok(new
                {
                    Message = result.Succeeded
                        ? "Thank you for confirming your email."
                        : "Error confirming your email.",
                });
            });

            accountsGroup.MapPost("/confirm-email-change", async (
                ConfirmEmailChangeRequest confirmRequest,
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager) =>
            {
                AppUser? user = await userManager.FindByIdAsync(confirmRequest.UserId);

                if (user is null)
                {
                    return Results.Ok(new
                    {
                        Message = "Unable to find user with Id '{userId}'",
                    });
                }

                string code;

                try
                {
                    code = Encoding.UTF8.GetString(
                        WebEncoders.Base64UrlDecode(confirmRequest.Code));
                }
                catch (FormatException)
                {
                    return Results.Ok(new { Message = "Error changing email." });
                }

                IdentityResult result =
                    await userManager.ChangeEmailAsync(user, confirmRequest.Email, code);

                if (!result.Succeeded)
                {
                    return Results.Ok(new { Message = "Error changing email." });
                }

                // In our UI email and user name are one and the same, so when we
                // update the email we need to update the user name.
                IdentityResult setUserNameResult =
                    await userManager.SetUserNameAsync(user, confirmRequest.Email);

                if (!setUserNameResult.Succeeded)
                {
                    return Results.Ok(new { Message = "Error changing user name." });
                }

                await signInManager.RefreshSignInAsync(user);

                return Results.Ok(new
                {
                    Message = "Thank you for confirming your email change.",
                });
            });

            // The demo has no real email sender registered, so — exactly like the
            // Blazor RegisterConfirmation page — the confirmation link is surfaced
            // directly for the just-registered account.
            accountsGroup.MapGet("/register-confirmation", async (
                string email,
                string? returnUrl,
                HttpContext httpContext,
                UserManager<AppUser> userManager,
                IEmailSender<AppUser> emailSender) =>
            {
                AppUser? user = await userManager.FindByEmailAsync(email);

                if (user is null)
                {
                    return Results.NotFound(new
                    {
                        Message = "Error finding user for unspecified email",
                    });
                }

                string? emailConfirmationLink = null;

                if (emailSender is IdentityNoOpEmailSender)
                {
                    // Once you add a real email sender, you should remove this code
                    // that lets you confirm the account.
                    string userId = await userManager.GetUserIdAsync(user);
                    string token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                    emailConfirmationLink = QueryHelpers.AddQueryString(
                        $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/Account/ConfirmEmail",
                        new Dictionary<string, string?>
                        {
                            ["userId"] = userId,
                            ["code"] = code,
                            ["returnUrl"] = returnUrl,
                        });
                }

                return Results.Ok(new { EmailConfirmationLink = emailConfirmationLink });
            });
        }

        private static async Task<string> LoadAuthenticatorKeyAsync(
            UserManager<AppUser> userManager,
            AppUser user)
        {
            string? unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(unformattedKey))
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
            }

            return unformattedKey!;
        }

        private static string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;

            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }

            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private static string GenerateQrCodeUri(
            UrlEncoder urlEncoder,
            string email,
            string unformattedKey)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                AuthenticatorUriFormat,
                urlEncoder.Encode("Glory2Him"),
                urlEncoder.Encode(email),
                unformattedKey);
        }
    }
}
