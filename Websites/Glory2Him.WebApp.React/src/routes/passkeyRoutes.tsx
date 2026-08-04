import { RouteObject } from 'react-router-dom';
import ManageLayout from '../components/layouts/manageLayout';
import { SecuredRoute } from '../components/securitys/securedRoutes';
import { ExternalLogins } from '../pages/account/manage/externalLogins';
import { Passkeys } from '../pages/account/manage/passkeys';
import { RenamePasskey } from '../pages/account/manage/renamePasskey';

// The passkey and external-login area of Account/Manage, converted from Blazor's
// Manage/Passkeys, Manage/RenamePasskey and Manage/ExternalLogins pages. Spread
// these under the Root route's children alongside accountRoutes.
export const passkeyRoutes: RouteObject[] = [
    {
        element: <ManageLayout />,
        children: [
            {
                path: 'Account/Manage/Passkeys',
                element:
                    <SecuredRoute>
                        <Passkeys />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/RenamePasskey/:id',
                element:
                    <SecuredRoute>
                        <RenamePasskey />
                    </SecuredRoute>
            },
            {
                path: 'Account/Manage/ExternalLogins',
                element:
                    <SecuredRoute>
                        <ExternalLogins />
                    </SecuredRoute>
            },
        ]
    },
];
