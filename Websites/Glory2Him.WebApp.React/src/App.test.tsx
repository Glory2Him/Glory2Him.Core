import { render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CurrentUser } from './models/accounts/currentUser';
import App from './App';

// The guard is mocked to a marker that names the reader useAuth gives it. Outside AuthProvider,
// useAuth answers the context's default, nobody signed in, so one marker naming the signed-in
// reader proves the guard is mounted once and inside the provider.
vi.mock('./components/securitys/restoredPageGuard', async () => {
    const { useAuth } = await import('./components/securitys/authProvider');

    return {
        RestoredPageGuard: () => {
            const { user } = useAuth();

            return <span data-testid="restored-page-guard">{user?.userId || 'nobody'}</span>;
        }
    };
});

vi.mock('./services/foundations/accountService', async importOriginal => {
    const original = await importOriginal<typeof import('./services/foundations/accountService')>();

    const signedInReader = new CurrentUser({
        isAuthenticated: true,
        userId: 'reader-a',
        userName: 'readera',
        displayName: 'Reader A'
    });

    return {
        accountService: {
            ...original.accountService,
            useGetCurrentUser: () => ({ data: signedInReader, isLoading: false, refetch: vi.fn() })
        }
    };
});

describe('App', () => {
    // The first route's own reads (the feed, the settings, the reactions and the front-end
    // configuration) are held unsent: the test is about where the guard is mounted.
    beforeEach(() => {
        vi.spyOn(XMLHttpRequest.prototype, 'send').mockImplementation(() => { });
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it('should mount the restored-page guard once inside the auth provider', () => {
        // given . when
        render(<App />);

        // then
        const guards = screen.getAllByTestId('restored-page-guard');
        expect(guards).toHaveLength(1);
        expect(guards[0]).toHaveTextContent('reader-a');
    });
});
