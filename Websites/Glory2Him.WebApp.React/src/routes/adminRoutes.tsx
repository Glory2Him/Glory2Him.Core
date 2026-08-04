import { RouteObject } from 'react-router-dom';
import SidebarLayout from '../components/layouts/sidebarLayout';
import { SecuredRoute } from '../components/securitys/securedRoutes';
import securityPoints from '../securityMatrix';
import { Dashboard } from '../pages/dashboard';
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
                path: 'Admin/Posts',
                element:
                    <SecuredRoute allowedRoles={securityPoints.posts.view}>
                        <PostsPage />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/Posts/New',
                element:
                    <SecuredRoute allowedRoles={securityPoints.posts.add}>
                        <PostDetailPage />
                    </SecuredRoute>,
            },
            {
                path: 'Admin/Posts/:postId',
                element:
                    <SecuredRoute allowedRoles={securityPoints.posts.edit}>
                        <PostDetailPage />
                    </SecuredRoute>,
            },
        ],
    },
];
