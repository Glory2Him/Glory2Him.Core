import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import SidebarLayout from './sidebarLayout';
import { AuthProvider } from '../securitys/authProvider';
import { createAuthState, signInAs } from '../../tests/testAuth';

// The admin shell's fold control. What this suite pins is what the LAYOUT owns: the menu folds
// to its icons, the content takes back the width it was occupying, the way to unfold it
// survives, and the choice outlives a remount.
const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

const renderLayout = () =>
    render(
        <MemoryRouter initialEntries={['/Admin/Posts']}>
            <AuthProvider>
                <SidebarLayout />
            </AuthProvider>
        </MemoryRouter>);

describe('SidebarLayout', () => {
    beforeEach(() => {
        window.localStorage.clear();
        signInAs(authState, ['Administrators']);
    });

    it('should open with the menu shown', () => {
        // when
        const { container } = renderLayout();

        // then
        expect(screen.getByRole('button', { name: 'Collapse the menu' })).toBeInTheDocument();
        expect(container.querySelector('.col-lg-3')).toBeInTheDocument();
        expect(container.querySelector('.col-lg-9')).toBeInTheDocument();
    });

    /// Folding the menu that only navigates is worth nothing unless the content takes the
    /// quarter it was occupying — a folded menu beside a gutter is the same screen, minus the
    /// links.
    it('should give the content the width the menu gives up', async () => {
        // given
        const { container } = renderLayout();

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Collapse the menu' }));

        // then
        expect(container.querySelector('.col-lg-3')).toBeNull();
        expect(container.querySelector('.col-lg-9')).toBeNull();
        expect(container.querySelector('aside.col-auto')).toBeInTheDocument();
    });

    /// The one thing a fold control must never do is fold itself away with what it folded.
    it('should keep the way back when the menu is folded', async () => {
        // given
        renderLayout();

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Collapse the menu' }));

        // then
        const toggle = screen.getByRole('button', { name: 'Expand the menu' });
        expect(toggle).toBeInTheDocument();
        expect(toggle).toHaveAttribute('aria-expanded', 'false');

        // and it leads back
        await userEvent.click(toggle);

        expect(screen.getByRole('button', { name: 'Collapse the menu' }))
            .toHaveAttribute('aria-expanded', 'true');
    });

    /// FOLDING COSTS WORDS, NOT DESTINATIONS. The rail is the same menu — every leaf it had is
    /// still one click away, named by its tooltip instead of by text beside it.
    it('should keep every destination when folded, as an icon named by its tooltip',
        async () => {
            // given: the menu carries Dashboard more than once — the area's own and the sample
            // pages' — so the count is what matters, not a single match
            renderLayout();
            const shownCount = screen.getAllByRole('link', { name: /Dashboard/ }).length;
            expect(shownCount).toBeGreaterThan(0);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Collapse the menu' }));

            // then: still there, and still reachable by the same name
            const folded = screen.getAllByRole('link', { name: /Dashboard/ });
            expect(folded).toHaveLength(shownCount);

            // the title IS the tooltip, and the text beside the icon is gone
            expect(folded[0]).toHaveAttribute('title');
            expect(folded[0].textContent).toBe('');
        });

    /// A rail cannot nest — there is no room beside an icon for what hangs off it — so a group
    /// asks for the menu back rather than pretending to open in place.
    it('should give the menu back when a folded group is taken', async () => {
        // given
        renderLayout();
        await userEvent.click(screen.getByRole('button', { name: 'Collapse the menu' }));

        // when
        await userEvent.click(
            screen.getByRole('button', { name: /Components — expand the menu/ }));

        // then
        expect(screen.getByRole('button', { name: 'Collapse the menu' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: /Content Item Settings/ })).toBeInTheDocument();
    });

    /// Someone who folds the menu to read a long post is still reading it after the next
    /// navigation. A preference that reset on every mount would be worse than none.
    it('should remember the fold across a remount', async () => {
        // given
        const first = renderLayout();
        await userEvent.click(screen.getByRole('button', { name: 'Collapse the menu' }));
        first.unmount();

        // when
        renderLayout();

        // then
        expect(screen.getByRole('button', { name: 'Expand the menu' })).toBeInTheDocument();
    });

    /// A browser that refuses storage must still get the menu — the shown state is the one the
    /// toggle can be found in, so it is the safe fallback.
    it('should show the menu when the preference cannot be read', () => {
        // given
        const getItem = vi.spyOn(Storage.prototype, 'getItem')
            .mockImplementation(() => { throw new Error('blocked'); });

        // when
        renderLayout();

        // then
        expect(screen.getByRole('button', { name: 'Collapse the menu' })).toBeInTheDocument();

        getItem.mockRestore();
    });

    /// The same for writing it: the fold still works for this session even when nothing can be
    /// stored.
    it('should still fold when the preference cannot be written', async () => {
        // given
        const setItem = vi.spyOn(Storage.prototype, 'setItem')
            .mockImplementation(() => { throw new Error('blocked'); });

        renderLayout();

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Collapse the menu' }));

        // then
        expect(screen.getByRole('button', { name: 'Expand the menu' })).toBeInTheDocument();

        setItem.mockRestore();
    });
});
