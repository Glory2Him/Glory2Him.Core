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

using Glory2Him.WebApp.Models.Views.Navigations;

namespace Glory2Him.WebApp.Components.Navigation
{
    // Static menu tree for the dashboard/admin sidebar. Icons are Bootstrap Icons (bi-*), which the
    // Blogzine template already ships — the CoreUI `cil-*` icon set is not loaded in this site.
    public static class NavMenuProvider
    {
        public static IReadOnlyList<NavItem> GetNavMenu() =>
            new[]
            {
                new NavItem(
                    Title: "Dashboard",
                    Icon: "bi-speedometer2",
                    Href: "dashboard",
                    RequiresAuth: true,
                    ExactMatch: true),

                new NavItem(
                    Title: "Admin",
                    Icon: "bi-gear",
                    Href: "",
                    Roles: new[] { "Administrators" },
                    RequiresAuth: true,
                    Children: new[]
                    {
                        new NavItem("Users", "bi-people", "admin/users",
                            Roles: new[] { "Administrators" }, RequiresAuth: true),

                        new NavItem("Posts", "bi-file-earmark-text", "admin/posts",
                            Roles: new[] { "Administrators" }, RequiresAuth: true),
                    }),

                new NavItem(
                    Title: "My Account",
                    Icon: "bi-person",
                    Href: "",
                    RequiresAuth: true,
                    Children: new[]
                    {
                        new NavItem("Profile", "bi-person", "Account/Manage",
                            RequiresAuth: true, ExactMatch: true),

                        new NavItem("Email", "bi-envelope", "Account/Manage/Email",
                            RequiresAuth: true),

                        new NavItem("Password", "bi-lock", "Account/Manage/ChangePassword",
                            RequiresAuth: true),

                        new NavItem("Two-factor Authentication", "bi-shield-check",
                            "Account/Manage/TwoFactorAuthentication", RequiresAuth: true),

                        new NavItem("Passkeys", "bi-key", "Account/Manage/Passkeys",
                            RequiresAuth: true),

                        new NavItem("Personal Data", "bi-file-earmark",
                            "Account/Manage/PersonalData", RequiresAuth: true),
                    })
            };
    }
}
