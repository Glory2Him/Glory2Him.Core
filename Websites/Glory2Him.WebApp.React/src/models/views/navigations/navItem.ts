// The sidebar shows one area at a time, so running the site never means scrolling past
// sections that belong to a different job.
export enum NavArea {
    // Dashboard and the administration pages.
    Admin = "Admin",

    // The signed-in user's own account pages.
    Account = "Account",
}

// Children nest to any depth: a top-level item renders as a sidebar section heading, and any
// deeper item that still has children renders as a collapsible group (see navMenu.tsx).
export interface NavItem {
    title: string;
    icon: string;
    href: string;
    roles?: string[];
    requiresAuth?: boolean;
    children?: NavItem[];
    exactMatch?: boolean;
    area?: NavArea;
}

export const hasChildren = (item: NavItem): boolean =>
    (item.children?.length ?? 0) > 0;
