import { ReactNode } from 'react';
import { AxiosError } from 'axios';
import {
    MutationCache,
    QueryClient,
    QueryClientProvider,
    useQuery
} from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { accountService } from './accountService';
import { queryClientGlobalOptions } from '../../brokers/apiBroker.globals';

const logoutAsync = vi.fn();

vi.mock('../../brokers/apiBroker.accounts', () => ({
    default: class {
        LogoutAsync = logoutAsync;
    }
}));

vi.mock('../../brokers/toastBroker.error', () => ({
    toastError: vi.fn()
}));

const { toastError } = await import('../../brokers/toastBroker.error');
const toastErrorMock = vi.mocked(toastError);

describe('accountService.useLogout', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    beforeEach(() => {
        vi.clearAllMocks();
        logoutAsync.mockResolvedValue(undefined);

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    it('should log the reader out through the broker', async () => {
        // given
        const { result } = renderHook(() => accountService.useLogout(), { wrapper });

        // when
        await result.current.mutateAsync();

        // then
        expect(logoutAsync).toHaveBeenCalledTimes(1);
    });
    // THE CURRENT USER READ, seeded for real and served by an active observer, so a hook that
    // invalidates it re-sends it at once. After a logout the server answers that nobody is
    // signed in, and the home page's load (#835, #836) is what reads who is signed in next.
    it('should not read the current user again once the reader is logged out', async () => {
        // given
        const currentUserRead = vi.fn().mockResolvedValue({ userId: 'reader-1' });

        renderHook(
            () => useQuery({
                queryKey: ['AccountsGetCurrentUser'],
                queryFn: currentUserRead
            }),
            { wrapper });

        await waitFor(() => expect(currentUserRead).toHaveBeenCalledTimes(1));

        const { result } = renderHook(() => accountService.useLogout(), { wrapper });

        // when
        await result.current.mutateAsync();

        // then
        await new Promise(resolve => setTimeout(resolve, 50));
        expect(queryClient.getQueryState(['AccountsGetCurrentUser'])?.isInvalidated).toBe(false);
        expect(currentUserRead).toHaveBeenCalledTimes(1);
    });
    // THE ERROR LEAVES THE HOOK UNCHANGED (§UI20.9.2 departure 2): neither swallowed nor wrapped.
    it("should fail with the broker's error when the logout fails", async () => {
        // given
        const brokerError = new AxiosError('Error: Logout failed.');
        logoutAsync.mockRejectedValue(brokerError);

        const { result } = renderHook(() => accountService.useLogout(), { wrapper });

        // when
        const logout = result.current.mutateAsync();

        // then
        await expect(logout).rejects.toBe(brokerError);
    });

    // A FAILED LOGOUT IS ANNOUNCED BY THE GLOBAL TOAST ALONE (§UI20.8 rule 3): the header and
    // the user menu show no message of their own. Driven through the app's own global handler,
    // with the toasts counted inside it, so a hook that suppresses it and raises its own toast
    // is not mistaken for one that leaves it on.
    it('should leave the global toast on when the logout fails', async () => {
        // given
        const brokerError = new AxiosError('Error: Logout failed.');
        logoutAsync.mockRejectedValue(brokerError);

        const globalOnError =
            queryClientGlobalOptions.getMutationCache().config.onError!;

        let toastsRaisedByTheGlobalHandler = 0;

        const globalClient = new QueryClient({
            mutationCache: new MutationCache({
                onError: (...args) => {
                    const toastsBefore = toastErrorMock.mock.calls.length;

                    try {
                        globalOnError(...args);
                    } finally {
                        toastsRaisedByTheGlobalHandler +=
                            toastErrorMock.mock.calls.length - toastsBefore;
                    }
                }
            })
        });

        const globalWrapper = ({ children }: { children: ReactNode }) => (
            <QueryClientProvider client={globalClient}>{children}</QueryClientProvider>
        );

        const { result } = renderHook(
            () => accountService.useLogout(), { wrapper: globalWrapper });

        // when
        await result.current.mutateAsync().catch(() => undefined);

        // then
        expect(toastsRaisedByTheGlobalHandler).toBe(1);
        expect(toastErrorMock).toHaveBeenCalledTimes(1);
        expect(toastErrorMock).toHaveBeenCalledWith(
            'An unknown error has occurred, please try again.');
    });
});
