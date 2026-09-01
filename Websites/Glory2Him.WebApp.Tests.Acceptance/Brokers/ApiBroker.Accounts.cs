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

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        // Identity's own arrangement seam, alongside the role one. An account and a change-email
        // token are state no endpoint in this host can produce — registration mints a user but
        // never a token, and the token is signed by the same UserManager that will verify it, so
        // a hand-written one would only prove that a forgery is rejected.
        public async ValueTask<AppUser> AddUserAsync(
            string userName,
            string email,
            string password)
        {
            using IServiceScope scope = this.webApplicationFactory.Services.CreateScope();

            UserManager<AppUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                Name = string.Empty,
                Surname = string.Empty,
            };

            IdentityResult result = await userManager.CreateAsync(user, password);

            if (result.Succeeded is false)
            {
                throw new InvalidOperationException(
                    $"Could not arrange the test user: {string.Join(" ", result.Errors)}");
            }

            return user;
        }

        // Base64Url-encoded, because that is the shape the endpoint decodes — the link in a
        // confirmation email carries it that way, and a raw token would be rejected before the
        // flow under test was reached.
        public async ValueTask<string> GenerateEncodedChangeEmailTokenAsync(
            Guid userId,
            string newEmail)
        {
            using IServiceScope scope = this.webApplicationFactory.Services.CreateScope();

            UserManager<AppUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            AppUser user = await userManager.FindByIdAsync(userId.ToString());
            string token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);

            return WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
        }

        // Read back through UserManager rather than the response body: the endpoint answers with
        // a message, and the whole point of this arrangement is what the ROW says afterwards.
        public async ValueTask<AppUser> GetUserByIdAsync(Guid userId)
        {
            using IServiceScope scope = this.webApplicationFactory.Services.CreateScope();

            UserManager<AppUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            return await userManager.FindByIdAsync(userId.ToString());
        }

        public async ValueTask RemoveUserAsync(Guid userId)
        {
            using IServiceScope scope = this.webApplicationFactory.Services.CreateScope();

            UserManager<AppUser> userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            AppUser user = await userManager.FindByIdAsync(userId.ToString());

            if (user is not null)
            {
                await userManager.DeleteAsync(user);
            }
        }

        public async ValueTask<string> ConfirmEmailChangeAsync(
            Guid userId,
            string newEmail,
            string encodedCode)
        {
            var confirmRequest = new
            {
                UserId = userId.ToString(),
                Email = newEmail,
                Code = encodedCode,
            };

            // The raw client, not apiFactoryClient. RESTFulSense posts as "text/json", which the
            // controllers accept through their input formatters but a minimal API does not bind —
            // the request never reaches the endpoint and comes back off Program.cs's /api
            // fallback as a 404, which reads exactly like a missing route.
            var content = new StringContent(
                JsonSerializer.Serialize(confirmRequest),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response =
                await this.httpClient.PostAsync("api/accounts/confirm-email-change", content);

            response.EnsureSuccessStatusCode();

            ConfirmEmailChangeResponse confirmResponse =
                JsonSerializer.Deserialize<ConfirmEmailChangeResponse>(
                    await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return confirmResponse.Message;
        }

        private sealed class ConfirmEmailChangeResponse
        {
            public string Message { get; set; }
        }
    }
}
