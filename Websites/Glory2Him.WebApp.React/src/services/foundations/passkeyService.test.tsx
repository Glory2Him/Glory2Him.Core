import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { passkeyService } from './passkeyService';

const { requestPasskeyAndSignInAsync } = vi.hoisted(() => ({
    requestPasskeyAndSignInAsync: vi.fn()
}));

vi.mock('../../hooks/usePasskeys', () => ({
    createAndRegisterPasskeyAsync: vi.fn(),
    requestPasskeyAndSignInAsync
}));

const signedInUser = { userId: 'user-1', displayName: 'Reader' };

describe('passkeyService.usePasskeySignIn', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    beforeEach(() => {
        vi.clearAllMocks();
        requestPasskeyAndSignInAsync.mockResolvedValue(signedInUser);

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    it('should hand the ceremony the trimmed email', async () => {
        // given
        const { result } = renderHook(
            () => passkeyService.usePasskeySignIn(), { wrapper });

        // when
        await result.current.mutateAsync('  reader@example.com  ');

        // then
        expect(requestPasskeyAndSignInAsync).toHaveBeenCalledTimes(1);
        expect(requestPasskeyAndSignInAsync).toHaveBeenCalledWith('reader@example.com');
    });
});
