import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SecuredRoute } from './securedRoutes';
import {
    createAuthState,
    renderWithAuth,
    setLoading,
    signInAs,
    signOut
} from '../../tests/testAuth';

const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

describe('SecuredRoute', () => {
    beforeEach(() => {
        signOut(authState);
    });

    it('should render nothing while the current user is still loading', () => {
        // given
        setLoading(authState);

        // when
        const { container } = renderWithAuth(
            <SecuredRoute>
                <span>secured page</span>
            </SecuredRoute>);

        // then
        expect(container).toBeEmptyDOMElement();
    });

    it('should render children when authenticated and no roles are required', () => {
        // given
        signInAs(authState);

        // when
        renderWithAuth(
            <SecuredRoute>
                <span>secured page</span>
            </SecuredRoute>);

        // then
        expect(screen.getByText('secured page')).toBeInTheDocument();
    });

    it('should render children when the user holds an allowed role', () => {
        // given
        signInAs(authState, ['Administrators']);

        // when
        renderWithAuth(
            <SecuredRoute allowedRoles={['Administrators']}>
                <span>admin page</span>
            </SecuredRoute>);

        // then
        expect(screen.getByText('admin page')).toBeInTheDocument();
    });

    it('should show invalid access without a login button when the user lacks the allowed roles', () => {
        // given
        signInAs(authState, ['Members']);

        // when
        renderWithAuth(
            <SecuredRoute allowedRoles={['Administrators']}>
                <span>admin page</span>
            </SecuredRoute>);

        // then
        expect(screen.queryByText('admin page')).not.toBeInTheDocument();
        expect(screen.getByText('Invalid Access')).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Login' })).not.toBeInTheDocument();
    });

    it('should show invalid access when the user holds a denied role', () => {
        // given
        signInAs(authState, ['Administrators', 'Banned']);

        // when
        renderWithAuth(
            <SecuredRoute
                allowedRoles={['Administrators']}
                deniedRoles={['Banned']}>
                <span>admin page</span>
            </SecuredRoute>);

        // then
        expect(screen.queryByText('admin page')).not.toBeInTheDocument();
        expect(screen.getByText('Invalid Access')).toBeInTheDocument();
    });

    it('should show access restricted with a login button for an anonymous user', () => {
        // given
        signOut(authState);

        // when
        renderWithAuth(
            <SecuredRoute>
                <span>secured page</span>
            </SecuredRoute>);

        // then
        expect(screen.queryByText('secured page')).not.toBeInTheDocument();
        expect(screen.getByText('Access Restricted')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Login' })).toBeInTheDocument();
    });
});
