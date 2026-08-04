import { ReactElement } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from './authProvider';

type SecuredLinkParameters = {
    to: string,
    children: string,
    className?: string,
    allowedRoles?: Array<string>,
    deniedRoles?: Array<string>
}

export const SecuredLink = ({ to, children, className, allowedRoles = [], deniedRoles = [] }: SecuredLinkParameters): ReactElement => {
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
        return <span><Link to={to} className={className}>{children}</Link></span>
    }

    return <></>;
}
