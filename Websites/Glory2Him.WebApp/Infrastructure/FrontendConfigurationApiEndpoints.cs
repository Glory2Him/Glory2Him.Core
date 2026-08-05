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

namespace Glory2Him.WebApp.Infrastructure
{
    // Anonymous JSON endpoint consumed by the React SPA (Glory2Him.WebApp.React).
    // Exposes the public frontend configuration values the SPA needs at startup —
    // currently only the YouVersion Platform app key, which is a publishable
    // client-side key (not a secret) read from the "FrontendConfiguration"
    // section of appsettings.
    public static class FrontendConfigurationApiEndpoints
    {
        public static IEndpointRouteBuilder MapFrontendConfigurationApiEndpoints(
            this IEndpointRouteBuilder endpoints)
        {
            RouteGroupBuilder frontendConfigurationsGroup =
                endpoints.MapGroup("/api/frontend-configurations");

            frontendConfigurationsGroup.MapGet("/", (IConfiguration configuration) =>
            {
                string youVersionAppKey =
                    configuration["FrontendConfiguration:YouVersion:AppKey"] ?? string.Empty;

                // The committed appsettings.json carries a placeholder so the setting's
                // shape is documented; the real key lives in appsettings.Development.json
                // (gitignored) or an environment override. The placeholder is treated as
                // "not configured" so the SPA degrades gracefully instead of mounting the
                // SDK with a key that cannot work.
                if (youVersionAppKey == "your-real-key-here")
                {
                    youVersionAppKey = string.Empty;
                }

                return Results.Ok(new
                {
                    YouVersionAppKey = youVersionAppKey,
                });
            });

            return endpoints;
        }
    }
}
