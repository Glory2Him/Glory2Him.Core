import { onlineManager } from '@tanstack/react-query';
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

const nobody = new CurrentUser({ isAuthenticated: false });

// Each read of the current user builds a new CurrentUser, so the fresh read never answers with
// the object the page noted as it went into the cache.
const freshCopyOf = (currentUser: CurrentUser): CurrentUser =>
    new CurrentUser({ ...currentUser });

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

const isHidden = (): boolean => {
    const rootStyle = document.documentElement.style;

    return rootStyle.visibility === 'hidden'
        && rootStyle.getPropertyValue('opacity') === '0'
        && rootStyle.getPropertyPriority('opacity') === 'important';
};

const isShown = (): boolean => {
    const rootStyle = document.documentElement.style;

    return rootStyle.visibility === '' && rootStyle.getPropertyValue('opacity') === '';
};

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
        onlineManager.setOnline(true);
        document.documentElement.style.removeProperty('visibility');
        document.documentElement.style.removeProperty('opacity');
    });

    it('should resume a restored page for the same signed-in reader', async () => {
        // given
        const { rerender } = render(<RestoredPageGuard />);
        mocks.currentUser = readerA;
        rerender(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(freshCopyOf(readerA));

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(mocks.refetch).toHaveBeenCalledTimes(1);
        expect(reload).not.toHaveBeenCalled();
        expect(isShown()).toBe(true);
    });

    it('should hide a restored page and read the current user again before deciding', async () => {
        // given
        mocks.currentUser = readerA;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        mocks.refetch.mockReturnValue(new Promise(() => { }));

        // when
        dispatchPageTransition('pageshow', true);

        // then
        expect(isHidden()).toBe(true);
        expect(mocks.refetch).toHaveBeenCalledTimes(1);
    });

    // A stylesheet can show an element inside a hidden root (`visibility: visible`, even with
    // `!important`), but nothing inside the root escapes the root's opacity.
    it('should hide everything on a restored page until it resumes, even what its stylesheet shows', async () => {
        // given
        mocks.currentUser = readerA;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(freshCopyOf(readerA));
        const rootStyle = document.documentElement.style;

        // when
        dispatchPageTransition('pageshow', true);
        const opacityAtRestore = rootStyle.getPropertyValue('opacity');
        const opacityPriorityAtRestore = rootStyle.getPropertyPriority('opacity');
        await settle();

        // then
        expect(opacityAtRestore).toBe('0');
        expect(opacityPriorityAtRestore).toBe('important');
        expect(rootStyle.getPropertyValue('opacity')).toBe('');
    });

    it('should compare with the reader noted when the page was cached', async () => {
        // given
        mocks.currentUser = readerA;
        const { rerender } = render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        mocks.currentUser = readerB;
        rerender(<RestoredPageGuard />);
        answerFreshRead(readerB);

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });

    it('should reload a restored page when another reader is signed in', async () => {
        // given
        mocks.currentUser = readerA;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(readerB);

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });

    it('should reload a restored page when its reader has signed out', async () => {
        // given
        mocks.currentUser = readerA;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(nobody);

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });

    it('should reload a restored page when nobody was signed in either time', async () => {
        // given
        mocks.currentUser = nobody;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(nobody);

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });

    it('should reload a restored page whose reader was never read', async () => {
        // given
        mocks.currentUser = undefined;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(readerB);

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });

    it('should reload a restored page when a reader has signed in since it was cached', async () => {
        // given
        mocks.currentUser = nobody;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(readerB);

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });

    it('should leave an ordinary page load alone', async () => {
        // given
        mocks.currentUser = readerA;
        render(<RestoredPageGuard />);
        answerFreshRead(freshCopyOf(readerA));

        // when
        dispatchPageTransition('pageshow', false);
        const shownAtOnce = isShown();
        await settle();

        // then
        expect(shownAtOnce).toBe(true);
        expect(isShown()).toBe(true);
        expect(mocks.refetch).not.toHaveBeenCalled();
        expect(reload).not.toHaveBeenCalled();
    });

    it('should reload a restored page when the current user cannot be read', async () => {
        // given
        mocks.currentUser = readerA;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);

        // React Query keeps the last answer beside the error of a failed read.
        mocks.refetch.mockResolvedValue({
            data: readerA,
            isError: true,
            error: new Error('The current user could not be read.')
        });

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });

    it('should reload a restored page at once while the app is offline', async () => {
        // given
        mocks.currentUser = readerA;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        onlineManager.setOnline(false);

        // React Query pauses a read while it counts the browser offline, so it never settles.
        mocks.refetch.mockReturnValue(new Promise(() => { }));

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });

    it('should tell readers apart by their user id', async () => {
        // given
        const anotherReaderNamedAsA = new CurrentUser({
            isAuthenticated: true,
            userId: 'reader-c',
            userName: readerA.userName,
            displayName: readerA.displayName
        });

        mocks.currentUser = readerA;
        render(<RestoredPageGuard />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(anotherReaderNamedAsA);

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });
});
