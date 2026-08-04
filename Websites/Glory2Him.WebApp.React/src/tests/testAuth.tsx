import { ReactElement, ReactNode } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { render, RenderResult } from '@testing-library/react';
import { AuthProvider } from '../components/securitys/authProvider';
import { CurrentUser } from '../models/accounts/currentUser';

// Shared authentication test double. Each security test file mocks
// services/foundations/accountService so useGetCurrentUser returns this mutable state,
// letting tests flip between anonymous / authenticated / role-holding users without
// touching the real broker or react-query.
export interface TestAuthState {
    data: CurrentUser | undefined;
    isLoading: boolean;
}

export const createAuthState = (): TestAuthState => ({
    data: undefined,
    isLoading: false
});

export const signInAs = (authState: TestAuthState, roles: Array<string> = []): void => {
    authState.data = new CurrentUser({
        isAuthenticated: true,
        userId: 'user-1',
        userName: 'tester',
        email: 'tester@example.com',
        displayName: 'Tester',
        roles
    });

    authState.isLoading = false;
};

export const signOut = (authState: TestAuthState): void => {
    authState.data = new CurrentUser({ isAuthenticated: false });
    authState.isLoading = false;
};

export const setLoading = (authState: TestAuthState): void => {
    authState.data = undefined;
    authState.isLoading = true;
};

export const renderWithAuth = (ui: ReactNode): RenderResult =>
    render(
        <MemoryRouter initialEntries={['/Secured/Page']}>
            <AuthProvider>{ui as ReactElement}</AuthProvider>
        </MemoryRouter>
    );
