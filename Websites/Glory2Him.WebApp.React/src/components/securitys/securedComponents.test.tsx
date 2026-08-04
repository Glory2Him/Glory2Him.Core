import { screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SecuredComponent } from './securedComponents';
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

describe('SecuredComponent', () => {
    beforeEach(() => {
        signOut(authState);
    });

    it('should render children when authenticated and no roles are required', () => {
        // given
        signInAs(authState);

        // when
        renderWithAuth(
            <SecuredComponent>
                <span>secret content</span>
            </SecuredComponent>);

        // then
        expect(screen.getByText('secret content')).toBeInTheDocument();
    });

    it('should render children when the user holds one of the allowed roles', () => {
        // given
        signInAs(authState, ['Members', 'Administrators']);

        // when
        renderWithAuth(
            <SecuredComponent allowedRoles={['Administrators']}>
                <span>admin content</span>
            </SecuredComponent>);

        // then
        expect(screen.getByText('admin content')).toBeInTheDocument();
    });

    it('should render nothing when the user lacks every allowed role', () => {
        // given
        signInAs(authState, ['Members']);

        // when
        renderWithAuth(
            <SecuredComponent allowedRoles={['Administrators']}>
                <span>admin content</span>
            </SecuredComponent>);

        // then
        expect(screen.queryByText('admin content')).not.toBeInTheDocument();
    });

    it('should render nothing when the user holds a denied role', () => {
        // given
        signInAs(authState, ['Administrators', 'Banned']);

        // when
        renderWithAuth(
            <SecuredComponent
                allowedRoles={['Administrators']}
                deniedRoles={['Banned']}>
                <span>admin content</span>
            </SecuredComponent>);

        // then
        expect(screen.queryByText('admin content')).not.toBeInTheDocument();
    });

    it('should render nothing for an anonymous user', () => {
        // given
        signOut(authState);

        // when
        renderWithAuth(
            <SecuredComponent>
                <span>secret content</span>
            </SecuredComponent>);

        // then
        expect(screen.queryByText('secret content')).not.toBeInTheDocument();
    });
});
