import { ReactElement } from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentItemPanel } from './contentItemPanel';
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
    render(
        <MemoryRouter initialEntries={['/myposts/devotional-1']}>
            <AuthProvider>{ui}</AuthProvider>
        </MemoryRouter>);

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

describe('ContentItemPanel', () => {
    beforeEach(() => {
        signOut(authState);
    });

    describe('template dispatch', () => {
        it('should render a quote through the quotes override, whole', () => {
            renderCard(<ContentItemPanel contentItem={quoteItem} />);

            expect(screen.getByText(new RegExp('coincidences happen'))).toBeInTheDocument();
            expect(screen.getByText(/— William Temple/)).toBeInTheDocument();
        });

        it('should render every other type through the default template, excerpted', () => {
            renderCard(<ContentItemPanel contentItem={devotionalItem} />);

            expect(screen.getByText('Walking daily in grace')).toBeInTheDocument();
            expect(screen.getByText(new RegExp('daily air'))).toBeInTheDocument();
            expect(screen.queryByText(devotionalItem.content)).not.toBeInTheDocument();
        });

        // A verse card IS its verse: the content already carries its own quotation marks and
        // reference, so the verses override renders it whole and appends nothing.
        it('should render a verse image through the verses override, appending nothing', () => {
            renderCard(
                <ContentItemPanel
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
            renderCard(<ContentItemPanel contentItem={quoteItem} />);

            expect(screen.getByText('Bryan')).toBeInTheDocument();
            expect(screen.getByText('Public Domain')).toBeInTheDocument();
            expect(screen.getByText('Jul 18, 2026')).toBeInTheDocument();
        });
    });

    describe('the element governs its own card', () => {
        it('should name the card by its own setting rather than the enum member', () => {
            renderCard(<ContentItemPanel contentItem={quoteItem} />);

            expect(screen.getByRole('button', { name: /Quotes/ })).toBeInTheDocument();
        });

        it('should fall back to the enum label when the element carries no setting', () => {
            renderCard(
                <ContentItemPanel
                    contentItem={{ ...devotionalItem, contentItemSetting: undefined }} />);

            expect(screen.getByRole('button', { name: /Devotional/ })).toBeInTheDocument();
        });

        // The one-element-swap contract: hand the SAME item a different governing row and only
        // what that row says changes — no collection, no refetch.
        it('should follow a swapped setting on its own element', () => {
            const taggedItem = { ...devotionalItem, tags: ['grace'] };

            const rendered = renderCard(<ContentItemPanel contentItem={taggedItem} />);

            expect(screen.getByText('#grace')).toBeInTheDocument();

            rendered.rerender(
                <AuthProvider>
                    <ContentItemPanel
                        contentItem={withSetting(taggedItem, { showTags: false })} />
                </AuthProvider>);

            expect(screen.queryByText('#grace')).not.toBeInTheDocument();
        });

        it('should hide the tags and references where its setting says so', () => {
            renderCard(
                <ContentItemPanel
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
                <ContentItemPanel
                    contentItem={withSetting(devotionalItem, { showComments: false })}
                    onCommentsClick={vi.fn()} />);

            expect(screen.queryByText(/comments/i)).not.toBeInTheDocument();
        });

        it('should drop the author segment on a type whose setting carries none', () => {
            renderCard(
                <ContentItemPanel
                    contentItem={withSetting(devotionalItem, { hasAuthor: false })}
                    onAuthorClick={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /Author/ })).not.toBeInTheDocument();
        });

        it('should hide the assigned reactions where its setting hides them', () => {
            renderCard(
                <ContentItemPanel
                    contentItem={withSetting(quoteItem, { showReactions: false })} />);

            expect(screen.queryByText('142')).not.toBeInTheDocument();
        });
    });

    describe('status', () => {
        // The pill is the surface's opt-in, like the ribbon — and where a surface opted
        // in, EVERY status wears one, the ordinary Approved included.
        it('should wear no status pill unless the surface opted in', () => {
            renderCard(
                <ContentItemPanel
                    contentItem={{
                        ...devotionalItem,
                        approvalStatus: ApprovalStatus.Submitted
                    }} />);

            expect(screen.queryByText('In review')).not.toBeInTheDocument();
        });

        it('should wear its status pill when asked, whatever the status', () => {
            const submitted = renderCard(
                <ContentItemPanel
                    showApprovalStatus
                    contentItem={{
                        ...devotionalItem,
                        approvalStatus: ApprovalStatus.Submitted
                    }} />);

            expect(screen.getByText('In review')).toBeInTheDocument();
            submitted.unmount();

            renderCard(
                <ContentItemPanel
                    showApprovalStatus
                    contentItem={{
                        ...devotionalItem,
                        approvalStatus: ApprovalStatus.Approved
                    }} />);

            // Approved is pilled too — opting in means asking for every status
            expect(screen.getByText('Approved')).toBeInTheDocument();
        });

        it('should wear no corner ribbon unless the surface opted in', () => {
            const { container } = renderCard(
                <ContentItemPanel
                    contentItem={{
                        ...devotionalItem,
                        approvalStatus: ApprovalStatus.Approved
                    }} />);

            expect(container.querySelector('.g2h-approval-ribbon')).toBeNull();
        });

        it('should wear a corner ribbon carrying the status member name when asked', () => {
            // The stylesheet colours off the member NAME in data-approval-status — grey
            // Draft, yellow Submitted, green Approved, red Rejected — so the name IS the
            // colour contract, exactly as the type chip's palette works.
            const { container } = renderCard(
                <ContentItemPanel
                    showApprovalStatusRibbon
                    contentItem={{
                        ...devotionalItem,
                        approvalStatus: ApprovalStatus.Rejected
                    }} />);

            const ribbon = container.querySelector('.g2h-approval-ribbon');
            expect(ribbon).not.toBeNull();
            expect(ribbon!.getAttribute('data-approval-status')).toBe('Rejected');
            expect(ribbon!.textContent).toBe('Rejected');
        });

        it('should ribbon the ordinary approved case too — that is what opting in means', () => {
            const { container } = renderCard(
                <ContentItemPanel
                    showApprovalStatusRibbon
                    contentItem={{
                        ...quoteItem,
                        approvalStatus: ApprovalStatus.Approved
                    }} />);

            // On the QUOTE template — the ribbon renders on the card root, so a derived
            // template wears it without writing a line.
            const ribbon = container.querySelector('.g2h-approval-ribbon');
            expect(ribbon).not.toBeNull();
            expect(ribbon!.getAttribute('data-approval-status')).toBe('Approved');
        });
    });

    describe('event hooks', () => {
        it('should raise onContentTypeClick from the type badge', async () => {
            const onContentTypeClick = vi.fn();

            renderCard(
                <ContentItemPanel
                    contentItem={devotionalItem}
                    onContentTypeClick={onContentTypeClick} />);

            await userEvent.click(screen.getByRole('button', { name: /Devotional/ }));

            expect(onContentTypeClick).toHaveBeenCalledWith(devotionalItem);
        });

        it('should raise onTitleClick from the title', async () => {
            const onTitleClick = vi.fn();

            renderCard(
                <ContentItemPanel
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
                <ContentItemPanel contentItem={quoteItem} onTitleClick={onTitleClick} />);

            await userEvent.click(
                screen.getByRole('button', { name: new RegExp('coincidences happen') }));

            expect(onTitleClick).toHaveBeenCalledWith(quoteItem);
        });

        it('should raise onSubmittedByClick and onAuthorClick as two different people', async () => {
            const onSubmittedByClick = vi.fn();
            const onAuthorClick = vi.fn();

            renderCard(
                <ContentItemPanel
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
                <ContentItemPanel
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
                <ContentItemPanel
                    contentItem={quoteItem}
                    onCommentsClick={onCommentsClick} />);

            await userEvent.click(screen.getByRole('button', { name: /9 comments/ }));

            expect(onCommentsClick).toHaveBeenCalledWith(quoteItem);
        });

        it('should raise onReadMoreClick from the read-more affordance', async () => {
            const onReadMoreClick = vi.fn();

            renderCard(
                <ContentItemPanel
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
                <ContentItemPanel contentItem={devotionalItem} />);

            expect(screen.queryByRole('button', { name: /Share/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Save/ })).not.toBeInTheDocument();

            rendered.rerender(
                <AuthProvider>
                    <ContentItemPanel
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
                <ContentItemPanel
                    contentItem={devotionalItem}
                    onEditClick={onEditClick} />);

            // somebody else's item — no Edit however wired
            expect(screen.queryByRole('button', { name: /Edit/ })).not.toBeInTheDocument();

            rendered.rerender(
                <AuthProvider>
                    <ContentItemPanel contentItem={ownItem} onEditClick={onEditClick} />
                </AuthProvider>);

            await userEvent.click(screen.getByRole('button', { name: /Edit/ }));

            expect(onEditClick).toHaveBeenCalledWith(ownItem);
        });

        it('should offer Edit to no anonymous visitor', () => {
            renderCard(
                <ContentItemPanel contentItem={ownItem} onEditClick={vi.fn()} />);

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
                <ContentItemPanel
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
                <ContentItemPanel
                    contentItem={devotionalItem}
                    onModerateClick={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /Moderate/ }))
                .not.toBeInTheDocument();
        });

        it('should offer no Moderate to an ordinary reader', () => {
            signInAs(authState, ['Users']);

            renderCard(
                <ContentItemPanel
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
                <ContentItemPanel
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
                <ContentItemPanel
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
                <ContentItemPanel
                    contentItem={ownItem}
                    onEditClick={vi.fn()}
                    onModerateClick={vi.fn()} />);

            expect(screen.getByRole('button', { name: /Edit/ })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: /Moderate/ })).toBeInTheDocument();
        });
    });

    describe('assigned reactions', () => {
        it('should show the compact cluster with the summed total', () => {
            renderCard(<ContentItemPanel contentItem={quoteItem} />);

            expect(screen.getByText('142')).toBeInTheDocument();
            expect(screen.queryByText('All 142')).not.toBeInTheDocument();
        });

        it('should toggle to the per-reaction counts and back', async () => {
            renderCard(<ContentItemPanel contentItem={quoteItem} />);

            await userEvent.click(screen.getByRole('button', { name: 'Reaction counts' }));

            expect(screen.getByText('All 142')).toBeInTheDocument();
            expect(screen.getByText(/85/)).toBeInTheDocument();
            expect(screen.getByText(/43/)).toBeInTheDocument();

            await userEvent.click(screen.getByRole('button', { name: 'Reaction counts' }));

            expect(screen.queryByText('All 142')).not.toBeInTheDocument();
        });

        it('should show no cluster on a card given no summary', () => {
            renderCard(<ContentItemPanel contentItem={devotionalItem} />);

            expect(screen.queryByRole('button', { name: 'Reaction counts' }))
                .not.toBeInTheDocument();
        });
    });

    describe('giving a reaction', () => {
        it('should open the choices from Like and raise the selection', async () => {
            const onReactionSelected = vi.fn();

            renderCard(
                <ContentItemPanel
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
                <ContentItemPanel
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
                <ContentItemPanel
                    contentItem={quoteItem}
                    reactionOptions={reactionOptions} />);

            expect(screen.queryByRole('button', { name: /Like/ })).not.toBeInTheDocument();
        });

        it('should offer no Like when its setting does not accept reactions', () => {
            renderCard(
                <ContentItemPanel
                    contentItem={withSetting(quoteItem, { reactionsAllowed: false })}
                    reactionOptions={reactionOptions}
                    onReactionSelected={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /Like/ })).not.toBeInTheDocument();
        });

        it('should keep only the love option where its setting limits to it', async () => {
            renderCard(
                <ContentItemPanel
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
                <ContentItemPanel
                    contentItem={{ ...devotionalItem, commentCount: undefined }}
                    onCommentsClick={vi.fn()} />);

            expect(screen.getByRole('button', { name: 'Comments' })).toBeInTheDocument();
            expect(screen.queryByText(/\d+ comments/)).not.toBeInTheDocument();
        });

        it('should fall back to the content when no excerpt was written', () => {
            renderCard(
                <ContentItemPanel
                    contentItem={{ ...devotionalItem, excerpt: undefined }} />);

            expect(screen.getByText(new RegExp('far too long'))).toBeInTheDocument();
        });
    });

    describe('the section switches', () => {
        // Separate from what the settings allow: the setting says what the TYPE shows,
        // the switch says what this SURFACE has room for. A section renders only when
        // BOTH agree, and every switch defaults true — so the projection's setting stays
        // the deciding factor unless the surface specifically overrides it.
        it('should hide the tags a setting would show when the surface has them elsewhere', () => {
            const taggedItem = { ...devotionalItem, tags: ['grace'] };

            renderCard(
                <ContentItemPanel
                    contentItem={taggedItem}
                    showTagSection={false} />);

            expect(screen.queryByText('#grace')).not.toBeInTheDocument();
        });

        it('should hide the bible references the same way', () => {
            const referencedItem = {
                ...devotionalItem,
                bibleReferences: ['Romans 8:28']
            };

            renderCard(
                <ContentItemPanel
                    contentItem={referencedItem}
                    showBibleReferenceSection={false} />);

            expect(screen.queryByText('Romans 8:28')).not.toBeInTheDocument();
        });

        it('should hide the whole reaction cluster when the surface says so', () => {
            signInAs(authState);

            renderCard(
                <ContentItemPanel
                    contentItem={quoteItem}
                    reactionOptions={reactionOptions}
                    showReactionSection={false}
                    onReactionSelected={vi.fn()} />);

            expect(screen.queryByRole('button', { name: 'Like' })).not.toBeInTheDocument();
        });

        it('should hide comments, share and save on their switches', () => {
            renderCard(
                <ContentItemPanel
                    contentItem={quoteItem}
                    showCommentsSection={false}
                    showShareSection={false}
                    showSaveSection={false}
                    onCommentsClick={vi.fn()}
                    onShareClick={vi.fn()}
                    onSaveClick={vi.fn()} />);

            expect(screen.queryByRole('button', { name: /comment/i }))
                .not.toBeInTheDocument();

            expect(screen.queryByRole('button', { name: 'Share' })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
        });

        it('should still let the setting decide when the surface says nothing', () => {
            // given: switches untouched, setting shows tags
            const taggedItem = { ...devotionalItem, tags: ['grace'] };

            renderCard(<ContentItemPanel contentItem={taggedItem} />);

            // then: default true — the projection's setting is the deciding factor
            expect(screen.getByText('#grace')).toBeInTheDocument();
        });
    });

    describe('the merged faces', () => {
        // The merge: no separate detail component. A settings collection and no item is
        // the ADD surface; an owner taking Edit on a page that listens on onModified gets
        // the editor IN PLACE; a page that wired onEditClick alone still gets its event.
        const ownItem: ContentItemSearchItem = {
            ...devotionalItem,
            submittedById: 'user-1',
            sharePermission: ''
        };

        it('should render the add face when handed settings and no item', () => {
            // given
            signInAs(authState);

            // when
            renderCard(
                <ContentItemPanel
                    contentItemSettingCollection={[devotionalSetting]} />);

            // then: the picker and the submit pair — the contribution form, right here
            expect(screen.getByText('What are you sharing?')).toBeInTheDocument();

            expect(screen.getByRole('button', { name: 'Submit for review' }))
                .toBeInTheDocument();
        });

        it('should open the editor in place when the page listens on onModified', async () => {
            // given: the owner, on a surface that allows editing and owns persistence
            signInAs(authState);
            const onModified = vi.fn();

            renderCard(
                <ContentItemPanel
                    contentItem={ownItem}
                    isEditingAllowed
                    onModified={onModified} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

            // then: the card became the editor, seeded from the element
            expect(screen.getByLabelText(/Title/)).toHaveValue('Walking daily in grace');
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();

            // when: the editor is abandoned
            await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

            // then: the view face is back
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument();
        });

        it('should close the editor on Save and hand the amendments to the page', async () => {
            // given: an owned item on an owned basis (no permission note to demand)
            signInAs(authState);
            const onModified = vi.fn();

            renderCard(
                <ContentItemPanel
                    contentItem={ownItem}
                    isEditingAllowed
                    onModified={onModified} />);

            await userEvent.click(screen.getByRole('button', { name: 'Edit' }));
            await userEvent.clear(screen.getByLabelText(/Title/));
            await userEvent.type(screen.getByLabelText(/Title/), 'Walking hourly in grace');

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Save' }));

            // then: the editor closed like Cancel does, and the amendments went UP —
            // the page persists and swaps the element, which is what the card shows
            expect(screen.queryByLabelText(/Title/)).not.toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument();

            expect(onModified).toHaveBeenCalledWith(expect.objectContaining({
                title: 'Walking hourly in grace'
            }));
        });

        it('should discard the draft on Cancel — reopening shows the original', async () => {
            // given
            signInAs(authState);

            renderCard(
                <ContentItemPanel
                    contentItem={ownItem}
                    isEditingAllowed
                    onModified={vi.fn()} />);

            await userEvent.click(screen.getByRole('button', { name: 'Edit' }));
            await userEvent.clear(screen.getByLabelText(/Title/));
            await userEvent.type(screen.getByLabelText(/Title/), 'Abandoned words');

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));
            await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

            // then
            expect(screen.getByLabelText(/Title/)).toHaveValue('Walking daily in grace');
        });

        it('should land straight on the editor when the page asks with mode', () => {
            // given
            signInAs(authState);

            // when
            renderCard(
                <ContentItemPanel
                    contentItem={ownItem}
                    mode="edit"
                    isEditingAllowed
                    onModified={vi.fn()} />);

            // then
            expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
        });

        it('should keep routing Edit to the page when only onEditClick is wired', async () => {
            // given: a feed page that routes to its own edit surface
            signInAs(authState);
            const onEditClick = vi.fn();

            renderCard(
                <ContentItemPanel
                    contentItem={ownItem}
                    onEditClick={onEditClick} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Edit' }));

            // then: the event fired and no editor opened here
            expect(onEditClick).toHaveBeenCalledWith(ownItem);
            expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
        });

        it('should never fall into add from a list — an item always renders its card', () => {
            // given: a card handed BOTH an element and a collection (a detail page does)
            signInAs(authState);

            // when
            renderCard(
                <ContentItemPanel
                    contentItem={ownItem}
                    contentItemSettingCollection={[devotionalSetting]} />);

            // then
            expect(screen.queryByText('What are you sharing?')).not.toBeInTheDocument();
            expect(screen.getByText('Walking daily in grace')).toBeInTheDocument();
        });
    });
});
