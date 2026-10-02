import { render, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import HeaderComponent from './header';
import { AuthContextOverride } from '../securitys/authProvider';

// The header's Logout (§UI20.8 rule 3): a logout that succeeds loads the home page afresh,
// replacing the reader's page in the tab's history; one that fails loads nothing.
type MutateOptions = { onSuccess?: () => void; onError?: (error: Error) => void };

let logoutOutcome: 'success' | 'failure';

const mutate = vi.fn((_variables?: unknown, options?: MutateOptions) => {
    if (logoutOutcome === 'success') {
        options?.onSuccess?.();
    } else {
        options?.onError?.(new Error('logout failed'));
    }
});

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useLogout: () => ({ mutate })
    }
}));

const replace = vi.fn();

const renderHeader = () =>
    render(
        <MemoryRouter initialEntries={['/Account/Manage']}>
            <AuthContextOverride userId="account-joan" displayName="joan" roles={[]}>
                <HeaderComponent />
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
        vi.stubGlobal('location', { ...window.location, replace });
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
        const { container } = renderHeader();

        // when
        await pressHeaderLogout(container);

        // then
        expect(mutate).toHaveBeenCalledTimes(1);
        expect(replace).not.toHaveBeenCalled();
    });
});
