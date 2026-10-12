import { ReactNode } from 'react';
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
});
