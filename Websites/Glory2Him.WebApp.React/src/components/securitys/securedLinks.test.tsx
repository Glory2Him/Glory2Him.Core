import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SecuredLink } from './securedLinks';
import {
    createAuthState,
    renderWithAuth,
    signInAs,
    signOut
} from '../../tests/testAuth';

const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

describe('SecuredLink', () => {
    beforeEach(() => {
        signOut(authState);
    });

    it('should render a link with href, text and class when authenticated', () => {
        // given
        signInAs(authState);

        // when
        renderWithAuth(
            <SecuredLink to="/Dashboard" className="nav-link">
                Dashboard
            </SecuredLink>);

        // then
        const link = screen.getByRole('link', { name: 'Dashboard' });
        expect(link).toHaveAttribute('href', '/Dashboard');
        expect(link).toHaveClass('nav-link');
    });

    it('should render the link when the user holds an allowed role', () => {
        // given
        signInAs(authState, ['Administrators']);

        // when
        renderWithAuth(
            <SecuredLink to="/Admin/Users" allowedRoles={['Administrators']}>
                Users
            </SecuredLink>);

        // then
        expect(screen.getByRole('link', { name: 'Users' })).toBeInTheDocument();
    });

    it('should render nothing when the user lacks every allowed role', () => {
        // given
        signInAs(authState, ['Members']);

        // when
        renderWithAuth(
            <SecuredLink to="/Admin/Users" allowedRoles={['Administrators']}>
                Users
            </SecuredLink>);

        // then
        expect(screen.queryByRole('link')).not.toBeInTheDocument();
    });

    it('should render nothing when the user holds a denied role', () => {
        // given
        signInAs(authState, ['Banned']);

        // when
        renderWithAuth(
            <SecuredLink to="/Community" deniedRoles={['Banned']}>
                Community
            </SecuredLink>);

        // then
        expect(screen.queryByRole('link')).not.toBeInTheDocument();
    });

    it('should render nothing for an anonymous user', () => {
        // given
        signOut(authState);

        // when
        renderWithAuth(
            <SecuredLink to="/Dashboard">Dashboard</SecuredLink>);

        // then
        expect(screen.queryByRole('link')).not.toBeInTheDocument();
    });
});
