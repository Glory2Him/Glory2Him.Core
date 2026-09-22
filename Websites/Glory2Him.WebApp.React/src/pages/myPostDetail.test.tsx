import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MyPostDetail } from './myPostDetail';
import { AuthProvider } from '../components/securitys/authProvider';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { ApprovalStatus } from '../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../models/components/contentItems/contentItemFormItem';
import { createAuthState, signInAs } from '../tests/testAuth';
import { testContentItemSetting } from '../tests/testContentItemSettings';

import {
    ContentItemSetting
} from '../models/foundations/contentItemSettings/contentItemSetting';

// The contributor's own detail surface: the way back to their list, the item on the left, and
// the association surfaces beside it. The reads are mocked at their own boundary; what this
// suite pins is the LAYOUT contract the page owns — the back button, the 7/5 split, and the
// three right-column panels.
const authState = createAuthState();
let contentItem: ContentItem | undefined;

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

const modifiedWith = vi.fn();

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemService: {
        useGetContentItemById: () => ({
            data: contentItem,
            isLoading: false,
            isError: false
        }),

        useModifyContentItem: () => ({
            mutateAsync: modifiedWith,
            isPending: false
        })
    }
}));

// A quote carries no title, so the editor shapes itself without one — and with a setting at
// all, which an empty list would not give it.
const quoteSetting =
    testContentItemSetting(ContentType.Quote, 'Quote', { hasTitle: false });

// What the type's effective row says, which a test about the reaction gate rewrites - the
// setting is the only thing that decides whether the card offers a Like (§DOM6.5).
let effectiveSettings: ContentItemSetting[] = [quoteSetting];

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: effectiveSettings }),
        useGetEffectiveSettingsFor: () => ({ data: effectiveSettings })
    }
}));

// The reaction vocabulary behind the Like control, the same read the list at /myposts makes.
vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        useGetApprovedReactions: () => ({
            data: [
                {
                    id: 'reaction-1',
                    name: 'Amen',
                    unicodeEmoji: '👍',
                    isPublished: true,
                    approvalStatus: 2,
                    isDeleted: false
                },
                {
                    // WHICH ONE IS LOVE is a case-insensitive match on the NAME - the rows
                    // carry no flag of their own - so the fixture has to be named for it.
                    id: 'reaction-2',
                    name: 'Love',
                    unicodeEmoji: '❤️',
                    isPublished: true,
                    approvalStatus: 2,
                    isDeleted: false
                }
            ]
        })
    }
}));

const draftQuote: ContentItem = {
    id: 'quote-1',
    contentType: ContentType.Quote,
    title: null,
    author: 'D. L. Moody',
    content: 'Character is what you are in the dark.',
    shareabilityBasis: ShareabilityBasis.PublicDomain,
    sharePermission: null,
    contentHash: 'hash-1',
    groupId: 'group-1',
    version: 1,
    publishDate: null,
    isPublished: false,
    approvalStatus: ApprovalStatus.Draft,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy: 'user-1',
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: 'user-1',
    updatedWhen: '2026-07-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null
};

const renderPage = (initialEntry: Parameters<typeof MemoryRouter>[0]['initialEntries'] extends
    ReadonlyArray<infer T> | undefined ? T : never = '/myposts/quote-1') =>
    render(
        <MemoryRouter initialEntries={[initialEntry]}>
            <AuthProvider>
                <MyPostDetail />
            </AuthProvider>
        </MemoryRouter>);

describe('MyPostDetail', () => {
    beforeEach(() => {
        contentItem = draftQuote;
        modifiedWith.mockReset();
        modifiedWith.mockResolvedValue(undefined);
        effectiveSettings = [quoteSetting];
        signInAs(authState, ['Users']);
    });

    it('should offer the way back to my posts', () => {
        // when
        renderPage();

        // then
        expect(screen.getByRole('link', { name: /Back to my posts/ }))
            .toHaveAttribute('href', '/myposts');
    });

    // A redirect that carried its origin gets a TRUE back — a filtered list returns filtered.
    it('should walk back to the origin a redirect carried in state', () => {
        // when
        render(
            <MemoryRouter
                initialEntries={[{
                    pathname: '/myposts/quote-1',
                    state: { from: '/myposts?type=Quote' }
                }]}>
                <AuthProvider>
                    <MyPostDetail />
                </AuthProvider>
            </MemoryRouter>);

        // then
        expect(screen.getByRole('link', { name: /Back to my posts/ }))
            .toHaveAttribute('href', '/myposts?type=Quote');
    });

    it('should stand the item in the seven beside a five', () => {
        // when
        const { container } = renderPage();

        // then: the layout contract itself — 7 for the item, 5 for what sits beside it
        expect(container.querySelector('.col-lg-7 h1')).toBeInTheDocument();
        expect(container.querySelector('.col-lg-5')).toBeInTheDocument();
    });

    it('should stand the association and sharing surfaces in the five', () => {
        // when
        const { container } = renderPage();
        const rightColumn = container.querySelector('.col-lg-5') as HTMLElement;

        // then
        expect(rightColumn.textContent).toContain('Tags');
        expect(rightColumn.textContent).toContain('Bible references');
        expect(rightColumn.textContent).toContain('Have something to share?');
    });

    it('should render the item itself on the left', () => {
        // when
        renderPage();

        // then: a quote leads with its content — the hero face may append the author, so
        // the match is a fragment rather than the whole line
        expect(screen.getByText(/Character is what you are in the dark\./))
            .toBeInTheDocument();
    });

    /// THE SAVE IS REAL, and it replaced a local merge that could only ever carry the fields
    /// somebody remembered to list. That list held the content fields and not approvalStatus,
    /// so a contributor offering a draft for review watched the card go on saying Draft. The
    /// write now goes to the server and the row is re-read, status and all.
    // THE LIKE CONTROL, on the contributor's own detail surface. /myposts already offers it on
    // the card for this very item, so one item read two ways answered two different things.
    it("should offer the like control on my own post's detail page", () => {
        // when
        renderPage();

        // then
        expect(screen.getByRole('button', { name: /Like/ })).toBeInTheDocument();
    });

    // THE WIRING DOES NOT OVERRIDE THE GATE - it makes the gate the only thing deciding. A
    // type whose setting refuses reactions offers nothing here, exactly as it does everywhere
    // else the rule is asked (contentItemPanel.tsx).
    it('should show no like control on the newly wired pages for a type whose setting '
        + 'refuses reactions', () => {
        // given
        effectiveSettings = [{ ...quoteSetting, reactionsAllowed: false }];

        // when
        renderPage();

        // then
        expect(screen.queryByRole('button', { name: /Like/ })).not.toBeInTheDocument();
    });

    // LIMITREACTIONSTOLOVEONLY NARROWS THE PICKER to the one option (§DOM6.5). No seeded type
    // carries it, so the fixture is constructed rather than found.
    it('should offer only the love option on a love-only type on the newly wired pages',
        async () => {
            // given
            effectiveSettings = [{ ...quoteSetting, limitReactionsToLoveOnly: true }];
            renderPage();

            // when
            await userEvent.click(screen.getByRole('button', { name: /Like/ }));

            // then
            const offered = screen.getAllByRole('menuitem');

            expect(offered).toHaveLength(1);
            expect(offered[0]).toHaveAccessibleName('Love');
        });

    // ONLY THE LIKE CONTROL. Taking the engagement hook for its reaction members does not wire
    // its Share and Save members, and the card keeps both off the row because no handler was
    // passed. Share copies the item's /posts/{id} address, which answers nothing for a Draft or
    // for an item still under moderation; Save is a different entity and a different outcome.
    it('should add only the like control to the newly wired pages', () => {
        // when
        const { container } = renderPage();
        const card = container.querySelector('.g2h-content-item-card') as HTMLElement;

        // then
        expect(within(card).getByRole('button', { name: /Like/ })).toBeInTheDocument();
        expect(within(card).queryByRole('button', { name: /Share/ })).not.toBeInTheDocument();
        expect(within(card).queryByRole('button', { name: /Save/ })).not.toBeInTheDocument();
    });

    it('should send the whole row with the amendment over it', async () => {
        // given
        renderPage();
        await userEvent.click(screen.getByRole('button', { name: /Edit/ }));

        const contentBox =
            screen.getByDisplayValue('Character is what you are in the dark.');

        await userEvent.clear(contentBox);
        await userEvent.type(contentBox, 'Character is what you are in the dark, always.');

        // when
        await userEvent.click(screen.getByRole('button', { name: /Save/ }));

        // then
        expect(modifiedWith).toHaveBeenCalledTimes(1);

        expect(modifiedWith).toHaveBeenCalledWith(expect.objectContaining({
            content: 'Character is what you are in the dark, always.',
            id: 'quote-1',
            groupId: 'group-1',
            createdBy: 'user-1'
        }));
    });
});
