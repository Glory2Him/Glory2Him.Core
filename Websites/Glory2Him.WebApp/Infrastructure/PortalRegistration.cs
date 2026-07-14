// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System;
using Glory2Him.WebApp.Brokers.DateTimes;
using Glory2Him.WebApp.Brokers.Identities;
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Components.Account;
using Glory2Him.WebApp.Data;
using Glory2Him.WebApp.Models.Foundations.Roles;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Services.Cart;
using Glory2Him.WebApp.Services.Views.Posts;
using Glory2Him.WebApp.Services.Views.Products;
using Glory2Him.WebApp.Services.Views.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.WebApp.Infrastructure
{
    public static class PortalRegistration
    {
        // LocalDB "instance startup" transient (0x89c5010a) is not in EF Core's default
        // transient-error set, so add it explicitly to the Identity DbContext retry policy.
        private static readonly int[] LocalDbTransientErrorNumbers = new[] { -1983577846 };

        public static IServiceCollection AddPortalBrokers(this IServiceCollection services)
        {
            services.AddTransient<IDateTimeBroker, DateTimeBroker>();
            services.AddTransient<ILoggingBroker, LoggingBroker>();

            return services;
        }

        public static IServiceCollection AddPortalIdentity(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            string securityConnectionString =
                configuration.GetConnectionString("Glory2HimSecurityConnection")
                    ?? throw new InvalidOperationException(
                        "Missing connection string 'Glory2HimSecurityConnection'.");

            services.AddCascadingAuthenticationState();
            services.AddScoped<IdentityRedirectManager>();

            services.AddScoped<
                AuthenticationStateProvider,
                IdentityRevalidatingAuthenticationStateProvider>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
                .AddIdentityCookies();

            services.AddDbContext<SecurityDbContext>(options =>
                options.UseSqlServer(
                    securityConnectionString,
                    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(3),
                        errorNumbersToAdd: LocalDbTransientErrorNumbers)));

            services.AddIdentityCore<AppUser>(options =>
            {
                // Schema Version3 adds the passkey store the Manage > Passkeys page requires
                // (without it the page throws because IdentityUserPasskey is not in the model).
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;

                // Default credentials are intentionally weak for first-run/demo purposes.
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
                options.Password.RequiredUniqueChars = 1;
            })
                .AddRoles<AppRole>()
                .AddEntityFrameworkStores<SecurityDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();

            services.AddSingleton<IEmailSender<AppUser>, IdentityNoOpEmailSender>();

            services.AddTransient<IIdentityBroker, IdentityBroker>();

            return services;
        }

        public static IServiceCollection AddPortalViewServices(this IServiceCollection services)
        {
            services.AddTransient<IPostsViewService, PostsViewService>();
            services.AddTransient<IUsersViewService, UsersViewService>();
            services.AddTransient<IProductsViewService, ProductsViewService>();

            // The demo cart holds per-user state for the circuit lifetime.
            services.AddScoped<ICartService, CartService>();

            return services;
        }
    }
}
