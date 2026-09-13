import { createElement } from 'react';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PostDetail } from './postDetail';
import { AuthProvider } from '../components/securitys/authProvider';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentItemSetting } from '../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { createAuthState, signInAs, signOut } from '../tests/testAuth';

import {
    ContentItemPanelProps
} from '../components/contentItems/contentItemPanel';

const authState = createAuthState();
let contentItem: ContentItem | undefined;
let isLoading = false;
let isError = false;
let requestedId = '';

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

const toastSuccess = vi.fn();

vi.mock('../brokers/toastBroker.success', () => ({
    toastSuccess: (message: string) => toastSuccess(message)
}));

// WHAT THE PAGE HANDS THE PANEL, captured while the REAL panel still renders — every other
// test in this file reads the rendered card, so the spy wraps rather than replaces.
//
// It exists for the two section switches. Whether the CARD obeys them is pinned where it
// belongs, on the panel's own suite (contentItemPanel.test.tsx puts tags on an item and
// asserts the chip is absent). It cannot be pinned HERE by rendering: toContentItemSearchItem
// builds its projection field by field and deliberately leaves tags and bibleReferences unset
// until the association reads exist (#318), so no item this page can construct would make the
// in-card sections render whatever the switches said.
let panelProps: ContentItemPanelProps | undefined;

vi.mock('../components/contentItems/contentItemPanel', async () => {
    const actual = await vi.importActual<
        typeof import('../components/contentItems/contentItemPanel')>(
            '../components/contentItems/contentItemPanel');

    return {
        ...actual,
        ContentItemPanel: (props: ContentItemPanelProps) => {
            panelProps = props;

            return createElement(actual.ContentItemPanel, props);
        }
    };
});

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemService: {
        useGetContentItemById: (contentItemId: string) => {
            requestedId = contentItemId;

            return { data: contentItem, isLoading, isError };
        }
    }
}));

const testimonySetting: ContentItemSetting = {
    id: '11111111-1111-1111-1111-111111111111',
    contentType: ContentType.Testimony,
    contentItemId: null,
    contentTypeName: 'Testimony',
    contentTypeDescription: 'Your walk with Him',
    contentTypeIconCssClass: 'bi-chat-heart',
    sortOrder: 0,
    hasTitle: true,
    hasAuthor: false,
    isAvailableAsGeneralUserContribution: true,
    tagsAllowed: true,
    showTags: true,
    reactionsAllowed: true,
    showReactions: true,
    linksAllowed: false,
    showLinks: true,
    attachmentsAllowed: false,
    showAttachments: true,
    commentsAllowed: true,
    showComments: true,
    bibleReferenceAllowed: true,
    showBibleReferences: true,
    limitReactionsToLoveOnly: false,
    createdBy: 'system-seed',
    createdWhen: '2026-08-28T12:21:18.308+00:00',
    updatedBy: 'system-seed',
    updatedWhen: '2026-08-28T12:21:18.308+00:00',
    deletedBy: null,
    deletedWhen: null,
    isDeleted: false,
    deletionReason: null
};

// An override for THIS item, and a quote whose type carries no title at all - the page's heading
// has to resolve both exactly as the panel does.
const testimonyOverride: ContentItemSetting = {
    ...testimonySetting,
    id: '22222222-2222-2222-2222-222222222222',
    contentItemId: 'content-item-1',
    contentTypeName: 'A Testimony, Retitled'
};

const quoteSetting: ContentItemSetting = {
    ...testimonySetting,
    id: '33333333-3333-3333-3333-333333333333',
    contentType: ContentType.Quote,
    contentTypeName: 'Quote',
    hasTitle: false
};

let contentItemSettings: ContentItemSetting[] = [];

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: contentItemSettings, isLoading: false, isError: false }),
        useGetEffectiveSettingsFor: () =>
            ({ data: contentItemSettings, isLoading: false, isError: false })
    }
}));

// The reaction vocabulary behind the Like control, the same read the feeds make.
vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        useGetApprovedReactions: () => ({
            data: [{
                id: 'reaction-1',
                name: 'Amen',
                unicodeEmoji: '🙏',
                isPublished: true,
                approvalStatus: 2,
                isDeleted: false
            }]
        })
    }
}));

// The byline's second read. Mocked with a resolved contributor by default so the byline is
// present in every test below rather than being a special case, and captured so the page can be
// held to asking for the account the ITEM names rather than for the reader who is signed in.
let requestedContributorId = '';

vi.mock('../services/foundations/contributorService', () => ({
    contributorService: {
        useGetContributorById: (userId: string) => {
            requestedContributorId = userId;

            return {
                data: {
                    userId,
                    displayName: 'Louis Ferguson',
                    imageUrl: null
                },
                isLoading: false,
                isError: false
            };
        }
    }
}));

// signInAs mints userId 'user-1', so this row belongs to the reader in every test below — the
// hardest case for "editing is disabled here", since the owner is the one account that could
// otherwise amend it at any status.
const ownedItem: ContentItem = {
    id: 'content-item-1',
    contentType: ContentType.Testimony,
    title: 'He kept me through the night shift',
    author: null,
    content: 'The whole testimony, as it happened.',
    shareabilityBasis: 0,
    sharePermission: null,
    contentHash: 'hash',
    groupId: 'group-1',
    version: 1,
    publishDate: null,
    isPublished: false,
    approvalStatus: 0,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy: 'user-1',
    createdWhen: '2026-08-30T10:22:41.237+00:00',
    updatedBy: 'user-1',
    updatedWhen: '2026-08-30T10:22:41.237+00:00',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null
};

const renderPage = () =>
    render(
        <MemoryRouter initialEntries={['/posts/content-item-1']}>
            <AuthProvider>
                <Routes>
                    {/* Declared before the parameter route for the reader's sake — React
                        Router ranks a static segment above a dynamic one whatever the order.
                        It stands here so the invitation to contribute has a real destination
                        to land on rather than an assertion about a spy. */}
                    <Route path="/posts/contribute" element={<h2>Share something</h2>} />
                    <Route path="/posts/:contentItemId" element={<PostDetail />} />
                </Routes>
            </AuthProvider>
        </MemoryRouter>);

describe('PostDetail', () => {
    beforeEach(() => {
        toastSuccess.mockClear();
        panelProps = undefined;
        signOut(authState);
        contentItem = ownedItem;
        contentItemSettings = [testimonySetting, quoteSetting];
        isLoading = false;
        isError = false;
    });

    it('should read the item named by the route', () => {
        // when
        renderPage();

        // then
        expect(requestedId).toBe('content-item-1');
    });

    it('should render the item through the view face, full and unclamped', () => {
        // when
        renderPage();

        // then: the same card the feeds show, carrying the FULL content — the page left
        // the excerpt off, so nothing clamps the reading surface
        expect(screen.getByRole('heading',
            { name: 'He kept me through the night shift', level: 3 })).toBeInTheDocument();

        expect(screen.getByText('The whole testimony, as it happened.')).toBeInTheDocument();
        expect(screen.getByText('Testimony')).toBeInTheDocument();
        expect(screen.queryByLabelText(/Title/)).not.toBeInTheDocument();
    });

    it('should stand the item in the seven beside a five', () => {
        // when
        const { container } = renderPage();

        // then: the layout contract itself — 7 for the item, 5 for what sits beside it,
        // the same split the contributor's own surface keeps
        expect(container.querySelector('.col-lg-7 h1')).toBeInTheDocument();
        expect(container.querySelector('.col-lg-5')).toBeInTheDocument();
    });

    it('should stand the association surfaces in the five', () => {
        // when
        const { container } = renderPage();
        const rightColumn = container.querySelector('.col-lg-5') as HTMLElement;

        // then: a reader meets the tags and the bible references beside the article — the
        // controls this page carried nowhere before
        expect(within(rightColumn).getByRole('heading', { name: 'Tags' }))
            .toBeInTheDocument();

        expect(within(rightColumn).getByRole('heading', { name: 'Bible references' }))
            .toBeInTheDocument();
    });

    it('should offer a signed-in reader the box itself, not only its heading', () => {
        // given: signed out the panels show a login prompt and the suggest HEADING renders
        // on both branches, so only a signed-in render proves the box is really there
        signInAs(authState);

        // when
        renderPage();

        // then
        expect(screen.getByRole('textbox', { name: 'Suggest a tag' })).toBeInTheDocument();

        expect(screen.getByRole('textbox', { name: 'Suggest a bible reference' }))
            .toBeInTheDocument();
    });

    it('should answer a suggested tag honestly rather than dropping it', async () => {
        // given
        signInAs(authState);
        renderPage();

        // when
        await userEvent.type(
            screen.getByRole('textbox', { name: 'Suggest a tag' }), 'grace');

        await userEvent.click(
            within(screen.getByRole('textbox', { name: 'Suggest a tag' })
                .closest('section') as HTMLElement)
                .getByRole('button', { name: 'Add' }));

        // then: the write lands with the association exposer (#318); until then somebody who
        // typed a suggestion is told where it went rather than watching it vanish
        expect(toastSuccess).toHaveBeenCalledWith('Suggesting tags is coming soon.');
    });

    it('should answer a suggested bible reference honestly rather than dropping it', async () => {
        // given
        signInAs(authState);
        renderPage();

        // when
        await userEvent.type(
            screen.getByRole('textbox', { name: 'Suggest a bible reference' }),
            'Romans 3:23');

        await userEvent.click(
            within(screen.getByRole('textbox', { name: 'Suggest a bible reference' })
                .closest('section') as HTMLElement)
                .getByRole('button', { name: 'Add' }));

        // then
        expect(toastSuccess)
            .toHaveBeenCalledWith('Suggesting bible references is coming soon.');
    });

    it('should invite the reader to share something of their own from the five', () => {
        // when
        const { container } = renderPage();
        const rightColumn = container.querySelector('.col-lg-5') as HTMLElement;

        // then
        expect(rightColumn.textContent).toContain('Have something to share?');
    });

    it('should carry the reader to the contribution surface and back here', async () => {
        // given
        renderPage();

        // when
        await userEvent.click(screen.getByRole('button', { name: /Submit a contribution/ }));

        // then: the invitation leads somewhere real. What it carries in router state is
        // asserted by nobody here on purpose — /posts/contribute does not read it yet, so a
        // test on it would pin an intention rather than a behaviour
        expect(screen.getByRole('heading', { name: 'Share something' })).toBeInTheDocument();
    });

    it('should say the same association fact once, beside the card and not within it', () => {
        // when
        renderPage();

        // then: the in-card sections are switched off, so when the association reads land
        // (#318) a tag cannot appear both on the card and in the panel next to it.
        //
        // ASSERTED AT THE SEAM, and it has to be: the projection carries no tags or bible
        // references yet, so the in-card sections cannot render whatever the switches say and
        // a rendering assertion here would pass against a page that never set them. What the
        // CARD does with the switches is pinned on the panel's own suite.
        expect(panelProps?.showTagSection).toBe(false);
        expect(panelProps?.showBibleReferenceSection).toBe(false);
    });

    it('should head the document once, out of sight, and let the card carry the title', () => {
        // when
        renderPage();

        // then: the outline gets its h1 (visually hidden — the card says it in view), and
        // the visible title is the card's own h3, same as everywhere else in the family
        const pageHeading = screen.getByRole('heading',
            { name: 'He kept me through the night shift', level: 1 });

        expect(pageHeading).toHaveClass('visually-hidden');

        expect(screen.getByRole('heading',
            { name: 'He kept me through the night shift', level: 3 })).toBeInTheDocument();
    });

    it('should name the contributor the item records, not the reader looking at it', () => {
        // given: a reader who is not the contributor
        signInAs(authState);
        contentItem = { ...ownedItem, createdBy: 'somebody-else' };

        // when
        renderPage();

        // then
        expect(requestedContributorId).toBe('somebody-else');
        expect(screen.getByText('Submitted by')).toBeInTheDocument();
        expect(screen.getByText('Louis Ferguson')).toBeInTheDocument();
    });

    it('should carry the engagement row the feeds carry', () => {
        // when
        renderPage();

        // then: a reader who followed a card here meets the same three controls the card
        // offered — the detail surface must not be the one place they go missing
        expect(screen.getByRole('button', { name: /Like/ })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /Share/ })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /Save/ })).toBeInTheDocument();
    });

    it('should answer Save honestly, so the control is not merely present', async () => {
        // given: presence alone would pass against a transposed pair of handlers
        renderPage();

        // when
        await userEvent.click(screen.getByRole('button', { name: /Save/ }));

        // then: saving is a ContentItem association and waits on #318 like the rest
        expect(toastSuccess).toHaveBeenCalledWith('Saving posts is coming soon.');
    });

    it('should copy THIS post’s address when the reader shares it', async () => {
        // given
        const writeText = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal('navigator', { ...navigator, clipboard: { writeText } });
        renderPage();

        // when
        await userEvent.click(screen.getByRole('button', { name: /Share/ }));

        // then: the item's own permanent address, not the feed the reader came from
        expect(writeText).toHaveBeenCalledWith(
            `${window.location.origin}/posts/content-item-1`);

        vi.unstubAllGlobals();
    });

    it('should offer no moderation control however the reader is trusted', () => {
        // given: an administrator holds every grant there is, and a public reading surface
        // still decides nothing — Moderate is what the control is called off an admin surface
        signInAs(authState, ['Administrators']);

        // when
        renderPage();

        // then
        expect(screen.queryByRole('button', { name: /Moderate/ })).not.toBeInTheDocument();
    });

    // Choosing CLOSES the picker — the panel's own behaviour — so both tests below reopen it
    // to read the mark back. The card itself shows nothing yet: a summary needs counts, and
    // those arrive with the association reads (#318).
    const chooseReaction = async () => {
        await userEvent.click(screen.getByRole('button', { name: /Like/ }));
        await userEvent.click(screen.getByRole('menuitem', { name: 'Amen' }));
    };

    const reopenPicker = async () => {
        await userEvent.click(screen.getByRole('button', { name: /Like/ }));

        return screen.getByRole('menuitem', { name: 'Amen' });
    };

    it('should mark the reaction the reader chose for this visit', async () => {
        // given
        renderPage();

        // when
        await chooseReaction();

        // then: the choice reached the CARD, which is what this page's fold of the visit's
        // reactions into its one projection is for. Nothing is persisted — the write lands
        // with the association exposer (#318)
        expect(await reopenPicker()).toHaveAttribute('aria-pressed', 'true');
    });

    it('should withdraw the reaction when the reader chooses it again', async () => {
        // given
        renderPage();
        await chooseReaction();

        // when: the same choice again is a change of mind
        await userEvent.click(await reopenPicker());

        // then
        expect(await reopenPicker()).toHaveAttribute('aria-pressed', 'false');
    });

    it('should claim no engagement figures it has no source for', () => {
        // when
        renderPage();

        // then: there is no comment, reaction or view client in this app yet, and a zero would
        // assert an empty conversation rather than an absent one
        expect(screen.queryByText(/reaction/)).not.toBeInTheDocument();
        expect(screen.queryByText(/comment/)).not.toBeInTheDocument();
        expect(screen.queryByText(/View/)).not.toBeInTheDocument();
    });

    it('should fall back to the content type name for a type that carries no title', () => {
        // given
        contentItem = { ...ownedItem, title: null };

        // when
        renderPage();

        // then
        expect(screen.getByRole('heading', { name: 'Testimony', level: 1 })).toBeInTheDocument();
    });

    it('should not shout a title the panel deliberately hides', () => {
        // given: a type whose effective setting carries no title, on a row that still has one -
        // the panel hides it, so the page must not promote it to the h1
        contentItem = { ...ownedItem, contentType: ContentType.Quote, title: 'A stored title' };

        // when
        renderPage();

        // then
        expect(screen.queryByRole('heading', { name: 'A stored title' })).not.toBeInTheDocument();
        expect(screen.getByRole('heading', { name: 'Quote', level: 1 })).toBeInTheDocument();
    });

    it('should prefer an item override when naming the page', () => {
        // given
        contentItemSettings = [testimonySetting, testimonyOverride];
        contentItem = { ...ownedItem, title: null };

        // when
        renderPage();

        // then: the override for THIS item wins over the content type default
        expect(screen.getByRole('heading', { name: 'A Testimony, Retitled', level: 1 }))
            .toBeInTheDocument();
    });

    it('should name the type rather than a literal before the settings arrive', () => {
        // given
        contentItemSettings = [];
        contentItem = { ...ownedItem, title: null };

        // when
        renderPage();

        // then: the fixed enum label, never "Contribution"
        expect(screen.getByRole('heading', { name: 'Testimony', level: 1 })).toBeInTheDocument();
    });

    it('should offer no editing to the reader who contributed it', () => {
        // given: showEditSection is left off, which is the switch ahead of every role check
        signInAs(authState);

        // when
        renderPage();

        // then
        expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
    });

    it('should offer no editing to an administrator either', () => {
        // given
        signInAs(authState, ['Administrators']);

        // when
        renderPage();

        // then
        expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: /Delete/ })).not.toBeInTheDocument();
    });

    it('should say so rather than render an empty page when the item cannot be read', () => {
        // given: the read is [AllowAnonymous] but filtered per caller, so "not found" and
        // "not yours to read" are the same answer here
        isError = true;
        contentItem = undefined;

        // when
        renderPage();

        // then
        expect(screen.getByRole('alert')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: /Back to the journal/ })).toBeInTheDocument();
    });
});
