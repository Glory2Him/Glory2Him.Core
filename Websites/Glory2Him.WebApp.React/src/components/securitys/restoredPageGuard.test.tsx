import { act, render } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CurrentUser } from '../../models/accounts/currentUser';
import { RestoredPageGuard } from './restoredPageGuard';

// The guard reads the current user through accountService. Each render hands it a NEW result
// object, as React Query does, so a guard that keeps the result of its first render cannot
// pass by having the shared state mutated under it.
const mocks = vi.hoisted(() => ({
    currentUser: undefined as unknown,
    refetch: vi.fn()
}));

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => ({
            data: mocks.currentUser,
            isLoading: mocks.currentUser === undefined,
            refetch: mocks.refetch
        })
    }
}));

const readerA = new CurrentUser({
    isAuthenticated: true,
    userId: 'reader-a',
    userName: 'readera',
    displayName: 'Reader A'
});

const readerB = new CurrentUser({
    isAuthenticated: true,
    userId: 'reader-b',
    userName: 'readerb',
    displayName: 'Reader B'
});

// The test environment treats a PageTransitionEvent as a plain Event and drops `persisted`
// from its constructor, so it is set on the event itself.
const dispatchPageTransition = (type: 'pagehide' | 'pageshow', persisted: boolean): void => {
    const event = new Event(type);
    Object.defineProperty(event, 'persisted', { value: persisted });
    window.dispatchEvent(event);
};

const answerFreshRead = (currentUser: CurrentUser | undefined): void => {
    mocks.refetch.mockResolvedValue({ data: currentUser, isError: false, error: null });
};

// Lets the fresh read's promise and everything the guard does after it run.
const settle = async (): Promise<void> => {
    await act(async () => {
        await new Promise(resolve => setTimeout(resolve, 0));
    });
};

const isHidden = (): boolean =>
    document.documentElement.style.visibility === 'hidden';

describe('RestoredPageGuard', () => {
    let reload: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        mocks.currentUser = undefined;
        mocks.refetch.mockReset();
        reload = vi.fn();
        vi.spyOn(window.location, 'reload').mockImplementation(reload);
    });

    afterEach(() => {
        vi.restoreAllMocks();
        document.documentElement.style.removeProperty('visibility');
    });

    it('should resume a restored page for the same signed-in reader', async () => {
        // given
        const { rerender } = render(<RestoredPageGuard />);
        mocks.currentUser = readerA;
        rerender(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(readerA);

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(mocks.refetch).toHaveBeenCalledTimes(1);
        expect(reload).not.toHaveBeenCalled();
        expect(isHidden()).toBe(false);
    });
});
