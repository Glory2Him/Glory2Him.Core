import { ReactElement, useState } from 'react';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Home } from './home';
import { MyPosts } from './myPosts';
import { Posts } from './posts';
import { ContentItemModerationPage } from './admin/contentItemModerationPage';
import { AuthProvider } from '../components/securitys/authProvider';
import { EntityType } from '../models/foundations/approvalSettings/approvalSetting';
import { AssociationRequest } from '../models/foundations/associations/associationRequest';
import { ContentItemReactionSummary } from '../models/foundations/associations/contentItemReactionSummary';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { Reaction } from '../models/foundations/reactions/reaction';
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
let hasNextPage = false;

// The pages the read holds but has not delivered yet: each next page request delivers the first
// of them.
let undeliveredPages: ContentItemPage[] = [];

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
            const [, setDeliveries] = useState(0);
            void criteria;
            searchedOptions = options;

            return {
                data: { pages },
                isLoading: false,
                isError: false,
                hasNextPage,
                isFetchingNextPage: false,
                fetchNextPage: () => {
                    if (undeliveredPages.length > 0) {
                        pages = [...pages, undeliveredPages[0]];
                        undeliveredPages = undeliveredPages.slice(1);
                        hasNextPage = undeliveredPages.length > 0;
                        setDeliveries((count) => count + 1);
                    }
                }
            };
        }
    }
}));

const reactionFor = (name: string, unicodeEmoji: string): Reaction => ({
    id: `reaction-${name.toLowerCase()}`,
    name,
    unicodeEmoji,
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isDeleted: false
});

const vocabulary: ReadonlyArray<Reaction> = [
    reactionFor('Amen', '👍'),
    reactionFor('Love', '❤️'),
    reactionFor('Joy', '😊')
];

const reactionNamed = (name: string): Reaction =>
    vocabulary.find((reaction) => reaction.name === name)!;

vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        useGetApprovedReactions: () => ({ data: vocabulary })
    }
}));

// The engagement hook reads the cards' reaction summaries through a query, which a harness with
// no QueryClientProvider cannot hold, so it is mocked. It answers only for the ids it is handed,
// as the real one does, so a card whose page the hook was never handed carries no counts. Its
// writes are mutations, mocked for the same reason, and a write made through them stays pending
// for the length of the test, so a chosen reaction's overlay stands.
let handedPages: ReadonlyArray<ReadonlyArray<string>> | undefined;
let serverSummaries: Record<string, ContentItemReactionSummary> = {};
const upsertAssociation = vi.fn<(association: AssociationRequest) => Promise<unknown>>();
const removeAssociationByPair = vi.fn<(association: AssociationRequest) => Promise<unknown>>();

vi.mock('../services/foundations/associationService', () => ({
    associationService: {
        useGetReactionSummaries: (contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>) => {
            handedPages = contentItemIdPages;

            return {
                summaries: Object.fromEntries(contentItemIdPages
                    .flat()
                    .filter((contentItemId) => serverSummaries[contentItemId] !== undefined)
                    .map((contentItemId) => [contentItemId, serverSummaries[contentItemId]])),
                isLoading: false,
                isError: false
            };
        },

        useUpsertAssociation: () => ({ mutateAsync: upsertAssociation }),
        useRemoveAssociationByPair: () => ({ mutateAsync: removeAssociationByPair }),
        useReadReactionSummariesAgain: () => () => new Promise(() => undefined)
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

// One of the reader's own posts, approved and published, so publicly visible: the signed-in
// account is always user-1, the fixture's contributor.
const myPostFor = (id: string): ContentItem =>
    contentItemFor({ id, title: `My post ${id}` });

const pageOf = (pageIndex: number, ids: ReadonlyArray<string>): ContentItemPage => ({
    items: ids.map(myPostFor),
    pageIndex,
    pageSize: 8,
    hasNextPage: false
});

// One item's summary as the server sends it: each reaction given and its count, and the one
// the reader holds, if any.
const summaryOf = (
    contentItemId: string,
    counts: ReadonlyArray<[string, number]>,
    viewerReactionName: string | null = null): ContentItemReactionSummary => ({
    contentItemId,
    reactions: counts.map(([name, count]) => ({
        reactionId: reactionNamed(name).id,
        name,
        unicodeEmoji: reactionNamed(name).unicodeEmoji,
        count
    })),
    viewerReactionId: viewerReactionName === null ? null : reactionNamed(viewerReactionName).id,
    viewerReactionName
});

const cardFor = (contentItemId: string): HTMLElement =>
    screen.getAllByRole('article').find((card) =>
        within(card).queryByText(`My post ${contentItemId}`) !== null)!;

const reactionCountsOn = (contentItemId: string): HTMLElement =>
    within(cardFor(contentItemId)).getByRole('button', { name: 'Reaction counts' });

// One reaction's own count on a card, read from the counts' expanded face, which lists each
// reaction given beside its glyph; the collapsed face shows only their sum.
const reactionCountOn = async (contentItemId: string, reactionName: string): Promise<string> => {
    const reactionCounts = reactionCountsOn(contentItemId);

    if (reactionCounts.getAttribute('aria-expanded') !== 'true') {
        await userEvent.click(reactionCounts);
    }

    const reaction = within(reactionCountsOn(contentItemId)).getByTitle(reactionName);

    return (reaction.textContent ?? '').replace(reactionNamed(reactionName).unicodeEmoji, '').trim();
};

// Opens a card's Like control: the reactions it offers, each pressed or not.
const openLikeOn = async (contentItemId: string): Promise<void> =>
    await userEvent.click(within(cardFor(contentItemId)).getByRole('button', { name: /Like/ }));

const chooseOn = async (contentItemId: string, reactionName: string): Promise<void> => {
    await openLikeOn(contentItemId);
    await userEvent.click(within(cardFor(contentItemId)).getByRole('menuitem', { name: reactionName }));
};

const reactionPairFor = (contentItemId: string, reactionName: string): AssociationRequest => ({
    entityAType: EntityType.ContentItem,
    entityAKeyId: contentItemId,
    entityBType: EntityType.Reaction,
    entityBKeyId: reactionNamed(reactionName).id
});

describe('The content item feed pages', () => {
    beforeEach(() => {
        searchedOptions = null;
        pages = [{ items: [contentItemFor()], pageIndex: 0, pageSize: 8, hasNextPage: false }];
        hasNextPage = false;
        undeliveredPages = [];
        handedPages = undefined;
        serverSummaries = {};
        upsertAssociation.mockReset();
        upsertAssociation.mockImplementation(() => new Promise(() => undefined));
        removeAssociationByPair.mockReset();
        removeAssociationByPair.mockImplementation(() => new Promise(() => undefined));
        signOut(authState);
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    describe('Home', () => {
        // THE FRONT PAGE'S DEFAULT LISTING IS THE FEED (§DOM11.3): effective publication order,
        // with topics and series excluded. This replaces the assertion that the page always read
        // /Public — it now reads the public route only when the reader has actually narrowed
        // something, because the feed route takes no $filter at all.
        //
        // Both reads are caller-INDEPENDENT (§14.1), so neither can be widened by anybody's
        // roles; which of the two answers is the page's decision, not the reader's identity.
        // (homePageReadsTheFeedWhenNoCriteriaAreSupplied)
        it('should feed the panel from the feed read when no criteria are supplied', () => {
            // when
            renderPage(<Home />);

            // then
            expect(searchedOptions).toEqual(expect.objectContaining({ scope: 'feed' }));
        });

        // THE SIX CRITERIA THAT PUT A $filter ON THE WIRE, each on its own. The moment one is
        // set the page goes back to the public read exactly as it behaves today — complete with
        // its topics, because filtering them out of a NARROWING read would make every topic
        // unsearchable.
        // (homePageReadsThePublicReadWhenACriterionIsSupplied)
        it.each([
            ['free text', '/?q=grace'],
            ['a content type', '/?type=Devotional'],
            ['an author', '/?author=Temple'],
            ['a submitter', '/?by=user-9'],
            ['a shareability basis', '/?shareability=PublicDomain'],
            ['chosen approval statuses', '/?status=Approved']
        ])('should feed the panel from the public read when the reader supplied %s',
            (_criterion, initialUrl) => {
                // when
                renderPage(<Home />, initialUrl);

                // then
                expect(searchedOptions).toEqual(expect.objectContaining({ scope: 'public' }));
            });

        // TAGS AND BIBLE REFERENCES NARROW NOTHING YET — no read is filtered on them until
        // #318 — so treating them as criteria would cost the feed's order and re-admit topics
        // in exchange for no narrowing at all.
        it.each([
            ['a tag', '/?tags=grace'],
            ['a Bible reference', '/?refs=John+3:16']
        ])('should stay on the feed when the reader supplied only %s',
            (_criterion, initialUrl) => {
                // when
                renderPage(<Home />, initialUrl);

                // then
                expect(searchedOptions).toEqual(expect.objectContaining({ scope: 'feed' }));
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

        describe('the Like control', () => {
            it("should show each card's reaction counts on /myposts", () => {
                // given
                signInAs(authState, ['Users']);
                pages = [pageOf(0, ['post-1', 'post-2'])];

                serverSummaries = {
                    'post-1': summaryOf('post-1', [['Love', 3], ['Amen', 2]]),
                    'post-2': summaryOf('post-2', [['Joy', 7]])
                };

                // when
                renderPage(<MyPosts />, '/myposts');

                // then
                expect(handedPages).toEqual([['post-1', 'post-2']]);
                expect(reactionCountsOn('post-1')).toHaveTextContent('5');
                expect(reactionCountsOn('post-2')).toHaveTextContent('7');
            });

            // EACH PAGE AS IT WAS DELIVERED: asking for the third page's ids alone is the
            // summaries read's, one query per page it is handed, so the page hands every page
            // it holds and the first two pages' cards keep their counts.
            it('should hand the engagement hook every page delivered on /myposts', async () => {
                // given
                signInAs(authState, ['Users']);
                pages = [pageOf(0, ['post-1', 'post-2']), pageOf(1, ['post-3'])];
                undeliveredPages = [pageOf(2, ['post-4', 'post-5'])];
                hasNextPage = true;

                // Without an IntersectionObserver the list offers Load more, a press the test
                // can make.
                vi.stubGlobal('IntersectionObserver', undefined);

                serverSummaries = {
                    'post-1': summaryOf('post-1', [['Love', 1]]),
                    'post-2': summaryOf('post-2', [['Amen', 2]]),
                    'post-3': summaryOf('post-3', [['Joy', 3]]),
                    'post-4': summaryOf('post-4', [['Love', 4]]),
                    'post-5': summaryOf('post-5', [['Amen', 5], ['Joy', 1]])
                };

                renderPage(<MyPosts />, '/myposts');

                // when
                await userEvent.click(screen.getByRole('button', { name: 'Load more' }));

                // then
                expect(handedPages).toEqual([
                    ['post-1', 'post-2'],
                    ['post-3'],
                    ['post-4', 'post-5']
                ]);

                expect(reactionCountsOn('post-1')).toHaveTextContent('1');
                expect(reactionCountsOn('post-2')).toHaveTextContent('2');
                expect(reactionCountsOn('post-3')).toHaveTextContent('3');
                expect(reactionCountsOn('post-4')).toHaveTextContent('4');
                expect(reactionCountsOn('post-5')).toHaveTextContent('6');
            });

            it("should mark the reader's own reaction on /myposts", async () => {
                // given
                signInAs(authState, ['Users']);
                pages = [pageOf(0, ['post-1'])];

                serverSummaries = {
                    'post-1': summaryOf('post-1', [['Love', 3], ['Amen', 2]], 'Love')
                };

                renderPage(<MyPosts />, '/myposts');

                // when
                await openLikeOn('post-1');

                // then
                expect(within(cardFor('post-1')).getByRole('menuitem', { name: 'Love' }))
                    .toHaveAttribute('aria-pressed', 'true');

                expect(within(cardFor('post-1')).getByRole('menuitem', { name: 'Amen' }))
                    .toHaveAttribute('aria-pressed', 'false');
            });

            it('should record a chosen reaction on /myposts', async () => {
                // given
                signInAs(authState, ['Users']);
                pages = [pageOf(0, ['post-1'])];

                serverSummaries = {
                    'post-1': summaryOf('post-1', [['Love', 3], ['Amen', 2]])
                };

                renderPage(<MyPosts />, '/myposts');

                // when
                await chooseOn('post-1', 'Love');

                // then
                expect(upsertAssociation).toHaveBeenCalledTimes(1);
                expect(upsertAssociation).toHaveBeenCalledWith(reactionPairFor('post-1', 'Love'));
                expect(removeAssociationByPair).not.toHaveBeenCalled();
                expect(await reactionCountOn('post-1', 'Love')).toBe('4');
                expect(await reactionCountOn('post-1', 'Amen')).toBe('2');

                await openLikeOn('post-1');

                expect(within(cardFor('post-1')).getByRole('menuitem', { name: 'Love' }))
                    .toHaveAttribute('aria-pressed', 'true');
            });

            it('should withdraw a reaction chosen again on /myposts', async () => {
                // given
                signInAs(authState, ['Users']);
                pages = [pageOf(0, ['post-1'])];

                serverSummaries = {
                    'post-1': summaryOf('post-1', [['Love', 3], ['Amen', 2]], 'Love')
                };

                renderPage(<MyPosts />, '/myposts');

                // when
                await chooseOn('post-1', 'Love');

                // then
                expect(removeAssociationByPair).toHaveBeenCalledTimes(1);

                expect(removeAssociationByPair)
                    .toHaveBeenCalledWith(reactionPairFor('post-1', 'Love'));

                expect(upsertAssociation).not.toHaveBeenCalled();

                await openLikeOn('post-1');

                within(cardFor('post-1')).getAllByRole('menuitem').forEach((choice) =>
                    expect(choice).toHaveAttribute('aria-pressed', 'false'));
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
                // given: a DECIDED row the moderator did not contribute, which is what this
                // queue is mostly made of - it ticks Approved and Rejected by default. The
                // terminal lock greys out a moderation action that opens an editor (#508),
                // and here the action is a ROUTE to the detail page, so it stays live. On the
                // shared fixture this would prove nothing either way: its createdBy is the id
                // signInAs mints, so ownership would exempt the row whatever the lock did.
                pages = [{
                    items: [contentItemFor({
                        createdBy: 'account-miriam',
                        approvalStatus: ApprovalStatus.Approved
                    })],
                    pageIndex: 0,
                    pageSize: 8,
                    hasNextPage: false
                }];

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

        // THE ROUTE MUST SURVIVE A ROW THE MODERATOR DOES NOT OWN, which is the ordinary case
        // on a public feed and the one the two tests around this one cannot see: their fixture
        // is created by 'user-1', the very id signInAs mints, so the card reads as the
        // viewer's own and every ownership gate opens for the wrong reason. The feed carries
        // approved rows by construction (§14.1), and the terminal lock (#508) greys out a
        // moderation action that opens an editor on such a row — here the action is a ROUTE to
        // /Admin/Posts/{id}, where approval reset, the review thread and the takedown live, so
        // it stays live. Lock it and the moderation tier loses its only way in from a card.
        it('should send a moderator to the admin address on a row they did not contribute',
            async () => {
                // given
                pages = [{
                    items: [contentItemFor({ createdBy: 'account-miriam' })],
                    pageIndex: 0,
                    pageSize: 8,
                    hasNextPage: false
                }];

                renderPage(<Home />);

                // when
                await userEvent.click(screen.getByRole('button', { name: 'Moderate' }));

                // then
                expect(landedOn()).toBe('/Admin/Posts/devotional-1');
            });

        it('should send a moderator from the public list to the admin address', async () => {
            // given: approved and somebody else's, which is every row a public list carries
            pages = [{
                items: [contentItemFor({ createdBy: 'account-miriam' })],
                pageIndex: 0,
                pageSize: 8,
                hasNextPage: false
            }];

            renderPage(<Posts />, '/posts');

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
