import { ReactElement } from 'react';
import { useAuth } from './authProvider';

type SecuredComponentsParameters = {
    children: ReactElement,
    allowedRoles?: Array<string>,
    deniedRoles?: Array<string>
}

export const SecuredComponent = ({ children, allowedRoles = [], deniedRoles = [] }: SecuredComponentsParameters): ReactElement => {
    const { isAuthenticated, userRoles } = useAuth();

    const userIsInRole = (roles: Array<string>): boolean => {
        let found = false;
        roles.forEach(r => {
            if (userRoles.indexOf(r) > -1) {
                found = true;
            }
        });
        return found;
    }

    if (isAuthenticated && userIsInRole(deniedRoles)) {
        return <></>
    }

    if (isAuthenticated && (allowedRoles.length === 0 || userIsInRole(allowedRoles))) {
        return <>{children}</>
    }

    return <></>;
}
