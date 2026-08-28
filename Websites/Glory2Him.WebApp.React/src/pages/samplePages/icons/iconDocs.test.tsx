import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import emojiGroups from 'unicode-emoji-json/data-by-group.json';
import { BootstrapIconsDoc } from './bootstrapIconsDoc';
import { FontAwesomeDoc } from './fontAwesomeDoc';
import { fontAwesomeIcons } from './fontAwesomeCatalogue';
import { UnicodeEmojiDoc } from './unicodeEmojiDoc';
import {
    createAuthState,
    renderWithAuth,
    signInAs,
    signOut
} from '../../../tests/testAuth';

const authState = createAuthState();

vi.mock('../../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

// Each catalogue card is one child of a `d-flex flex-wrap gap-3` grid — counting those is how
// the tests check that nothing was left out, since the pages once capped an unfiltered grid at
// the first 150 entries and told the reader to "refine the search" for the rest.
const countCards = (container: HTMLElement): number =>
    container.querySelectorAll('.d-flex.flex-wrap.gap-3 > div').length;

const totalEmojiCount = emojiGroups.reduce((total, group) => total + group.emojis.length, 0);

const totalFontAwesomeCount = fontAwesomeIcons.reduce(
    (total, icon) => total + icon.styles.length, 0);

describe('Icon reference pages', () => {
    beforeEach(() => {
        signOut(authState);
        signInAs(authState, ['Administrators']);
    });

    describe('UnicodeEmojiDoc', () => {
        it('should render every emoji in the catalogue with no search term', () => {
            // when
            const { container } = renderWithAuth(<UnicodeEmojiDoc />);

            // then: the whole set, not a first-N slice — you cannot search for an emoji whose
            // name you do not already know
            expect(countCards(container)).toBe(totalEmojiCount);

            expect(screen.getByText(`Showing ${totalEmojiCount} of ${totalEmojiCount} emoji.`))
                .toBeInTheDocument();

            expect(screen.queryByText(/refine the search/i)).not.toBeInTheDocument();
        });

        it('should group the catalogue under its unicode.org headings', () => {
            // when
            renderWithAuth(<UnicodeEmojiDoc />);

            // then
            emojiGroups.forEach((group) =>
                expect(screen.getByRole('heading', { name: new RegExp(group.name), level: 3 }))
                    .toBeInTheDocument());
        });

        it('should narrow the grid to the matches when a search term is typed', async () => {
            // given
            const { container } = renderWithAuth(<UnicodeEmojiDoc />);
            const search = screen.getByPlaceholderText('Search emoji names, e.g. "heart"');

            // when
            await userEvent.type(search, 'yellow heart');

            // then
            await waitFor(() => expect(countCards(container)).toBe(1));
            expect(screen.getByText('yellow heart')).toBeInTheDocument();
        });

        it('should say so rather than show an empty grid when nothing matches', async () => {
            // given
            const { container } = renderWithAuth(<UnicodeEmojiDoc />);
            const search = screen.getByPlaceholderText('Search emoji names, e.g. "heart"');

            // when
            await userEvent.type(search, 'no-such-emoji');

            // then
            await waitFor(() => expect(screen.getByText('No matches.')).toBeInTheDocument());
            expect(countCards(container)).toBe(0);
        });
    });

    describe('BootstrapIconsDoc', () => {
        // Well past the 150 the page used to stop at, so a reinstated cap would fail here.
        const iconNames = Array.from({ length: 400 }, (_, index) => `icon-${index}`);

        beforeEach(() => {
            vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
                json: () => Promise.resolve(
                    Object.fromEntries(iconNames.map((name, index) => [name, index])))
            }));
        });

        afterEach(() => {
            vi.unstubAllGlobals();
        });

        it('should render every icon in the manifest with no search term', async () => {
            // when
            const { container } = renderWithAuth(<BootstrapIconsDoc />);

            // then
            await waitFor(() => expect(countCards(container)).toBe(iconNames.length));

            expect(screen.getByText(
                `Showing ${iconNames.length} of ${iconNames.length} icons.`))
                .toBeInTheDocument();

            expect(screen.queryByText(/refine the search/i)).not.toBeInTheDocument();
        });

        it('should narrow the grid to the matches when a search term is typed', async () => {
            // given
            const { container } = renderWithAuth(<BootstrapIconsDoc />);
            await waitFor(() => expect(countCards(container)).toBe(iconNames.length));
            const search = screen.getByPlaceholderText('Search icon names, e.g. "star"');

            // when
            await userEvent.type(search, 'icon-39');

            // then: icon-39, and icon-390 through icon-399
            await waitFor(() => expect(countCards(container)).toBe(11));
        });

        it('should report the failure rather than an empty catalogue when the manifest will not load', async () => {
            // given
            vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')));

            // when
            renderWithAuth(<BootstrapIconsDoc />);

            // then
            await waitFor(() => expect(
                screen.getByText('Could not load the Bootstrap Icons manifest.'))
                .toBeInTheDocument());
        });
    });

    describe('FontAwesomeDoc', () => {
        it('should render every icon of every style with no search term', () => {
            // when
            const { container } = renderWithAuth(<FontAwesomeDoc />);

            // then: an icon that ships in two styles is drawn under both, so the card count is
            // the name-and-style total rather than the number of names
            expect(countCards(container)).toBe(totalFontAwesomeCount);

            expect(screen.getByText(
                `Showing ${totalFontAwesomeCount} of ${totalFontAwesomeCount} icons.`))
                .toBeInTheDocument();

            expect(screen.queryByText(/refine the search/i)).not.toBeInTheDocument();
        });

        it('should draw each icon with the style class the shipped stylesheet defines', () => {
            // when
            const { container } = renderWithAuth(<FontAwesomeDoc />);

            // then: the bundle is Font Awesome Free 5.15.1, whose stylesheet defines fas / far /
            // fab — the v6 fa-solid / fa-regular / fa-brands spellings render nothing
            expect(container.querySelector('i.fas.fa-heart')).toBeInTheDocument();
            expect(container.querySelector('i.far.fa-heart')).toBeInTheDocument();
            expect(container.querySelector('i.fab.fa-github')).toBeInTheDocument();
            expect(container.querySelector('i.fa-solid')).not.toBeInTheDocument();
        });

        it('should narrow every style group at once when a search term is typed', async () => {
            // given
            const { container } = renderWithAuth(<FontAwesomeDoc />);
            const search = screen.getByPlaceholderText('Search icon names, e.g. "heart"');

            const expectedMatches = fontAwesomeIcons
                .filter((icon) => icon.name.includes('heart'))
                .reduce((total, icon) => total + icon.styles.length, 0);

            // when
            await userEvent.type(search, 'heart');

            // then
            await waitFor(() => expect(countCards(container)).toBe(expectedMatches));
            expect(expectedMatches).toBeLessThan(totalFontAwesomeCount);
        });
    });
});
