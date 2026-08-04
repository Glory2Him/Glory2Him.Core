import { NavArea, NavItem } from "../../../models/views/navigations/navItem";

// Static menu tree for the dashboard/admin sidebar, ported from the Blazor project's
// NavMenuProvider. Icons are Bootstrap Icons (bi-*), which the Blogzine template already
// ships — the CoreUI `cil-*` icon set is not loaded in this site.
const administratorsOnly = ["Administrators"];

const sample = (title: string, href: string, icon: string = "bi-dot"): NavItem => ({
    title,
    icon,
    href,
    roles: administratorsOnly,
    requiresAuth: true,
    exactMatch: true
});

// Layout demos ported from the Blogzine template, shown full width inside the Glory 2 Him
// header and footer. Each demo carries a "Back to Sample Pages" button that returns to
// /SamplePages, which renders in the admin shell with this navigation still on the left.
const samplePagesSection: NavItem = {
    title: "Sample Pages",
    icon: "bi-collection",
    href: "",
    roles: administratorsOnly,
    requiresAuth: true,
    area: NavArea.Admin,
    children: [
        {
            title: "Home", icon: "bi-house", href: "SamplePages/Home",
            children: [
                sample("Home Default", "SamplePages/Home/Default"),
                sample("Magazine", "SamplePages/Home/Magazine"),
                sample("Blog Classic", "SamplePages/Home/Blog-Classic"),
                sample("Blog Tech", "SamplePages/Home/Blog-Tech"),
                sample("Blog Podcast", "SamplePages/Home/Blog-Podcast"),
            ]
        },
        {
            title: "Pages", icon: "bi-files", href: "SamplePages/Pages",
            children: [
                sample("About", "SamplePages/Pages/About"),
                sample("Contact", "SamplePages/Pages/Contact"),
                sample("Error 404", "SamplePages/Pages/Error-404"),
                sample("Signin", "SamplePages/Pages/Signin"),
                sample("Signup", "SamplePages/Pages/Signup"),
                sample("Offline", "SamplePages/Pages/Offline"),
            ]
        },
        {
            title: "Post", icon: "bi-file-post", href: "SamplePages/Post",
            children: [
                {
                    title: "Post Grid", icon: "bi-grid",
                    href: "SamplePages/Post/Post-Grid",
                    children: [
                        sample("Post Grid", "SamplePages/Post/Post-Grid/Post-Grid"),
                        sample("Post Grid 4 Col", "SamplePages/Post/Post-Grid/Post-Grid-4-Col"),
                        sample("Post Grid Masonry", "SamplePages/Post/Post-Grid/Post-Grid-Masonry"),
                        sample("Post Grid Masonry Filter", "SamplePages/Post/Post-Grid/Post-Grid-Masonry-Filter"),
                        sample("Post Mixed Large Then Grid", "SamplePages/Post/Post-Grid/Post-Mixed-Large-Then-Grid"),
                    ]
                },
                sample("Post List", "SamplePages/Post/Post-List"),
                sample("Post Card", "SamplePages/Post/Post-Card"),
                sample("Post Overlay", "SamplePages/Post/Post-Overlay"),
                sample("Post Types", "SamplePages/Post/Post-Types"),
                sample("Post Single Magazine", "SamplePages/Post/Post-Single-Magazine"),
                sample("Post Single Classic", "SamplePages/Post/Post-Single-Classic"),
                sample("Post Single Minimal", "SamplePages/Post/Post-Single-Minimal"),
                sample("Post Single Card", "SamplePages/Post/Post-Single-Card"),
                sample("Post Single Review", "SamplePages/Post/Post-Single-Review"),
                sample("Post Single Video", "SamplePages/Post/Post-Single-Video"),
                sample("Podcast Single", "SamplePages/Post/Podcast-Single"),
                sample("Pagination Styles", "SamplePages/Post/Pagination-Styles"),
            ]
        },
        {
            title: "Bible References", icon: "bi-book",
            href: "SamplePages/BibleReferences",
            children: [
                sample(
                    "Bible Reference - Partial",
                    "SamplePages/BibleReferences/BibleReference-Single-verse"),

                sample(
                    "Bible Reference - Full Chapter",
                    "SamplePages/BibleReferences/BibleReference-Full-Chapter"),
            ]
        },
        sample("Lifestyle", "SamplePages/Lifestyle", "bi-stars"),
        sample("Dashboard", "SamplePages/Dashboard", "bi-speedometer2"),
    ]
};

const getFullNavMenu = (): NavItem[] => [
    {
        title: "Dashboard",
        icon: "bi-speedometer2",
        href: "Dashboard",
        requiresAuth: true,
        exactMatch: true,
        area: NavArea.Admin
    },
    {
        title: "Admin",
        icon: "bi-gear",
        href: "",
        roles: administratorsOnly,
        requiresAuth: true,
        area: NavArea.Admin,
        children: [
            {
                title: "Users", icon: "bi-people", href: "Admin/Users",
                roles: administratorsOnly, requiresAuth: true
            },
            {
                title: "Posts", icon: "bi-file-earmark-text", href: "Admin/Posts",
                roles: administratorsOnly, requiresAuth: true
            },
        ]
    },
    // samplePagesSection returns here once the Blogzine template demo pages are
    // converted; linking to them now would only hit the NotFound catch-all.
    {
        title: "My Account",
        icon: "bi-person",
        href: "",
        requiresAuth: true,
        area: NavArea.Account,
        children: [
            {
                title: "Profile", icon: "bi-person", href: "Account/Manage",
                requiresAuth: true, exactMatch: true
            },
            // Email, Two-factor Authentication, Passkeys and Personal Data return here
            // once their flows are converted (they have no JSON endpoints yet).
            {
                title: "Password", icon: "bi-lock",
                href: "Account/Manage/ChangePassword", requiresAuth: true
            },
        ]
    }
];

// Anything under /Account belongs to the signed-in user; everything else that reaches the
// sidebar (the dashboard and /Admin) is administration.
const resolveArea = (relativePath: string): NavArea => {
    const path = (relativePath ?? "").replace(/^\/+/, "");

    return path.toLowerCase().startsWith("account")
        ? NavArea.Account
        : NavArea.Admin;
};

export const navMenuProvider = {
    getNavMenu: (relativePath?: string): NavItem[] => {
        if (relativePath === undefined) {
            return getFullNavMenu();
        }

        const area = resolveArea(relativePath);

        return getFullNavMenu()
            .filter((item) => (item.area ?? NavArea.Admin) === area);
    },

    // Public so a /SamplePages landing page can list exactly what the sidebar offers — one
    // tree, no second catalogue to keep in step.
    getSamplePagesSection: (): NavItem =>
        samplePagesSection,

    resolveArea
};
