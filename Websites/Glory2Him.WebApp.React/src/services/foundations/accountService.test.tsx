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
});
