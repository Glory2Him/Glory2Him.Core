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
import { manageAccountService } from './manageAccountService';
import { queryClientGlobalOptions } from '../../brokers/apiBroker.globals';

const deletePersonalDataAsync = vi.fn();

vi.mock('../../brokers/apiBroker.manageAccounts', () => ({
    default: class {
        DeletePersonalDataAsync = deletePersonalDataAsync;
    }
}));

vi.mock('../../brokers/toastBroker.error', () => ({
    toastError: vi.fn()
}));

const { toastError } = await import('../../brokers/toastBroker.error');
const toastErrorMock = vi.mocked(toastError);

const readersPassword = 'readers-password';

describe('manageAccountService.useDeletePersonalData', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    beforeEach(() => {
        vi.clearAllMocks();
        deletePersonalDataAsync.mockResolvedValue(undefined);

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    it("should send the reader's password through the broker", async () => {
        // given
        const { result } = renderHook(
            () => manageAccountService.useDeletePersonalData(), { wrapper });

        // when
        await result.current.mutateAsync(readersPassword);

        // then
        expect(deletePersonalDataAsync).toHaveBeenCalledTimes(1);
        expect(deletePersonalDataAsync).toHaveBeenCalledWith(readersPassword);
    });

    // THE CURRENT USER READ, seeded for real and served by an active observer, so a hook that
    // invalidates it re-sends it at once. After a deletion the server answers that nobody is
    // signed in, and the page's load (#837) is what reads who is signed in next.
    it('should not read the current user again once the account is deleted', async () => {
        // given
        const currentUserRead = vi.fn().mockResolvedValue({ userId: 'reader-1' });

        renderHook(
            () => useQuery({
                queryKey: ['AccountsGetCurrentUser'],
                queryFn: currentUserRead
            }),
            { wrapper });

        await waitFor(() => expect(currentUserRead).toHaveBeenCalledTimes(1));

        const { result } = renderHook(
            () => manageAccountService.useDeletePersonalData(), { wrapper });

        // when
        await result.current.mutateAsync(readersPassword);

        // then
        await new Promise(resolve => setTimeout(resolve, 50));
        expect(queryClient.getQueryState(['AccountsGetCurrentUser'])?.isInvalidated).toBe(false);
        expect(currentUserRead).toHaveBeenCalledTimes(1);
    });

    // A FAILED DELETION IS ANNOUNCED BY THE PAGE ALONE (#846). Driven through the app's own
    // global handler, with the toasts counted inside it, so a hook that suppresses the handler
    // and raises its own toast is not mistaken for one that raises none.
    it('should leave the failure to the page without the global toast when the deletion fails', async () => {
        // given
        const brokerError = new Error('refused');
        deletePersonalDataAsync.mockRejectedValue(brokerError);

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
            () => manageAccountService.useDeletePersonalData(), { wrapper: globalWrapper });

        // when
        await result.current.mutateAsync(readersPassword).catch(() => undefined);

        // then
        expect(toastsRaisedByTheGlobalHandler).toBe(0);
        expect(toastErrorMock).not.toHaveBeenCalled();
    });

    // THE ERROR LEAVES THE HOOK UNCHANGED (§UI20.9.2 departure 2): with the toast off, it is
    // the only way the failure reaches the reader, through the page's message.
    it("should fail with the broker's error when the deletion fails", async () => {
        // given
        const brokerError = new AxiosError('Error: Incorrect password.');
        deletePersonalDataAsync.mockRejectedValue(brokerError);

        const { result } = renderHook(
            () => manageAccountService.useDeletePersonalData(), { wrapper });

        // when
        const deletion = result.current.mutateAsync(readersPassword);

        // then
        await expect(deletion).rejects.toBe(brokerError);
    });
});
