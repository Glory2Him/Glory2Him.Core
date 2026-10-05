import { render, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider, useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import HeaderComponent from './header';
import { AuthContextOverride } from '../securitys/authProvider';

// The header's Logout (§UI20.8 rule 3): a logout that succeeds loads the home page afresh,
// replacing the reader's page in the tab's history; one that fails loads nothing.
type MutateOptions = {
    onSuccess?: () => void;
    onError?: (error: Error) => void;
    onSettled?: () => void;
};

let logoutOutcome: 'success' | 'failure';

const mutate = vi.fn((_variables?: unknown, options?: MutateOptions) => {
    if (logoutOutcome === 'success') {
        options?.onSuccess?.();
    } else {
        options?.onError?.(new Error('logout failed'));
    }

    options?.onSettled?.();
});

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useLogout: () => ({ mutate })
    }
}));

const replace = vi.fn();
const reload = vi.fn();
const assign = vi.fn();
const setHref = vi.fn();
const setPathname = vi.fn();
const setLocation = vi.fn();
const historyMove = vi.fn();
const openWindow = vi.fn();

const locationStub = {
    replace,
    reload,
    assign,
    get pathname() {
        return '/Account/Manage';
    },
    set pathname(value: string) {
        setPathname(value);
    },
    get href() {
        return 'http://localhost/Account/Manage';
    },
    set href(value: string) {
        setHref(value);
    }
};

// These ways of loading or leaving a page are stubbed with an observer: the location's
// replace, reload and assign, its href and pathname setters, a write to window.location or
// document.location itself, the tab's history, and window.open. Its other setters, a form
// submit and navigation.reload() are not observed.
const stubLocation = () => {
    const locationProperty = {
        configurable: true,
        get: () => locationStub,
        set: (value: unknown) => setLocation(value)
    };

    Object.defineProperty(window, 'location', locationProperty);
    Object.defineProperty(document, 'location', locationProperty);

    for (const method of ['back', 'forward', 'go', 'pushState', 'replaceState'] as const) {
        vi.spyOn(window.history, method).mockImplementation(historyMove);
    }

    vi.spyOn(window, 'open').mockImplementation(openWindow);
};

const originalWindowLocation = Object.getOwnPropertyDescriptor(window, 'location');
const originalDocumentLocation = Object.getOwnPropertyDescriptor(document, 'location');

const restoreLocation = () => {
    if (originalWindowLocation) {
        Object.defineProperty(window, 'location', originalWindowLocation);
    }

    if (originalDocumentLocation) {
        Object.defineProperty(document, 'location', originalDocumentLocation);
    } else {
        delete (document as { location?: unknown }).location;
    }
};

const RouterPath = () => <span data-testid="router-path">{useLocation().pathname}</span>;

let router: ReturnType<typeof createMemoryRouter>;

const renderHeader = () => {
    router = createMemoryRouter(
        [{
            path: '*',
            element: (
                <AuthContextOverride userId="account-joan" displayName="joan" roles={[]}>
                    <HeaderComponent />
                    <RouterPath />
                </AuthContextOverride>)
        }],
        { initialEntries: ['/Account/Manage'] });

    vi.spyOn(router, 'navigate');

    return render(<RouterProvider router={router} />);
};

const pressHeaderLogout = async (container: HTMLElement) => {
    const topBar = container.querySelector('.navbar-top') as HTMLElement;
    await userEvent.click(within(topBar).getByRole('button', { name: 'Logout' }));
};

describe('HeaderComponent', () => {
    beforeEach(() => {
        mutate.mockClear();
        replace.mockClear();
        reload.mockClear();
        assign.mockClear();
        setHref.mockClear();
        setPathname.mockClear();
        setLocation.mockClear();
        historyMove.mockClear();
        openWindow.mockClear();
        stubLocation();
    });

    afterEach(() => {
        vi.restoreAllMocks();
        restoreLocation();
    });

    it("should load the home page afresh once the header's logout succeeds", async () => {
        // given
        logoutOutcome = 'success';
        const { container } = renderHeader();

        // when
        await pressHeaderLogout(container);

        // then
        expect(mutate).toHaveBeenCalledTimes(1);
        expect(replace).toHaveBeenCalledTimes(1);
        expect(replace).toHaveBeenCalledWith('/');
    });

    it("should stay on the page when the header's logout fails", async () => {
        // given
        logoutOutcome = 'failure';
        const { container, getByTestId } = renderHeader();

        // when
        await pressHeaderLogout(container);

        // then
        expect(mutate).toHaveBeenCalledTimes(1);
        expect(replace).not.toHaveBeenCalled();
        expect(reload).not.toHaveBeenCalled();
        expect(assign).not.toHaveBeenCalled();
        expect(setHref).not.toHaveBeenCalled();
        expect(setPathname).not.toHaveBeenCalled();
        expect(setLocation).not.toHaveBeenCalled();
        expect(historyMove).not.toHaveBeenCalled();
        expect(openWindow).not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalled();
        expect(getByTestId('router-path')).toHaveTextContent('/Account/Manage');
    });
});
