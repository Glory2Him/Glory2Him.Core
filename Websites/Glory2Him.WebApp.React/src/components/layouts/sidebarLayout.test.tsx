import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import SidebarLayout from './sidebarLayout';
import { AuthProvider } from '../securitys/authProvider';
import { createAuthState, signInAs } from '../../tests/testAuth';

// The admin shell's fold control. What this suite pins is what the LAYOUT owns: the menu column
// goes entirely, the content takes the whole row, the control stays put in the content because
// it never belonged to the menu, and the choice outlives a remount.
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
    /// links. Nothing of the column is left standing: no rail, no stub, no gutter.
    it('should take the whole menu column away and give the row to the content', async () => {
        // given
        const { container } = renderLayout();

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Collapse the menu' }));

        // then
        expect(container.querySelector('aside')).toBeNull();
        expect(container.querySelector('.col-lg-3')).toBeNull();
        expect(container.querySelector('.col-lg-9')).toBeNull();
        expect(container.querySelector('.col-12')).toBeInTheDocument();
    });

    /// The one thing a fold control must never do is fold itself away with what it folded —
    /// which is exactly why it lives in the content column rather than in the menu.
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

    /// AN ARIA RELATIONSHIP TO NOTHING IS WORSE THAN NONE. The menu is unmounted when folded,
    /// so naming it in aria-controls then points a screen reader at an element that is not in
    /// the document. aria-expanded carries the state in both directions regardless.
    it('should name the menu in aria-controls only while there is one', async () => {
        // given
        renderLayout();
        const shownToggle = screen.getByRole('button', { name: 'Collapse the menu' });
        const menuId = shownToggle.getAttribute('aria-controls');

        // then: shown, it names an element that is actually there
        expect(menuId).toBeTruthy();
        expect(document.getElementById(menuId as string)).toBeInTheDocument();

        // when
        await userEvent.click(shownToggle);

        // then: folded, it names nothing rather than something absent
        const foldedToggle = screen.getByRole('button', { name: 'Expand the menu' });
        expect(foldedToggle).not.toHaveAttribute('aria-controls');
        expect(foldedToggle).toHaveAttribute('aria-expanded', 'false');
    });

    /// Links that a screen reader can still reach while nobody else can see them are worse
    /// than absent, so the menu goes rather than being hidden — and comes back whole.
    it('should take the menu links away and bring them all back', async () => {
        // given: the menu carries Dashboard more than once — the area's own and the sample
        // pages' — so the count is what matters, not a single match
        renderLayout();
        const shownCount = screen.getAllByRole('link', { name: /Dashboard/ }).length;
        expect(shownCount).toBeGreaterThan(0);

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Collapse the menu' }));

        // then
        expect(screen.queryAllByRole('link', { name: /Dashboard/ })).toHaveLength(0);

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Expand the menu' }));

        // then: the whole menu, not a part of it
        expect(screen.getAllByRole('link', { name: /Dashboard/ })).toHaveLength(shownCount);
        // The exact name, not a pattern: the sample pages tree also carries a Content
        // Item Settings PANEL entry, and a loose match would find two links.
        expect(screen.getByRole('link', { name: 'Content Item Settings' }))
            .toBeInTheDocument();
    });

    /// The control belongs to the CONTENT, not to the menu it folds — which is what keeps it
    /// in one place whichever state the menu is in.
    it('should stand the control in the content column, not in the menu', async () => {
        // given
        const { container } = renderLayout();

        // then: shown, the button is outside the aside
        const aside = container.querySelector('aside') as HTMLElement;
        const shownToggle = screen.getByRole('button', { name: 'Collapse the menu' });
        expect(aside.contains(shownToggle)).toBe(false);

        // when
        await userEvent.click(shownToggle);

        // then: folded, it has not moved
        expect(screen.getByRole('button', { name: 'Expand the menu' })).toBeInTheDocument();
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
