import { ReactElement } from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentItemItemPanel } from './contentItemItemPanel';
import { AuthProvider } from '../securitys/authProvider';
import { createAuthState, signInAs, signOut } from '../../tests/testAuth';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    ApprovalStatus,
    ContentItemReactionOption,
    ContentItemSearchItem,
    ShareabilityBasis
} from '../../models/components/contentItems/contentItemSearchItem';

// One card, dispatched to a template by content type. EACH ELEMENT IS SELF-CONTAINED: the item
// arrives carrying its winning setting, resolved by the projection — the panel consults no
// collection, so every gate below is exercised by varying the element itself, which is exactly
// how a consumer changes one card without refetching a list.
//
// No router wrapper on purpose: every affordance is an EVENT, not a link. The auth double IS
// here, because two of the card's decisions are identity decisions: Edit belongs to the item's
// own submitter, Moderate to the moderation tier. Render gates only — the server re-decides
// both against the stored row.
const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

const renderCard = (ui: ReactElement) =>
    render(<AuthProvider>{ui}</AuthProvider>);

const settingFor = (
    contentType: ContentType,
    contentTypeName: string,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
        id: `setting-${contentType}`,
        contentType,
        contentItemId: null,
        contentTypeName,
        contentTypeDescription: contentTypeName,
        contentTypeIconCssClass: 'bi-chat-quote',
        sortOrder: contentType,
        hasTitle: contentType !== ContentType.Quote,
        hasAuthor: true,
        isAvailableAsGeneralUserContribution: true,
        tagsAllowed: true,
        showTags: true,
        reactionsAllowed: true,
        showReactions: true,
        linksAllowed: true,
        showLinks: true,
        attachmentsAllowed: true,
        showAttachments: true,
        commentsAllowed: true,
        showComments: true,
        bibleReferenceAllowed: true,
        showBibleReferences: true,
        limitReactionsToLoveOnly: false,
        createdBy: 'seed',
        createdWhen: '2026-01-01T00:00:00Z',
        updatedBy: 'seed',
        updatedWhen: '2026-01-01T00:00:00Z',
        deletedBy: null,
        deletedWhen: null,
        isDeleted: false,
        deletionReason: null,
        ...overrides
    });

const quoteSetting = settingFor(ContentType.Quote, 'Quotes');
const devotionalSetting = settingFor(ContentType.Devotional, 'Devotional');

const quoteItem: ContentItemSearchItem = {
    id: 'quote-1',
    contentType: ContentType.Quote,
    contentItemSetting: quoteSetting,
    author: 'William Temple',
    content: "When I pray, coincidences happen; when I don't, they don't",
    submittedById: 'account-bryan',
    submittedByName: 'Bryan',
    shareabilityBasis: ShareabilityBasis.PublicDomain,
    publishedDate: new Date(2026, 6, 18),
    tags: ['prayer', 'providence'],
    bibleReferences: ['James 5:16'],
    reactionSummary: [
        { label: 'Amen', glyph: '👍', count: 85 },
        { label: 'Love', glyph: '❤️', count: 43 },
        { label: 'Joy', glyph: '😄', count: 14 }
    ],
    commentCount: 9
};

const devotionalItem: ContentItemSearchItem = {
    id: 'devotional-1',
    contentType: ContentType.Devotional,
    contentItemSetting: devotionalSetting,
    title: 'Walking daily in grace',
    author: 'Joan',
    content: 'The whole devotional, far too long for a list.',
    excerpt: 'Grace is not a one-time event but the daily air the believer breathes.',
    imageUrl: 'https://example.test/grace.jpg',
    submittedById: 'account-joan',
    submittedByName: 'Joan',
    shareabilityBasis: ShareabilityBasis.Owned,
    publishedDate: new Date(2026, 6, 3),
    commentCount: 5
};

// The one-element-swap the model is built for: the same item under a different governing row.
const withSetting = (
    item: ContentItemSearchItem,
    overrides: Partial<ContentItemSetting>): ContentItemSearchItem => ({
        ...item,
        contentItemSetting: settingFor(
            item.contentType,
            item.contentItemSetting?.contentTypeName ?? 'Setting',
            overrides)
    });

const reactionOptions: ReadonlyArray<ContentItemReactionOption> = [
    { label: 'Amen', glyph: '👍' },
    { label: 'Love', glyph: '❤️', isLove: true },
    { label: 'Joy', glyph: '😄' }
];

describe('ContentItemItemPanel', () => {
    beforeEach(() => {
        signOut(authState);
    });

    describe('template dispatch', () => {
        it('should render a quote through the quotes override, whole', () => {
            renderCard(<ContentItemItemPanel contentItem={quoteItem} />);

            expect(screen.getByText(new RegExp('coincidences happen'))).toBeInTheDocument();
            expect(screen.getByText(/— William Temple/)).toBeInTheDocument();
        });

        it('should render every other type through the default template, excerpted', () => {
            renderCard(<ContentItemItemPanel contentItem={devotionalItem} />);

            expect(screen.getByText('Walking daily in grace')).toBeInTheDocument();
            expect(screen.getByText(new RegExp('daily air'))).toBeInTheDocument();
            expect(screen.queryByText(devotionalItem.content)).not.toBeInTheDocument();
        });

        // A verse card IS its verse: the content already carries its own quotation marks and
        // reference, so the verses override renders it whole and appends nothing.
        it('should render a verse image through the verses override, appending nothing', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={{
                        id: 'verses-1',
                        contentType: ContentType.Verses,
                        contentItemSetting: settingFor(
                            ContentType.Verses, 'Verse Image', { hasTitle: false }),
                        author: 'The Bible',
                        content: '“For God so loved the world…” — John 3:16 ESV',
                        submittedByName: 'Bryan',
                        publishedDate: new Date(2026, 6, 18)
                    }} />);

            expect(screen.getByText(new RegExp('John 3:16 ESV'))).toBeInTheDocument();

            expect(screen.queryByText(new RegExp('ESV — The Bible')))
                .not.toBeInTheDocument();

            expect(screen.getByText('Verse Image')).toBeInTheDocument();
        });

        // The quotes override DERIVES from the default: only the content slot differs, so the
        // meta row is the default's own on both.
        it('should carry the default meta row under the quotes override', () => {
            renderCard(<ContentItemItemPanel contentItem={quoteItem} />);

            expect(screen.getByText('Bryan')).toBeInTheDocument();
            expect(screen.getByText('Public Domain')).toBeInTheDocument();
            expect(screen.getByText('Jul 18, 2026')).toBeInTheDocument();
        });
    });

    describe('the element governs its own card', () => {
        it('should name the card by its own setting rather than the enum member', () => {
            renderCard(<ContentItemItemPanel contentItem={quoteItem} />);

            expect(screen.getByRole('button', { name: /Quotes/ })).toBeInTheDocument();
        });

        it('should fall back to the enum label when the element carries no setting', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={{ ...devotionalItem, contentItemSetting: undefined }} />);

            expect(screen.getByRole('button', { name: /Devotional/ })).toBeInTheDocument();
        });

        // The one-element-swap contract: hand the SAME item a different governing row and only
        // what that row says changes — no collection, no refetch.
        it('should follow a swapped setting on its own element', () => {
            const taggedItem = { ...devotionalItem, tags: ['grace'] };

            const rendered = renderCard(<ContentItemItemPanel contentItem={taggedItem} />);

            expect(screen.getByText('#grace')).toBeInTheDocument();

            rendered.rerender(
                <AuthProvider>
                    <ContentItemItemPanel
                        contentItem={withSetting(taggedItem, { showTags: false })} />
                </AuthProvider>);

            expect(screen.queryByText('#grace')).not.toBeInTheDocument();
        });

        it('should hide the tags and references where its setting says so', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={withSetting(
                        {
                            ...devotionalItem,
                            tags: ['grace'],
                            bibleReferences: ['Ephesians 2:8-9']
                        },
                        { showTags: false, showBibleReferences: false })} />);

            expect(screen.queryByText('#grace')).not.toBeInTheDocument();
            expect(screen.queryByText('Ephesians 2:8-9')).not.toBeInTheDocument();
        });

        it('should say nothing about comments where its setting hides them', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={withSetting(devotionalItem, { showComments: false })}
                    onCommentsClick={vi.fn()} />);

            expect(screen.queryByText(/comments/i)).not.toBeInTheDocument();
        });

        it('should drop the author segment on a type whose setting carries none', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={withSetting(devotionalItem, { hasAuthor: false })}
                    onAuthorClick={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /Author/ })).not.toBeInTheDocument();
        });

        it('should hide the assigned reactions where its setting hides them', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={withSetting(quoteItem, { showReactions: false })} />);

            expect(screen.queryByText('142')).not.toBeInTheDocument();
        });
    });

    describe('status', () => {
        it('should wear its status while it is not yet public', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={{
                        ...devotionalItem,
                        approvalStatus: ApprovalStatus.Submitted
                    }} />);

            expect(screen.getByText('In review')).toBeInTheDocument();
        });

        it('should say nothing about an approved item, which is the ordinary case', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={{
                        ...devotionalItem,
                        approvalStatus: ApprovalStatus.Approved
                    }} />);

            expect(screen.queryByText('In review')).not.toBeInTheDocument();
            expect(screen.queryByText('Draft')).not.toBeInTheDocument();
        });
    });

    describe('event hooks', () => {
        it('should raise onContentTypeClick from the type badge', async () => {
            const onContentTypeClick = vi.fn();

            renderCard(
                <ContentItemItemPanel
                    contentItem={devotionalItem}
                    onContentTypeClick={onContentTypeClick} />);

            await userEvent.click(screen.getByRole('button', { name: /Devotional/ }));

            expect(onContentTypeClick).toHaveBeenCalledWith(devotionalItem);
        });

        it('should raise onTitleClick from the title', async () => {
            const onTitleClick = vi.fn();

            renderCard(
                <ContentItemItemPanel
                    contentItem={devotionalItem}
                    onTitleClick={onTitleClick} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Walking daily in grace' }));

            expect(onTitleClick).toHaveBeenCalledWith(devotionalItem);
        });

        // A quote has no title, so its CONTENT is the way in — the same destination.
        it('should raise onTitleClick from the quote itself', async () => {
            const onTitleClick = vi.fn();

            renderCard(
                <ContentItemItemPanel contentItem={quoteItem} onTitleClick={onTitleClick} />);

            await userEvent.click(
                screen.getByRole('button', { name: new RegExp('coincidences happen') }));

            expect(onTitleClick).toHaveBeenCalledWith(quoteItem);
        });

        it('should raise onSubmittedByClick and onAuthorClick as two different people', async () => {
            const onSubmittedByClick = vi.fn();
            const onAuthorClick = vi.fn();

            renderCard(
                <ContentItemItemPanel
                    contentItem={quoteItem}
                    onSubmittedByClick={onSubmittedByClick}
                    onAuthorClick={onAuthorClick} />);

            await userEvent.click(screen.getByRole('button', { name: /Submitted by/ }));
            await userEvent.click(screen.getByRole('button', { name: /Author/ }));

            expect(onSubmittedByClick).toHaveBeenCalledWith(quoteItem);
            expect(onAuthorClick).toHaveBeenCalledWith(quoteItem);
        });

        it('should raise onTagClick and onBibleReferenceClick with the pill pressed', async () => {
            const onTagClick = vi.fn();
            const onBibleReferenceClick = vi.fn();

            renderCard(
                <ContentItemItemPanel
                    contentItem={quoteItem}
                    onTagClick={onTagClick}
                    onBibleReferenceClick={onBibleReferenceClick} />);

            await userEvent.click(screen.getByRole('button', { name: '#prayer' }));
            await userEvent.click(screen.getByRole('button', { name: /James 5:16/ }));

            expect(onTagClick).toHaveBeenCalledWith(quoteItem, 'prayer');
            expect(onBibleReferenceClick).toHaveBeenCalledWith(quoteItem, 'James 5:16');
        });

        it('should raise onCommentsClick with the count on show', async () => {
            const onCommentsClick = vi.fn();

            renderCard(
                <ContentItemItemPanel
                    contentItem={quoteItem}
                    onCommentsClick={onCommentsClick} />);

            await userEvent.click(screen.getByRole('button', { name: /9 comments/ }));

            expect(onCommentsClick).toHaveBeenCalledWith(quoteItem);
        });

        it('should raise onReadMoreClick from the read-more affordance', async () => {
            const onReadMoreClick = vi.fn();

            renderCard(
                <ContentItemItemPanel
                    contentItem={devotionalItem}
                    onReadMoreClick={onReadMoreClick} />);

            await userEvent.click(screen.getByRole('button', { name: 'read more…' }));

            expect(onReadMoreClick).toHaveBeenCalledWith(devotionalItem);
        });

        // Share and Save render only when somebody is listening — a control whose event goes
        // nowhere is worse than no control.
        it('should offer Share and Save only where they are wired', async () => {
            const onShareClick = vi.fn();

            const rendered = renderCard(
                <ContentItemItemPanel contentItem={devotionalItem} />);

            expect(screen.queryByRole('button', { name: /Share/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Save/ })).not.toBeInTheDocument();

            rendered.rerender(
                <AuthProvider>
                    <ContentItemItemPanel
                        contentItem={devotionalItem}
                        onShareClick={onShareClick} />
                </AuthProvider>);

            await userEvent.click(screen.getByRole('button', { name: /Share/ }));

            expect(onShareClick).toHaveBeenCalledWith(devotionalItem);
        });
    });

    describe('Edit and Moderate', () => {
        // signInAs mints userId 'user-1'; the devotional's submittedById is 'account-joan'.
        const ownItem = { ...devotionalItem, submittedById: 'user-1' };

        it('should offer Edit to the person who submitted it and to nobody else', async () => {
            const onEditClick = vi.fn();
            signInAs(authState, ['Users']);

            const rendered = renderCard(
                <ContentItemItemPanel
                    contentItem={devotionalItem}
                    onEditClick={onEditClick} />);

            // somebody else's item — no Edit however wired
            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();

            rendered.rerender(
                <AuthProvider>
                    <ContentItemItemPanel contentItem={ownItem} onEditClick={onEditClick} />
                </AuthProvider>);

            await userEvent.click(screen.getByRole('button', { name: /Edit/ }));

            expect(onEditClick).toHaveBeenCalledWith(ownItem);
        });

        it('should offer Edit to no anonymous visitor', () => {
            renderCard(
                <ContentItemItemPanel contentItem={ownItem} onEditClick={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();
        });

        it.each([
            ['Administrators'],
            ['Reviewers'],
            ['Publishers'],
            ['ContentItem-Reviewers'],
            ['ContentItem-Publishers'],
            ['ContentItem-Devotional-Reviewers'],
            ['ContentItem-Devotional-Publishers']
        ])('should offer Moderate to the %s tier with the shield', async (role) => {
            const onModerateClick = vi.fn();
            signInAs(authState, [role]);

            const rendered = renderCard(
                <ContentItemItemPanel
                    contentItem={devotionalItem}
                    onModerateClick={onModerateClick} />);

            await userEvent.click(screen.getByRole('button', { name: /Moderate/ }));

            expect(onModerateClick).toHaveBeenCalledWith(devotionalItem);
            expect(rendered.container.querySelector('i.bi-shield')).toBeInTheDocument();
        });

        // The narrow tier is scoped to the item's OWN type: a Story reviewer moderates
        // stories, not this devotional.
        it('should offer no Moderate to a tier scoped to another type', () => {
            signInAs(authState, ['ContentItem-Story-Reviewers']);

            renderCard(
                <ContentItemItemPanel
                    contentItem={devotionalItem}
                    onModerateClick={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /Moderate/ }))
                .not.toBeInTheDocument();
        });

        it('should offer no Moderate to an ordinary reader', () => {
            signInAs(authState, ['Users']);

            renderCard(
                <ContentItemItemPanel
                    contentItem={devotionalItem}
                    onModerateClick={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /Moderate/ }))
                .not.toBeInTheDocument();
        });

        // The sanction outranks every grant (#366): ReadOnly at any scope silences both
        // actions, the item's own submitter included.
        it('should honour the ReadOnly veto over both actions', () => {
            signInAs(authState, ['Administrators', 'ContentItem-Devotional-ReadOnly']);

            renderCard(
                <ContentItemItemPanel
                    contentItem={ownItem}
                    onEditClick={vi.fn()}
                    onModerateClick={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();

            expect(screen.queryByRole('button', { name: /Moderate/ }))
                .not.toBeInTheDocument();
        });

        // The isModeratedView matrix: the moderated surface offers Moderate ALONE, wearing
        // Edit's pencil and label — on a surface that IS moderation, the moderation action
        // is simply what editing means there.
        it('should dress Moderate as Edit and stand it alone on a moderated view', async () => {
            const onEditClick = vi.fn();
            const onModerateClick = vi.fn();
            signInAs(authState, ['Administrators']);

            const rendered = renderCard(
                <ContentItemItemPanel
                    contentItem={ownItem}
                    isModeratedView
                    onEditClick={onEditClick}
                    onModerateClick={onModerateClick} />);

            // one action button, labelled Edit, carrying the pencil — but it is Moderate
            expect(screen.queryByRole('button', { name: /Moderate/ }))
                .not.toBeInTheDocument();

            await userEvent.click(screen.getByRole('button', { name: /Edit/ }));

            expect(onModerateClick).toHaveBeenCalledWith(ownItem);
            expect(onEditClick).not.toHaveBeenCalled();
            expect(rendered.container.querySelector('i.bi-pencil')).toBeInTheDocument();
            expect(rendered.container.querySelector('i.bi-shield')).not.toBeInTheDocument();
        });

        it('should offer both, side by side, to an owning moderator off the moderated view', () => {
            signInAs(authState, ['Administrators']);

            renderCard(
                <ContentItemItemPanel
                    contentItem={ownItem}
                    onEditClick={vi.fn()}
                    onModerateClick={vi.fn()} />);

            expect(screen.getByRole('button', { name: /Edit/ })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: /Moderate/ })).toBeInTheDocument();
        });
    });

    describe('assigned reactions', () => {
        it('should show the compact cluster with the summed total', () => {
            renderCard(<ContentItemItemPanel contentItem={quoteItem} />);

            expect(screen.getByText('142')).toBeInTheDocument();
            expect(screen.queryByText('All 142')).not.toBeInTheDocument();
        });

        it('should toggle to the per-reaction counts and back', async () => {
            renderCard(<ContentItemItemPanel contentItem={quoteItem} />);

            await userEvent.click(screen.getByRole('button', { name: 'Reaction counts' }));

            expect(screen.getByText('All 142')).toBeInTheDocument();
            expect(screen.getByText(/85/)).toBeInTheDocument();
            expect(screen.getByText(/43/)).toBeInTheDocument();

            await userEvent.click(screen.getByRole('button', { name: 'Reaction counts' }));

            expect(screen.queryByText('All 142')).not.toBeInTheDocument();
        });

        it('should show no cluster on a card given no summary', () => {
            renderCard(<ContentItemItemPanel contentItem={devotionalItem} />);

            expect(screen.queryByRole('button', { name: 'Reaction counts' }))
                .not.toBeInTheDocument();
        });
    });

    describe('giving a reaction', () => {
        it('should open the choices from Like and raise the selection', async () => {
            const onReactionSelected = vi.fn();

            renderCard(
                <ContentItemItemPanel
                    contentItem={quoteItem}
                    reactionOptions={reactionOptions}
                    onReactionSelected={onReactionSelected} />);

            expect(screen.queryByRole('menu')).not.toBeInTheDocument();

            await userEvent.click(screen.getByRole('button', { name: /Like/ }));
            await userEvent.click(screen.getByRole('menuitem', { name: 'Love' }));

            expect(onReactionSelected).toHaveBeenCalledWith(
                quoteItem, expect.objectContaining({ label: 'Love' }));

            // Choosing closes the choices — one click, one decision.
            expect(screen.queryByRole('menu')).not.toBeInTheDocument();
        });

        it('should mark the reaction this reader already gave', async () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={{ ...quoteItem, viewerReactionLabel: 'Love' }}
                    reactionOptions={reactionOptions}
                    onReactionSelected={vi.fn()} />);

            await userEvent.click(screen.getByRole('button', { name: /Like/ }));

            expect(screen.getByRole('menuitem', { name: 'Love' }))
                .toHaveAttribute('aria-pressed', 'true');

            expect(screen.getByRole('menuitem', { name: 'Amen' }))
                .toHaveAttribute('aria-pressed', 'false');
        });

        it('should offer no Like when nobody is listening', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={quoteItem}
                    reactionOptions={reactionOptions} />);

            expect(screen.queryByRole('button', { name: /Like/ })).not.toBeInTheDocument();
        });

        it('should offer no Like when its setting does not accept reactions', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={withSetting(quoteItem, { reactionsAllowed: false })}
                    reactionOptions={reactionOptions}
                    onReactionSelected={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /Like/ })).not.toBeInTheDocument();
        });

        it('should keep only the love option where its setting limits to it', async () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={withSetting(quoteItem, { limitReactionsToLoveOnly: true })}
                    reactionOptions={reactionOptions}
                    onReactionSelected={vi.fn()} />);

            await userEvent.click(screen.getByRole('button', { name: /Like/ }));

            expect(screen.getByRole('menuitem', { name: 'Love' })).toBeInTheDocument();
            expect(screen.queryByRole('menuitem', { name: 'Amen' })).not.toBeInTheDocument();
            expect(screen.queryByRole('menuitem', { name: 'Joy' })).not.toBeInTheDocument();
        });
    });

    describe('honest figures', () => {
        // The count is a figure and figures are never invented; the way IN is an affordance
        // and renders regardless, uncounted.
        it('should offer the comments control uncounted when no count was given', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={{ ...devotionalItem, commentCount: undefined }}
                    onCommentsClick={vi.fn()} />);

            expect(screen.getByRole('button', { name: 'Comments' })).toBeInTheDocument();
            expect(screen.queryByText(/\d+ comments/)).not.toBeInTheDocument();
        });

        it('should fall back to the content when no excerpt was written', () => {
            renderCard(
                <ContentItemItemPanel
                    contentItem={{ ...devotionalItem, excerpt: undefined }} />);

            expect(screen.getByText(new RegExp('far too long'))).toBeInTheDocument();
        });
    });
});
