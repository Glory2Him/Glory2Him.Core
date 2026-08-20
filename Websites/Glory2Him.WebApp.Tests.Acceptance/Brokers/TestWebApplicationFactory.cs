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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Glory2Him.WebApp.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        static TestWebApplicationFactory()
        {
            // Configure configuration *before* the app's builder is used
            Program.TestConfigurationOverrides = builder =>
            {
                var testProjectPath =
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

                // This runs inside Program.cs right after CreateBuilder(...)
                // This lets us override any configuration values for testing
                builder.Configuration
                    .AddJsonFile(
                        Path.Combine(testProjectPath, "appsettings.json"),
                        optional: true)
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        // Put your strong overrides here

                        // Paging is a production posture, not something these assertions should
                        // have to walk: raised so a collection read returns everything the suite
                        // seeded. This replaces the #if DEBUG [EnableQuery(PageSize = 5000)] that
                        // used to sit on each exposer — the number now travels with the tests that
                        // need it instead of with the build configuration.
                        { ODataPageSizeConvention.ConfigurationKey, "5000" },
                    });
            };
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Make sure the app runs in a predictable test environment
            builder.UseEnvironment("Test");

            builder.ConfigureServices((context, services) =>
            {
                OverrideSecurityForTesting(services);
            });
        }

        private static void OverrideSecurityForTesting(IServiceCollection services)
        {
            // Remove existing authentication and authorization
            var authenticationDescriptor = services
                .FirstOrDefault(d => d.ServiceType == typeof(IAuthenticationSchemeProvider));

            if (authenticationDescriptor != null)
            {
                services.Remove(authenticationDescriptor);
            }

            // Override authentication and authorization
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
            })
            .AddScheme<CustomAuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options =>
            {
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("TestPolicy", policy => policy.RequireAssertion(_ => true));
            });
        }
    }
}
