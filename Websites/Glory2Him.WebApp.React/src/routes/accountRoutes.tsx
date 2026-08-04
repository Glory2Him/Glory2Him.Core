import { RouteObject } from 'react-router-dom';
import ManageLayout from '../components/layouts/manageLayout';
import { SecuredRoute } from '../components/securitys/securedRoutes';
import { AccessDenied } from '../pages/account/accessDenied';
import { ConfirmEmail } from '../pages/account/confirmEmail';
import { ConfirmEmailChange } from '../pages/account/confirmEmailChange';
import { ForgotPassword } from '../pages/account/forgotPassword';
import { ForgotPasswordConfirmation } from '../pages/account/forgotPasswordConfirmation';
import { InvalidPasswordReset } from '../pages/account/invalidPasswordReset';
import { InvalidUser } from '../pages/account/invalidUser';
import { Lockout } from '../pages/account/lockout';
import { Login } from '../pages/account/login';
import { LoginWith2fa } from '../pages/account/loginWith2fa';
import { LoginWithRecoveryCode } from '../pages/account/loginWithRecoveryCode';
import { ChangePassword } from '../pages/account/manage/changePassword';
import { DeletePersonalData } from '../pages/account/manage/deletePersonalData';
import { Disable2fa } from '../pages/account/manage/disable2fa';
import { Email } from '../pages/account/manage/email';
import { EnableAuthenticator } from '../pages/account/manage/enableAuthenticator';
import { GenerateRecoveryCodes } from '../pages/account/manage/generateRecoveryCodes';
import { ManageIndex } from '../pages/account/manage/index';
import { PersonalData } from '../pages/account/manage/personalData';
import { ResetAuthenticator } from '../pages/account/manage/resetAuthenticator';
import { TwoFactorAuthentication } from '../pages/account/manage/twoFactorAuthentication';
import { Register } from '../pages/account/register';
import { RegisterConfirmation } from '../pages/account/registerConfirmation';
import { ResendEmailConfirmation } from '../pages/account/resendEmailConfirmation';
import { ResetPassword } from '../pages/account/resetPassword';
import { ResetPasswordConfirmation } from '../pages/account/resetPasswordConfirmation';

// The Identity/account area, converted from Blazor's Components/Account pages. Spread these
// under the Root route's children: { path: "/", element: <Root />, children: [...accountRoutes] }.
export const accountRoutes: RouteObject[] = [
    { path: 'Account/Login', element: <Login /> },
    { path: 'Account/LoginWith2fa', element: <LoginWith2fa /> },
    { path: 'Account/LoginWithRecoveryCode', element: <LoginWithRecoveryCode /> },
    { path: 'Account/Register', element: <Register /> },
    { path: 'Account/RegisterConfirmation', element: <RegisterConfirmation /> },
    { path: 'Account/ResendEmailConfirmation', element: <ResendEmailConfirmation /> },
    { path: 'Account/ConfirmEmail', element: <ConfirmEmail /> },
    { path: 'Account/ConfirmEmailChange', element: <ConfirmEmailChange /> },
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
            {
                path: 'Account/Manage/Email',
                element:
                    <SecuredRoute>
                        <Email />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/TwoFactorAuthentication',
                element:
                    <SecuredRoute>
                        <TwoFactorAuthentication />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/EnableAuthenticator',
                element:
                    <SecuredRoute>
                        <EnableAuthenticator />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/Disable2fa',
                element:
                    <SecuredRoute>
                        <Disable2fa />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/GenerateRecoveryCodes',
                element:
                    <SecuredRoute>
                        <GenerateRecoveryCodes />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/ResetAuthenticator',
                element:
                    <SecuredRoute>
                        <ResetAuthenticator />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/PersonalData',
                element:
                    <SecuredRoute>
                        <PersonalData />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/DeletePersonalData',
                element:
                    <SecuredRoute>
                        <DeletePersonalData />
                    </SecuredRoute>
            },
        ]
    },
];
