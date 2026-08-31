import { RouteObject } from 'react-router-dom';
import SidebarLayout from '../components/layouts/sidebarLayout';
import { SecuredRoute } from '../components/securitys/securedRoutes';
import securityPoints from '../securityMatrix';
import { Dashboard } from '../pages/dashboard';
import { ContentItemSettingDetailPage } from '../pages/admin/contentItemSettingDetailPage';
import { ContentItemSettingsPage } from '../pages/admin/contentItemSettingsPage';
import { ContentItemModerationPage } from '../pages/admin/contentItemModerationPage';
import { PostDetailPage } from '../pages/admin/postDetailPage';
import { PostsPage } from '../pages/admin/postsPage';
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
                // The content item moderation queue — the ContentItemSearchPanel family over the
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
                path: 'Admin/SamplePosts',
                element:
                    <SecuredRoute allowedRoles={securityPoints.posts.view}>
                        <PostsPage />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/SamplePosts/New',
                element:
                    <SecuredRoute allowedRoles={securityPoints.posts.add}>
                        <PostDetailPage />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/SamplePosts/:postId',
                element:
                    <SecuredRoute allowedRoles={securityPoints.posts.edit}>
                        <PostDetailPage />
                    </SecuredRoute>,
            },
        ],
    },
];
