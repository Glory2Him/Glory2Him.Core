import { RouteObject } from 'react-router-dom';
import ManageLayout from '../components/layouts/manageLayout';
import { SecuredRoute } from '../components/securitys/securedRoutes';
import { AccessDenied } from '../pages/account/accessDenied';
import { ForgotPassword } from '../pages/account/forgotPassword';
import { ForgotPasswordConfirmation } from '../pages/account/forgotPasswordConfirmation';
import { InvalidPasswordReset } from '../pages/account/invalidPasswordReset';
import { InvalidUser } from '../pages/account/invalidUser';
import { Lockout } from '../pages/account/lockout';
import { Login } from '../pages/account/login';
import { ChangePassword } from '../pages/account/manage/changePassword';
import { ManageIndex } from '../pages/account/manage/index';
import { Register } from '../pages/account/register';
import { ResetPassword } from '../pages/account/resetPassword';
import { ResetPasswordConfirmation } from '../pages/account/resetPasswordConfirmation';

// The Identity/account area, converted from Blazor's Components/Account pages. Spread these
// under the Root route's children: { path: "/", element: <Root />, children: [...accountRoutes] }.
export const accountRoutes: RouteObject[] = [
    { path: 'Account/Login', element: <Login /> },
    { path: 'Account/Register', element: <Register /> },
    { path: 'Account/ForgotPassword', element: <ForgotPassword /> },
    { path: 'Account/ForgotPasswordConfirmation', element: <ForgotPasswordConfirmation /> },
    { path: 'Account/ResetPassword', element: <ResetPassword /> },
    { path: 'Account/ResetPasswordConfirmation', element: <ResetPasswordConfirmation /> },
    { path: 'Account/InvalidPasswordReset', element: <InvalidPasswordReset /> },
    { path: 'Account/AccessDenied', element: <AccessDenied /> },
    { path: 'Account/Lockout', element: <Lockout /> },
    { path: 'Account/InvalidUser', element: <InvalidUser /> },
    {
        element: <ManageLayout />,
        children: [
            {
                path: 'Account/Manage',
                element:
                    <SecuredRoute>
                        <ManageIndex />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/ChangePassword',
                element:
                    <SecuredRoute>
                        <ChangePassword />
                    </SecuredRoute>
            },
        ]
    },
];
