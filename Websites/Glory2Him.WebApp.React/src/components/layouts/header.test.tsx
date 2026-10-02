import { render, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
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

const stubLocation = () => {
    const stub = {
        pathname: '/Account/Manage',
        replace,
        reload,
        assign,
        get href() {
            return 'http://localhost/Account/Manage';
        },
        set href(value: string) {
            setHref(value);
        }
    };

    vi.stubGlobal('location', stub);
};

const RouterPath = () => <span data-testid="router-path">{useLocation().pathname}</span>;

const renderHeader = () =>
    render(
        <MemoryRouter initialEntries={['/Account/Manage']}>
            <AuthContextOverride userId="account-joan" displayName="joan" roles={[]}>
                <HeaderComponent />
                <RouterPath />
            </AuthContextOverride>
        </MemoryRouter>);

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
        stubLocation();
    });

    afterEach(() => {
        vi.unstubAllGlobals();
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
        expect(getByTestId('router-path')).toHaveTextContent('/Account/Manage');
    });
});
