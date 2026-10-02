import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import UserMenuComponent from './userMenu';
import { AuthContextOverride } from '../securitys/authProvider';
import { accountService } from '../../services/foundations/accountService';

type MutateOptions = { onSuccess?: () => void; onError?: () => void };

vi.mock('../../services/foundations/accountService', () => ({
    accountService: { useLogout: vi.fn() }
}));

const renderUnderSignedInReader = () =>
    render(
        <MemoryRouter>
            <AuthContextOverride userId="user-1" displayName="Reader One" roles={[]}>
                <UserMenuComponent />
            </AuthContextOverride>
        </MemoryRouter>);

describe('UserMenuComponent logout', () => {
    const replace = vi.fn();

    beforeEach(() => {
        vi.stubGlobal('location', { ...window.location, replace });
    });

    afterEach(() => {
        vi.unstubAllGlobals();
        vi.clearAllMocks();
    });

    const logoutThat = (outcome: 'succeeds' | 'fails') =>
        vi.mocked(accountService.useLogout).mockReturnValue({
            mutate: (_variables: unknown, options?: MutateOptions) =>
                outcome === 'succeeds' ? options?.onSuccess?.() : options?.onError?.()
        } as unknown as ReturnType<typeof accountService.useLogout>);

    it("should load the home page afresh once the user menu's logout succeeds", async () => {
        logoutThat('succeeds');
        renderUnderSignedInReader();

        await userEvent.click(screen.getByRole('button', { name: /logout/i }));

        expect(replace).toHaveBeenCalledOnce();
        expect(replace).toHaveBeenCalledWith('/');
    });
});
