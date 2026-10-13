import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { liveUpdateService } from './liveUpdateService';

const startLiveUpdatesAsync = vi.fn();
const stopLiveUpdatesAsync = vi.fn();

vi.mock('../../brokers/hubBroker.liveUpdates', () => ({
    default: class {
        startLiveUpdatesAsync = startLiveUpdatesAsync;
        stopLiveUpdatesAsync = stopLiveUpdatesAsync;
    }
}));

describe('liveUpdateService.useLiveUpdates', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    beforeEach(() => {
        vi.resetAllMocks();
        startLiveUpdatesAsync.mockResolvedValue(undefined);
        stopLiveUpdatesAsync.mockResolvedValue(undefined);

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    // Mounts the hook and lets what its mount set going settle, without moving the clock.
    const mountTheHookAsync = async () => {
        const mountedHook = renderHook(() => liveUpdateService.useLiveUpdates(), { wrapper });
        await act(async () => undefined);

        return mountedHook;
    };

    it('should start one live connection when mounted', async () => {
        // when
        await mountTheHookAsync();

        // then
        expect(startLiveUpdatesAsync).toHaveBeenCalledTimes(1);

        expect(startLiveUpdatesAsync).toHaveBeenCalledWith(
            {
                onLiveUpdate: expect.any(Function),
                onReconnected: expect.any(Function),
                onClosed: expect.any(Function)
            },
            expect.any(Function));
    });
});
