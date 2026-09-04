import { RouteObject } from 'react-router-dom';
import SidebarLayout from '../components/layouts/sidebarLayout';
import { SecuredRoute } from '../components/securitys/securedRoutes';
import securityPoints from '../securityMatrix';
import { Dashboard } from '../pages/dashboard';
import { ApprovalSettingDetailPage } from '../pages/admin/approvalSettingDetailPage';
import { ApprovalSettingsPage } from '../pages/admin/approvalSettingsPage';
import { ContentItemSettingDetailPage } from '../pages/admin/contentItemSettingDetailPage';
import { ContentItemSettingsPage } from '../pages/admin/contentItemSettingsPage';
import {
    ContentItemModerationDetailPage
} from '../pages/admin/contentItemModerationDetailPage';

import { ContentItemModerationPage } from '../pages/admin/contentItemModerationPage';
import { UserDetailPage } from '../pages/admin/userDetailPage';
import { UsersPage } from '../pages/admin/usersPage';

// The authenticated sidebar area, mirroring the Blazor pages' @layout SidebarLayout and
// [Authorize] attributes: the dashboard only requires authentication; the admin pages
// require the Administrators role.
export const adminRoutes: RouteObject[] = [
    {
        element: <SidebarLayout />,
        children: [
            {
                path: 'Dashboard',
                element:
                    <SecuredRoute>
                        <Dashboard />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/Users',
                element:
                    <SecuredRoute allowedRoles={securityPoints.users.view}>
                        <UsersPage />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/Users/:userId',
                element:
                    <SecuredRoute allowedRoles={securityPoints.users.view}>
                        <UserDetailPage />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/ContentItemSettings',
                element:
                    <SecuredRoute allowedRoles={securityPoints.contentItemSettings.view}>
                        <ContentItemSettingsPage />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/ContentItemSettings/:contentItemSettingId',
                element:
                    <SecuredRoute allowedRoles={securityPoints.contentItemSettings.edit}>
                        <ContentItemSettingDetailPage />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/ApprovalSettings',
                element:
                    <SecuredRoute allowedRoles={securityPoints.approvalSettings.view}>
                        <ApprovalSettingsPage />
                    </SecuredRoute>,
            },
            {
                // CREATE IS ITS OWN ROUTE, ahead of the id route: the host seeds the
                // entity-type defaults, but a content-type policy is an administrator's to
                // write, and adding a policy is a different permission from amending one. A
                // static segment outranks a dynamic one, so New never reads as an id.
                path: 'Admin/ApprovalSettings/New',
                element:
                    <SecuredRoute allowedRoles={securityPoints.approvalSettings.add}>
                        <ApprovalSettingDetailPage isNew />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/ApprovalSettings/:approvalSettingId',
                element:
                    <SecuredRoute allowedRoles={securityPoints.approvalSettings.edit}>
                        <ApprovalSettingDetailPage />
                    </SecuredRoute>,
            },
            {
                // The content item moderation queue — the ContentItemListPanel family over the
                // Draft + Submitted statuses. The demo Post table that used to answer here moved
                // to Admin/SamplePosts, because /posts is the content item collection now and
                // its admin surface belongs at the matching address.
                //
                // Administrators for the ROUTE, for now: SecuredRoute takes a fixed role list
                // and the review tier is suffix-matched (§18.6), which a list cannot express —
                // widening who reaches this surface is #361's scope, and the foundation decides
                // data visibility against the stored row regardless.
                path: 'Admin/Posts',
                element:
                    <SecuredRoute allowedRoles={securityPoints.contentItems.view}>
                        <ContentItemModerationPage />
                    </SecuredRoute>,
            },
            {
                // ONE ITEM FROM THE QUEUE, in the admin shell. A moderator stepping into a post
                // is still working the admin area, so the queue leads here rather than out to
                // the public /posts/{id} — which would swap the chrome, drop the sidebar and
                // lose the filtered queue they were part-way through.
                //
                // Gated by the same point as the queue: reaching an item must take no more
                // than reaching the list it sits in, and the foundation decides what this
                // caller's roles actually reach against the stored row regardless (§14.5).
                path: 'Admin/Posts/:contentItemId',
                element:
                    <SecuredRoute allowedRoles={securityPoints.contentItems.view}>
                        <ContentItemModerationDetailPage />
                    </SecuredRoute>,
            },
        ],
    },
];
