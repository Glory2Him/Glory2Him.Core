import { ReactElement } from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Home } from './home';
import { MyPosts } from './myPosts';
import { ContentItemModerationPage } from './admin/contentItemModerationPage';
import { AuthProvider } from '../components/securitys/authProvider';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { ApprovalStatus } from '../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../models/components/contentItems/contentItemFormItem';
import { createAuthState, signInAs, signOut } from '../tests/testAuth';

import {
    ContentItemPage
} from '../models/foundations/contentItems/contentItemSearchQuery';

import {
    ContentItemSearchCriteria
} from '../models/components/contentItems/contentItemSearchItem';

// The advanced fold-out is closed until it is asked for, so a test about what stands inside it
// opens it first — the same chevron a reader presses.
const openAdvancedSearchOptions = async () =>
    await userEvent.click(screen.getByRole('button', { name: 'Advanced search options' }));

// Three surfaces over one family, and what this suite pins is exactly what DIFFERS between them:
// which read each page feeds the panel from, and what each pins onto it. Everything the pages
// share — projection, URL round trip, paging — is posts.test.tsx's subject.
const authState = createAuthState();
let searchedOptions: Record<string, unknown> | null = null;
let pages: ContentItemPage[] = [];

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemSearchPageSize: 8,

    contentItemService: {
        useSearchContentItems: (
            criteria: ContentItemSearchCriteria,
            options: Record<string, unknown>) => {
            void criteria;
            searchedOptions = options;

            return {
                data: { pages },
                isLoading: false,
                isError: false,
                hasNextPage: false,
                isFetchingNextPage: false,
                fetchNextPage: vi.fn()
            };
        }
    }
}));

vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        useGetApprovedReactions: () => ({ data: [] })
    }
}));

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: [] }),
        useGetEffectiveSettingsFor: () => ({ data: [] })
    }
}));

const contentItemFor = (overrides: Partial<ContentItem> = {}): ContentItem => ({
    id: 'devotional-1',
    contentType: ContentType.Devotional,
    title: 'Grace for the ordinary Tuesday',
    author: 'Miriam Vale',
    content: 'Grace is not a one-time event.',
    shareabilityBasis: ShareabilityBasis.Owned,
    sharePermission: null,
    contentHash: 'hash-1',
    groupId: 'group-1',
    version: 1,
    publishDate: '2026-07-03T00:00:00Z',
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy: 'user-1',
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: 'user-1',
    updatedWhen: '2026-07-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null,
    ...overrides
});

// Where a click LANDED. The pages navigate rather than link, so the address is the only
// evidence of where a card leads — and on the admin surface it is the whole point.
const LocationProbe = () => {
    const location = useLocation();

    return <span data-testid="location">{location.pathname}</span>;
};

const landedOn = (): string | null =>
    screen.getByTestId('location').textContent;

const renderPage = (page: ReactElement, initialUrl = '/') =>
    render(
        <MemoryRouter initialEntries={[initialUrl]}>
            <AuthProvider>{page}</AuthProvider>
            <LocationProbe />
        </MemoryRouter>);

describe('The content item feed pages', () => {
    beforeEach(() => {
        searchedOptions = null;
        pages = [{ items: [contentItemFor()], pageIndex: 0, pageSize: 8, hasNextPage: false }];
        signOut(authState);
    });

    describe('Home', () => {
        // §14.1 by construction: the front page builds on the caller-INDEPENDENT read, so no
        // role change anywhere can leak a draft onto it.
        it('should feed the panel from the public read', () => {
            // when
            renderPage(<Home />);

            // then
            expect(searchedOptions).toEqual(expect.objectContaining({ scope: 'public' }));
        });

        // The button is a courtesy, never a boundary — the server re-decides against the stored
        // row — but a visitor with no account has nothing to edit, so they get no button. It
        // reads View on a feed: a listed card's pencil navigates rather than opening an editor.
        it('should offer the way in to a signed-in reader and not to a visitor', () => {
            // given / when: a visitor
            const rendered = renderPage(<Home />);

            // then
            expect(screen.queryByRole('button', { name: 'View' })).not.toBeInTheDocument();

            // given / when: signed in
            rendered.unmount();
            signInAs(authState, ['Users']);
            renderPage(<Home />);

            // then
            expect(screen.getByRole('button', { name: 'View' })).toBeInTheDocument();
        });

        // THE PUBLIC FRONT PAGE OFFERS NO STATUS TO FILTER ON. Every row it can reach is
        // approved — the caller-independent read sees to that — so a status box here would
        // be a control whose every setting says the same thing.
        it('should leave the approval statuses out of the search options', async () => {
            // when
            renderPage(<Home />);
            await openAdvancedSearchOptions();

            // then
            expect(screen.queryByRole('checkbox', { name: 'Draft' })).not.toBeInTheDocument();
        });

        it('should keep the verse of the day above the feed', () => {
            // when
            renderPage(<Home />);

            // then
            expect(screen.getByText(/Verse of the day/i)).toBeInTheDocument();

            expect(screen.getByRole('button', { name: 'Grace for the ordinary Tuesday' }))
                .toBeInTheDocument();
        });
    });

    describe('MyPosts', () => {
        it('should pin the read to the signed-in account', () => {
            // given
            signInAs(authState, ['Users']);

            // when
            renderPage(<MyPosts />, '/myposts');

            // then: user-1 is the id signInAs mints — and the WHOLE shelf by default, the
            // same four the boxes below start ticked with
            expect(searchedOptions).toEqual(
                expect.objectContaining({
                    scope: 'caller',
                    submittedById: 'user-1',
                    defaultApprovalStatuses: [
                        ApprovalStatus.Draft,
                        ApprovalStatus.Submitted,
                        ApprovalStatus.Approved,
                        ApprovalStatus.Rejected
                    ]
                }));
        });

        // The page must never ask for everybody's rows while the identity is still arriving.
        it('should hold the read while the account id has not resolved', () => {
            // when
            renderPage(<MyPosts />, '/myposts');

            // then
            expect(searchedOptions).toEqual(expect.objectContaining({ enabled: false }));
        });

        // The whole point of "my posts": the reader sees their own rows wearing their status.
        it('should show the caller their own draft wearing its ribbon', () => {
            // given
            signInAs(authState, ['Users']);

            pages = [{
                items: [contentItemFor({
                    id: 'draft-1',
                    title: 'When the answer is wait',
                    approvalStatus: ApprovalStatus.Draft,
                    isPublished: false,
                    publishDate: null
                })],
                pageIndex: 0,
                pageSize: 8,
                hasNextPage: false
            }];

            // when
            renderPage(<MyPosts />, '/myposts');

            // then: the corner ribbon and NOTHING ELSE — /myposts opts into ribbons and
            // leaves the pill off, so the card says Draft once rather than twice.
            const statusTexts = screen.getAllByText('Draft');

            expect(statusTexts).toHaveLength(1);
            expect(statusTexts[0]).toHaveClass('g2h-approval-ribbon');

            expect(document.querySelector('.g2h-approval-ribbon'))
                .toHaveAttribute('data-approval-status', 'Draft');
        });

        // A contributor's own shelf is exactly where "show me what is still a draft" is the
        // question, so the status boxes stand in the fold-out here.
        it('should offer the approval statuses in the search options', async () => {
            // given
            signInAs(authState, ['Users']);

            // when
            renderPage(<MyPosts />, '/myposts');
            await openAdvancedSearchOptions();

            // then: every box ticked, matching the read the page made
            ['Draft', 'Submitted', 'Approved', 'Rejected'].forEach((statusLabel) => {
                expect(screen.getByRole('checkbox', { name: statusLabel })).toBeChecked();
            });
        });
    });

    describe('ContentItemModerationPage', () => {
        // EVERY STATUS BY DEFAULT — a moderator's question changes by the hour, and the boxes
        // are how they ask it. The read is handed the same four the boxes start ticked with.
        it('should read every status where the moderator has chosen none', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderPage(<ContentItemModerationPage />, '/Admin/Posts');

            // then
            expect(searchedOptions).toEqual(
                expect.objectContaining({
                    scope: 'caller',
                    defaultApprovalStatuses: [
                        ApprovalStatus.Draft,
                        ApprovalStatus.Submitted,
                        ApprovalStatus.Approved,
                        ApprovalStatus.Rejected
                    ]
                }));
        });

        it('should offer every approval status ticked in the search options', async () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderPage(<ContentItemModerationPage />, '/Admin/Posts');
            await openAdvancedSearchOptions();

            // then: what the boxes say is what the read above was made with
            ['Draft', 'Submitted', 'Approved', 'Rejected'].forEach((statusLabel) => {
                expect(screen.getByRole('checkbox', { name: statusLabel })).toBeChecked();
            });
        });

        it('should render in the admin chrome with its breadcrumb', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderPage(<ContentItemModerationPage />, '/Admin/Posts');

            // then
            expect(screen.getByRole('heading', { name: 'Posts', level: 1 }))
                .toBeInTheDocument();
        });

        /// A moderator who steps into a post is still working the queue. The public route would
        /// swap the chrome out from under them and lose the filtered page they were part-way
        /// through, so every way into an item from here keeps the admin address.
        it('should keep Edit inside the admin area rather than the public post route',
            async () => {
                // given
                signInAs(authState, ['Administrators']);
                renderPage(<ContentItemModerationPage />, '/Admin/Posts');

                // when
                await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

                // then
                expect(landedOn()).toBe('/Admin/Posts/devotional-1');
                expect(landedOn()).not.toBe('/posts/devotional-1');
            });

        it('should send the card title to the same admin address as Edit', async () => {
            // given
            signInAs(authState, ['Administrators']);
            renderPage(<ContentItemModerationPage />, '/Admin/Posts');

            // when
            await userEvent.click(
                screen.getByRole('button', { name: 'Grace for the ordinary Tuesday' }));

            // then
            expect(landedOn()).toBe('/Admin/Posts/devotional-1');
        });

        /// Every row the panel renders is already a card, so framing the panel in another one
        /// is chrome inside chrome — a second border and card-body padding narrowing every row
        /// for nothing. The public feeds render this same panel bare.
        it('should render the panel bare, without a card around the cards', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            const { container } = renderPage(
                <ContentItemModerationPage />, '/Admin/Posts');

            // then
            expect(container.querySelector('.g2h-content-item-list-panel'))
                .toBeInTheDocument();

            expect(container.querySelector('.card-body > .g2h-content-item-list-panel'))
                .toBeNull();
        });
    });

    /// MODERATING IS ADMIN WORK. Every feed that offers it sends the moderator to the item's
    /// admin address — the public page is a reading surface with no moderation controls on it,
    /// so a moderator sent there arrived nowhere useful. The origin rides in state either way,
    /// which is what gives the admin page a true way back to the feed they left.
    describe('the moderate destination', () => {
        beforeEach(() => {
            signInAs(authState, ['Administrators']);
        });

        it('should send a moderator from the home feed to the admin address', async () => {
            // given
            renderPage(<Home />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Moderate' }));

            // then
            expect(landedOn()).toBe('/Admin/Posts/devotional-1');
        });

        it('should send a moderator from my posts to the admin address', async () => {
            // given
            renderPage(<MyPosts />, '/myposts');

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Moderate' }));

            // then
            expect(landedOn()).toBe('/Admin/Posts/devotional-1');
        });
    });
});
