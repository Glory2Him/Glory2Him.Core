import { ReactElement } from 'react';
import { onlineManager } from '@tanstack/react-query';
import { act, render, screen } from '@testing-library/react';
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

// The tests load no stylesheet, so parts that set `visibility: visible !important` on themselves
// stand for the theme's `.offcanvas.show`: one inside the app's container, and one appended to
// `document.body` outside it, as react-bootstrap's `Modal` renders. Hidden is judged by outcome on
// both: not drawn, not exposed to assistive technology, and out of reach of pointer and keyboard.
const showItself = (element: HTMLElement | null): void => {
    element?.style.setProperty('visibility', 'visible', 'important');
};

// The part that sets no `visibility` of its own sees a `visibility: hidden` left on `<html>`,
// which the parts that show themselves cannot.
const Page = (): ReactElement => (
    <>
        <RestoredPageGuard />
        <div data-testid="part-inside-the-app" ref={showItself} />
        <div data-testid="part-with-no-visibility" />
        <input aria-label="Draft title" defaultValue="A draft in progress" />
    </>
);

let partOutsideTheApp: HTMLElement;

const appendPartOutsideTheApp = (): void => {
    partOutsideTheApp = document.createElement('div');
    showItself(partOutsideTheApp);
    document.body.appendChild(partOutsideTheApp);
};

const partsThatShowThemselves = (): Array<HTMLElement> =>
    [screen.getByTestId('part-inside-the-app'), partOutsideTheApp];

const selfAndAncestorsOf = (element: Element): Array<Element> => {
    const elements: Array<Element> = [];

    for (let current: Element | null = element; current !== null; current = current.parentElement) {
        elements.push(current);
    }

    return elements;
};

const isUnderDisplayNone = (part: Element): boolean =>
    selfAndAncestorsOf(part).some(element => getComputedStyle(element).display === 'none');

const isUnderInert = (part: Element): boolean =>
    selfAndAncestorsOf(part).some(element =>
        element.hasAttribute('inert') || (element as HTMLElement).inert === true);

const isNotDrawn = (part: Element): boolean =>
    getComputedStyle(part).visibility !== 'visible'
    || isUnderDisplayNone(part)
    || selfAndAncestorsOf(part).some(element => getComputedStyle(element).opacity === '0');

const isNotExposed = (part: Element): boolean =>
    isUnderDisplayNone(part)
    || isUnderInert(part)
    || selfAndAncestorsOf(part).some(element => element.getAttribute('aria-hidden') === 'true');

const isOutOfReach = (part: Element): boolean =>
    isUnderDisplayNone(part) || isUnderInert(part);

const isHidden = (): boolean =>
    partsThatShowThemselves().every(part =>
        isNotDrawn(part) && isNotExposed(part) && isOutOfReach(part));

const isShown = (): boolean => {
    const visibilityOfAPartWithNone =
        getComputedStyle(screen.getByTestId('part-with-no-visibility')).visibility;

    return partsThatShowThemselves().every(part =>
        !isNotDrawn(part) && !isNotExposed(part) && !isOutOfReach(part))
        && visibilityOfAPartWithNone !== 'hidden'
        && visibilityOfAPartWithNone !== 'collapse';
};

// happy-dom scrolls and focuses under a hidden root, and never scrolls on focus. A browser takes
// the page out of the render while any part of the hiding remains: both its scroll offsets read as
// the top left from the moment the page is hidden until something sets them once the page is
// shown, the focus moves to `body`, and nothing under it is scrolled or focused meanwhile. Once
// the page is shown, `focus()` scrolls a field that is out of view to its top unless asked not to.
const topOfTheField = 40;
let scrollPosition = 0;
let horizontalScrollPosition = 0;
let isFollowingTheHiding = false;

const anyPartOfTheHidingRemains = (): boolean =>
    [document.documentElement, document.body].some(element => {
        const style = getComputedStyle(element);

        return style.display === 'none'
            || style.visibility === 'hidden'
            || style.visibility === 'collapse'
            || style.opacity === '0'
            || element.hasAttribute('inert');
    });

const followTheHiding = (): void => {
    if (isFollowingTheHiding) {
        return;
    }

    isFollowingTheHiding = true;

    try {
        if (anyPartOfTheHidingRemains()) {
            scrollPosition = 0;
            horizontalScrollPosition = 0;

            if (document.activeElement instanceof HTMLElement
                && document.activeElement !== document.body) {
                document.activeElement.blur();
            }
        }
    } finally {
        isFollowingTheHiding = false;
    }
};

const scrollPageTo = (left: number, top: number): void => {
    followTheHiding();

    if (!anyPartOfTheHidingRemains()) {
        horizontalScrollPosition = left;
        scrollPosition = top;
    }
};

const scrollPageAsAskedBy = (xOrOptions?: ScrollToOptions | number, y?: number): void =>
    typeof xOrOptions === 'object'
        ? scrollPageTo(xOrOptions.left ?? horizontalScrollPosition, xOrOptions.top ?? scrollPosition)
        : scrollPageTo(xOrOptions ?? horizontalScrollPosition, y ?? scrollPosition);

const scrollPageByAsAskedBy = (xOrOptions?: ScrollToOptions | number, y?: number): void =>
    typeof xOrOptions === 'object'
        ? scrollPageTo(
            horizontalScrollPosition + (xOrOptions.left ?? 0),
            scrollPosition + (xOrOptions.top ?? 0))
        : scrollPageTo(horizontalScrollPosition + (xOrOptions ?? 0), scrollPosition + (y ?? 0));

type Prototype = Record<string, (...args: Array<unknown>) => unknown>;

// Every way the page can be hidden passes through one of these, so the page is taken out of the
// render in the same task it is hidden in.
const followTheHidingAfterEachCallTo = (prototype: object, method: string): void => {
    const callAsHappyDomDoes = (prototype as Prototype)[method];

    vi.spyOn(prototype as Prototype, method).mockImplementation(function (this: unknown, ...args) {
        const result = callAsHappyDomDoes.apply(this, args);
        followTheHiding();

        return result;
    });
};

const scrollPositionProperty: PropertyDescriptor = {
    configurable: true,
    get: () => {
        followTheHiding();

        return scrollPosition;
    },
    set: (top: number) => scrollPageTo(horizontalScrollPosition, top)
};

const horizontalScrollPositionProperty: PropertyDescriptor = {
    configurable: true,
    get: () => {
        followTheHiding();

        return horizontalScrollPosition;
    },
    set: (left: number) => scrollPageTo(left, scrollPosition)
};

// Each property replaced on an object is put back as that object had it, its own or none, so no
// test after the model reads what the model left behind.
const replacedProperties: Array<[object, string, PropertyDescriptor | undefined]> = [];

const replaceProperty = (target: object, name: string, descriptor: PropertyDescriptor): void => {
    replacedProperties.push([target, name, Object.getOwnPropertyDescriptor(target, name)]);
    Object.defineProperty(target, name, descriptor);
};

const modelHowABrowserScrollsAndFocuses = (): void => {
    scrollPosition = 0;
    horizontalScrollPosition = 0;
    followTheHidingAfterEachCallTo(CSSStyleDeclaration.prototype, 'setProperty');
    followTheHidingAfterEachCallTo(Element.prototype, 'setAttribute');
    followTheHidingAfterEachCallTo(Element.prototype, 'setAttributeNS');
    followTheHidingAfterEachCallTo(Element.prototype, 'toggleAttribute');
    replaceProperty(window, 'scrollY', scrollPositionProperty);
    replaceProperty(window, 'pageYOffset', scrollPositionProperty);
    replaceProperty(document.documentElement, 'scrollTop', scrollPositionProperty);
    replaceProperty(document.body, 'scrollTop', scrollPositionProperty);
    replaceProperty(window, 'scrollX', horizontalScrollPositionProperty);
    replaceProperty(window, 'pageXOffset', horizontalScrollPositionProperty);
    replaceProperty(document.documentElement, 'scrollLeft', horizontalScrollPositionProperty);
    replaceProperty(document.body, 'scrollLeft', horizontalScrollPositionProperty);
    vi.spyOn(window, 'scrollTo').mockImplementation(scrollPageAsAskedBy);
    vi.spyOn(window, 'scroll').mockImplementation(scrollPageAsAskedBy);
    vi.spyOn(window, 'scrollBy').mockImplementation(scrollPageByAsAskedBy);

    const focusAsHappyDomDoes = HTMLElement.prototype.focus;

    vi.spyOn(HTMLElement.prototype, 'focus').mockImplementation(function (this: HTMLElement, options) {
        followTheHiding();

        if (anyPartOfTheHidingRemains()) {
            return;
        }

        focusAsHappyDomDoes.call(this, options);

        if (options?.preventScroll !== true && scrollPosition > topOfTheField) {
            scrollPosition = topOfTheField;
        }
    });
};

const stopModellingABrowser = (): void => {
    for (const [target, name, original] of replacedProperties.splice(0).reverse()) {
        if (original === undefined) {
            delete (target as Record<string, unknown>)[name];
        } else {
            Object.defineProperty(target, name, original);
        }
    }
};

const fieldBeingTypedIn = (): HTMLInputElement =>
    screen.getByRole('textbox', { name: 'Draft title' });

type PlaceOnThePage = {
    scrollPosition: number;
    horizontalScrollPosition: number;
    focusedElement: Element | null;
    caret: [number | null, number | null];
};

// Steps through the resume one microtask at a time and notes where the page is at the first step
// that finds it shown, so nothing the guard does after showing it counts.
const restoreAndNoteThePlaceAsFirstShown = async (): Promise<PlaceOnThePage | undefined> => {
    dispatchPageTransition('pageshow', true);

    for (let step = 0; step < 100; step += 1) {
        if (isShown()) {
            const field = fieldBeingTypedIn();

            return {
                scrollPosition: window.scrollY,
                horizontalScrollPosition: window.scrollX,
                focusedElement: document.activeElement,
                caret: [field.selectionStart, field.selectionEnd]
            };
        }

        await Promise.resolve();
    }

    return undefined;
};

describe('RestoredPageGuard', () => {
    let reload: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        mocks.currentUser = undefined;
        mocks.refetch.mockReset();
        reload = vi.fn();
        vi.spyOn(window.location, 'reload').mockImplementation(reload);
        appendPartOutsideTheApp();
    });

    afterEach(() => {
        vi.restoreAllMocks();
        onlineManager.setOnline(true);
        partOutsideTheApp.remove();

        // A page left hidden must not leak into the next test, whatever the guard hid it by.
        for (const element of [document.documentElement, document.body]) {
            element.removeAttribute('style');
            element.removeAttribute('inert');
            element.removeAttribute('aria-hidden');
        }
    });

    it('should resume a restored page for the same signed-in reader', async () => {
        // given
        const { rerender } = render(<Page />);
        mocks.currentUser = readerA;
        rerender(<Page />);
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
        render(<Page />);

        // A page goes into the cache hidden, so this one reaches the restore unhidden, and only the
        // restore's own hide can hide it.
        mocks.refetch.mockReturnValue(new Promise(() => { }));

        // when
        dispatchPageTransition('pageshow', true);

        // then
        expect(isHidden()).toBe(true);
        expect(mocks.refetch).toHaveBeenCalledTimes(1);
    });

    it('should compare with the reader noted when the page was cached', async () => {
        // given
        mocks.currentUser = readerA;
        const { rerender } = render(<Page />);
        dispatchPageTransition('pagehide', true);
        mocks.currentUser = readerB;
        rerender(<Page />);
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
        render(<Page />);
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
        render(<Page />);
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
        render(<Page />);
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
        render(<Page />);
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
        render(<Page />);
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
        render(<Page />);
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
        render(<Page />);
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
        render(<Page />);
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
        render(<Page />);
        dispatchPageTransition('pagehide', true);
        answerFreshRead(anotherReaderNamedAsA);

        // when
        dispatchPageTransition('pageshow', true);
        await settle();

        // then
        expect(reload).toHaveBeenCalledTimes(1);
        expect(isHidden()).toBe(true);
    });

    it('should hide a page by display none as it goes into the cache', () => {
        // given
        mocks.currentUser = readerA;
        render(<Page />);

        // when
        dispatchPageTransition('pagehide', true);

        // then
        expect(isHidden()).toBe(true);
        expect(getComputedStyle(document.documentElement).display).toBe('none');
    });

    it('should hide a page as it goes into the cache when nobody is signed in', () => {
        // given
        mocks.currentUser = nobody;
        render(<Page />);

        // when
        dispatchPageTransition('pagehide', true);

        // then
        expect(isHidden()).toBe(true);
        expect(getComputedStyle(document.documentElement).display).toBe('none');
    });

    it('should hide a page as it goes into the cache while the current user is being read', () => {
        // given
        mocks.currentUser = undefined;
        render(<Page />);

        // when
        dispatchPageTransition('pagehide', true);

        // then
        expect(isHidden()).toBe(true);
        expect(getComputedStyle(document.documentElement).display).toBe('none');
    });

    describe('in a browser that takes a hidden page out of the render', () => {
        beforeEach(() => {
            modelHowABrowserScrollsAndFocuses();
        });

        afterEach(() => {
            stopModellingABrowser();
        });

        it('should resume a restored page at the scroll position it had', async () => {
            // given
            mocks.currentUser = readerA;
            render(<Page />);
            window.scrollTo(0, 600);
            dispatchPageTransition('pagehide', true);
            answerFreshRead(freshCopyOf(readerA));

            // when
            const placeAsFirstShown = await restoreAndNoteThePlaceAsFirstShown();

            // then
            expect(placeAsFirstShown?.scrollPosition).toBe(600);
        });

        it('should resume a restored page with the focus where it was', async () => {
            // given
            mocks.currentUser = readerA;
            render(<Page />);
            const field = fieldBeingTypedIn();
            field.focus();
            field.setSelectionRange(5, 5);
            dispatchPageTransition('pagehide', true);
            answerFreshRead(freshCopyOf(readerA));

            // when
            const placeAsFirstShown = await restoreAndNoteThePlaceAsFirstShown();

            // then
            expect(placeAsFirstShown?.focusedElement).toBe(field);
            expect(placeAsFirstShown?.caret).toEqual([5, 5]);
        });

        it('should put the focus back without scrolling the page to the field', async () => {
            // given
            mocks.currentUser = readerA;
            render(<Page />);
            const field = fieldBeingTypedIn();
            field.focus();
            window.scrollTo(0, 600);
            dispatchPageTransition('pagehide', true);
            answerFreshRead(freshCopyOf(readerA));

            // when
            const placeAsFirstShown = await restoreAndNoteThePlaceAsFirstShown();

            // then
            expect(placeAsFirstShown?.focusedElement).toBe(field);
            expect(placeAsFirstShown?.scrollPosition).toBe(600);
        });

        it('should resume a restored page at the horizontal scroll position it had', async () => {
            // given
            mocks.currentUser = readerA;
            render(<Page />);
            window.scrollTo(300, 600);
            dispatchPageTransition('pagehide', true);
            answerFreshRead(freshCopyOf(readerA));

            // when
            const placeAsFirstShown = await restoreAndNoteThePlaceAsFirstShown();

            // then
            expect(placeAsFirstShown?.horizontalScrollPosition).toBe(300);
        });
    });
});
