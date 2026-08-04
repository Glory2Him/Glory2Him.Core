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
                    Href: "Dashboard",
                    RequiresAuth: true,
                    ExactMatch: true,
                    Area: NavArea.Admin),

                new NavItem(
                    Title: "Admin",
                    Icon: "bi-gear",
                    Href: "",
                    Roles: new[] { "Administrators" },
                    RequiresAuth: true,
                    Area: NavArea.Admin,
                    Children: new[]
                    {
                        new NavItem("Users", "bi-people", "Admin/Users",
                            Roles: new[] { "Administrators" }, RequiresAuth: true),

                        new NavItem("Posts", "bi-file-earmark-text", "Admin/Posts",
                            Roles: new[] { "Administrators" }, RequiresAuth: true),
                    }),

                SamplePagesSection,

                new NavItem(
                    Title: "My Account",
                    Icon: "bi-person",
                    Href: "",
                    RequiresAuth: true,
                    Area: NavArea.Account,
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

        // Layout demos ported from the Blogzine template, shown full width inside the Glory 2 Him
        // header and footer. Each demo carries a "Back to Sample Pages" button that returns to
        // /SamplePages, which renders in the admin shell with this navigation still on the left.
        private static readonly string[] AdministratorsOnly = new[] { "Administrators" };

        // Public so the /SamplePages landing page lists exactly what the sidebar offers — one tree,
        // no second catalogue to keep in step.
        public static NavItem GetSamplePagesSection() =>
            SamplePagesSection;

        private static NavItem SamplePagesSection =>
            new NavItem(
                Title: "Sample Pages",
                Icon: "bi-collection",
                Href: "",
                Roles: AdministratorsOnly,
                RequiresAuth: true,
                Area: NavArea.Admin,
                Children: new[]
                {
                    new NavItem("Home", "bi-house", "SamplePages/Home", Children: new[]
                    {
                        Sample("Home Default", "SamplePages/Home/Default"),
                        Sample("Magazine", "SamplePages/Home/Magazine"),
                        Sample("Blog Classic", "SamplePages/Home/Blog-Classic"),
                        Sample("Blog Tech", "SamplePages/Home/Blog-Tech"),
                        Sample("Blog Podcast", "SamplePages/Home/Blog-Podcast"),
                    }),

                    new NavItem("Pages", "bi-files", "SamplePages/Pages", Children: new[]
                    {
                        Sample("About", "SamplePages/Pages/About"),
                        Sample("Contact", "SamplePages/Pages/Contact"),
                        Sample("Error 404", "SamplePages/Pages/Error-404"),
                        Sample("Signin", "SamplePages/Pages/Signin"),
                        Sample("Signup", "SamplePages/Pages/Signup"),
                        Sample("Offline", "SamplePages/Pages/Offline"),
                    }),

                    new NavItem("Post", "bi-file-post", "SamplePages/Post", Children: new[]
                    {
                        new NavItem("Post Grid", "bi-grid", "SamplePages/Post/Post-Grid", Children: new[]
                        {
                            Sample("Post Grid", "SamplePages/Post/Post-Grid/Post-Grid"),
                            Sample("Post Grid 4 Col", "SamplePages/Post/Post-Grid/Post-Grid-4-Col"),
                            Sample("Post Grid Masonry", "SamplePages/Post/Post-Grid/Post-Grid-Masonry"),
                            Sample("Post Grid Masonry Filter", "SamplePages/Post/Post-Grid/Post-Grid-Masonry-Filter"),
                            Sample("Post Mixed Large Then Grid", "SamplePages/Post/Post-Grid/Post-Mixed-Large-Then-Grid"),
                        }),

                        Sample("Post List", "SamplePages/Post/Post-List"),
                        Sample("Post Card", "SamplePages/Post/Post-Card"),
                        Sample("Post Overlay", "SamplePages/Post/Post-Overlay"),
                        Sample("Post Types", "SamplePages/Post/Post-Types"),
                        Sample("Post Single Magazine", "SamplePages/Post/Post-Single-Magazine"),
                        Sample("Post Single Classic", "SamplePages/Post/Post-Single-Classic"),
                        Sample("Post Single Minimal", "SamplePages/Post/Post-Single-Minimal"),
                        Sample("Post Single Card", "SamplePages/Post/Post-Single-Card"),
                        Sample("Post Single Review", "SamplePages/Post/Post-Single-Review"),
                        Sample("Post Single Video", "SamplePages/Post/Post-Single-Video"),
                        Sample("Podcast Single", "SamplePages/Post/Podcast-Single"),
                        Sample("Pagination Styles", "SamplePages/Post/Pagination-Styles"),
                    }),

                    new NavItem("Bible References", "bi-book",
                        "SamplePages/BibleReferences", Children: new[]
                        {
                            Sample("Bible Reference - Partial",
                                "SamplePages/BibleReferences/BibleReference-Single-verse"),

                            Sample("Bible Reference - Full Chapter",
                                "SamplePages/BibleReferences/BibleReference-Full-Chapter"),
                        }),

                    Sample("Lifestyle", "SamplePages/Lifestyle", "bi-stars"),
                    Sample("Dashboard", "SamplePages/Dashboard", "bi-speedometer2"),
                });

        private static NavItem Sample(string title, string href, string icon = "bi-dot") =>
            new NavItem(
                Title: title,
                Icon: icon,
                Href: href,
                Roles: AdministratorsOnly,
                RequiresAuth: true,
                ExactMatch: true);

        public static IReadOnlyList<NavItem> GetNavMenu(string relativePath)
        {
            NavArea area = ResolveArea(relativePath);

            return GetNavMenu()
                .Where(item => item.Area == area)
                .ToList();
        }

        // Anything under /Account belongs to the signed-in user; everything else that reaches the
        // sidebar (the dashboard and /Admin) is administration.
        public static NavArea ResolveArea(string relativePath)
        {
            string path = (relativePath ?? string.Empty).TrimStart('/');

            return path.StartsWith("Account", StringComparison.OrdinalIgnoreCase)
                ? NavArea.Account
                : NavArea.Admin;
        }
    }
}
