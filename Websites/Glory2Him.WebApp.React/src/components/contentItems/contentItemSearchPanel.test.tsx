import { ReactElement, ReactNode } from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ContentItemSearchPanel } from './contentItemSearchPanel';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    ApprovalStatus,
    ContentItemReactionOption,
    ContentItemSearchItem
} from '../../models/components/contentItems/contentItemSearchItem';

// The panel routes with <Link>, and TagPillList does too, so every render needs a router. It has
// no role gates of its own — nothing here is an authorization decision — so unlike its detail
// sibling it needs no auth provider.
const wrapped = (ui: ReactNode): ReactElement => (
    <MemoryRouter initialEntries={['/Journal']}>{ui}</MemoryRouter>
);

const renderPanel = (ui: ReactNode) => render(wrapped(ui));

const settingFor = (
    contentType: ContentType,
    contentTypeName: string,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
        id: `setting-${contentType}`,
        contentType,
        contentItemId: null,
        contentTypeName,
        contentTypeDescription: `A ${contentTypeName.toLowerCase()}`,
        contentTypeIconCssClass: 'bi-chat-quote',
        // The seed orders each type by its enum member, so a fixture that does the same puts the
        // Category options in the order the real box shows them.
        sortOrder: contentType,
        hasTitle: true,
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

const defaultSettings: ReadonlyArray<ContentItemSetting> = [
    settingFor(ContentType.Quote, 'Quote', { hasTitle: false }),
    settingFor(ContentType.Devotional, 'Devotional'),
    settingFor(ContentType.BibleStudy, 'Bible Study')
];

const quoteItem: ContentItemSearchItem = {
    id: 'quote-1',
    contentType: ContentType.Quote,
    author: 'D. L. Moody',
    content: 'Character is what you are in the dark.',
    contributorName: 'Bryan',
    publishedDate: new Date(2026, 6, 18),
    reactionCount: 142,
    commentCount: 9
};

const devotionalItem: ContentItemSearchItem = {
    id: 'devotional-1',
    contentType: ContentType.Devotional,
    title: 'Walking daily in grace',
    content: 'The whole devotional, which is far too long to sit in a list.',
    excerpt: 'Grace is not a one-time event but the daily air the believer breathes.',
    imageUrl: 'https://example.test/grace.jpg',
    contributorName: 'Joan',
    publishedDate: new Date(2026, 6, 3),
    reactionCount: 87,
    commentCount: 5
};

const reactionOptions: ReadonlyArray<ContentItemReactionOption> = [
    { label: 'Amen', glyph: '🙌' },
    { label: 'Love', glyph: '❤️', isLove: true },
    { label: 'Joy', glyph: '😄' }
];

// jsdom DOES define IntersectionObserver, and it never intersects — so a test that wants the
// observer path has to install one it can fire, and a test that wants the fallback has to take
// the global away. Both paths matter: the button is what a browser without the observer gets.
const installIntersectionObserver = () => {
    const observed: Element[] = [];
    let trigger: (() => void) | null = null;

    class StubIntersectionObserver {
        constructor(private readonly callback: IntersectionObserverCallback) {
            trigger = () =>
                this.callback(
                    observed.map((target) => ({ isIntersecting: true, target })) as never,
                    this as never);
        }

        observe(target: Element) {
            observed.push(target);
        }

        disconnect() {
            observed.length = 0;
        }

        unobserve() { }
        takeRecords() { return []; }
    }

    vi.stubGlobal('IntersectionObserver', StubIntersectionObserver);

    return { intersect: () => trigger?.() };
};

// A browser that has none — an older one, or a page where the polyfill did not load.
const removeIntersectionObserver = () =>
    vi.stubGlobal('IntersectionObserver', undefined);

afterEach(() => {
    vi.unstubAllGlobals();
});

describe('ContentItemSearchPanel', () => {
    describe('the two renders', () => {
        it('should show a quote whole rather than excerpting it', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByText('Character is what you are in the dark.'))
                .toBeInTheDocument();

            expect(screen.getByText('— D. L. Moody')).toBeInTheDocument();
        });

        it('should lead a non-quote with its title and its excerpt', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByRole('link', { name: 'Walking daily in grace' }))
                .toHaveAttribute('href', '/posts/devotional-1');

            expect(screen.getByText(devotionalItem.excerpt as string)).toBeInTheDocument();
            expect(screen.queryByText(devotionalItem.content)).not.toBeInTheDocument();
        });

        it('should fall back to the content when no excerpt was written', () => {
            const withoutExcerpt = { ...devotionalItem, excerpt: undefined };

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[withoutExcerpt]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByText(devotionalItem.content)).toBeInTheDocument();
        });

        // A story's author and its contributor are two different people, so the row credits the
        // author on its own line rather than folding them together in the byline.
        it('should credit the author of the words on a row that carries one', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[{ ...devotionalItem, author: 'Miriam Vale' }]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByText('by Miriam Vale')).toBeInTheDocument();
        });

        it('should credit no author on a type whose setting carries none', () => {
            const noAuthor = [
                settingFor(ContentType.Devotional, 'Devotional', { hasAuthor: false })
            ];

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[{ ...devotionalItem, author: 'Miriam Vale' }]}
                    contentItemSettingCollection={noAuthor} />);

            expect(screen.queryByText('by Miriam Vale')).not.toBeInTheDocument();
        });

        it('should name the card by the setting rather than by the enum member', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[
                        { ...devotionalItem, contentType: ContentType.BibleStudy }]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByText('Bible Study')).toBeInTheDocument();
        });

        // §6.4 / §12.5.2 rules 1-2: an item-level override wins over the type default, and only
        // for the item it belongs to.
        it('should apply an override to its own item and to no other', () => {
            const overridden = settingFor(ContentType.Devotional, 'Advent Note', {
                id: 'override-1',
                contentItemId: 'devotional-1'
            });

            const second: ContentItemSearchItem = {
                ...devotionalItem,
                id: 'devotional-2',
                title: 'Another devotional'
            };

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem, second]}
                    contentItemSettingCollection={[...defaultSettings, overridden]} />);

            expect(screen.getByText('Advent Note')).toBeInTheDocument();
            expect(screen.getByText('Devotional')).toBeInTheDocument();
        });

        it('should render a card with no image without a thumbnail', () => {
            const rendered = renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[{ ...devotionalItem, imageUrl: undefined }]}
                    contentItemSettingCollection={defaultSettings} />);

            // By tag rather than by role: the thumbnail is decorative (alt=""), and the byline's
            // initials avatar carries role="img" whether or not there is a card image.
            expect(rendered.container.querySelector('img')).toBeNull();

            // The badge moves off the missing image and into the body rather than vanishing.
            expect(screen.getByText('Devotional')).toBeInTheDocument();
        });
    });

    describe('status', () => {
        it('should wear its status while it is not yet public', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[
                        { ...devotionalItem, approvalStatus: ApprovalStatus.Submitted }]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByText('In review')).toBeInTheDocument();
        });

        it('should say nothing about an approved item, which is the ordinary case', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[
                        { ...devotionalItem, approvalStatus: ApprovalStatus.Approved }]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.queryByText('In review')).not.toBeInTheDocument();
            expect(screen.queryByText('Draft')).not.toBeInTheDocument();
        });
    });

    describe('reacting', () => {
        it('should offer a reaction on a quote, whose whole content is on screen', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings}
                    reactionOptions={reactionOptions}
                    onReacted={vi.fn()} />);

            expect(screen.getByRole('button', { name: 'Amen' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Love' })).toBeInTheDocument();
        });

        it('should offer no reaction on a type the reader has only seen an excerpt of', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    contentItemSettingCollection={defaultSettings}
                    reactionOptions={reactionOptions}
                    onReacted={vi.fn()} />);

            expect(screen.queryByRole('button', { name: 'Amen' })).not.toBeInTheDocument();

            expect(screen.getByRole('link', { name: 'Read and react' }))
                .toHaveAttribute('href', '/posts/devotional-1');
        });

        it('should raise onReacted with the item and the option chosen', async () => {
            const onReacted = vi.fn();

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings}
                    reactionOptions={reactionOptions}
                    onReacted={onReacted} />);

            await userEvent.click(screen.getByRole('button', { name: 'Love' }));

            expect(onReacted).toHaveBeenCalledWith(
                quoteItem,
                expect.objectContaining({ label: 'Love' }));
        });

        // A button whose event goes nowhere is worse than no button.
        it('should offer nothing when nobody is listening for the reaction', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings}
                    reactionOptions={reactionOptions} />);

            expect(screen.queryByRole('button', { name: 'Love' })).not.toBeInTheDocument();
        });

        it('should offer nothing when the type does not accept reactions', () => {
            const noReactions = [
                settingFor(ContentType.Quote, 'Quote', {
                    hasTitle: false,
                    reactionsAllowed: false
                })
            ];

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={noReactions}
                    reactionOptions={reactionOptions}
                    onReacted={vi.fn()} />);

            expect(screen.queryByRole('button', { name: 'Love' })).not.toBeInTheDocument();
        });

        it('should keep only the love option where the setting limits it to one', () => {
            const loveOnly = [
                settingFor(ContentType.Quote, 'Quote', {
                    hasTitle: false,
                    limitReactionsToLoveOnly: true
                })
            ];

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={loveOnly}
                    reactionOptions={reactionOptions}
                    onReacted={vi.fn()} />);

            expect(screen.getByRole('button', { name: 'Love' })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Amen' })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Joy' })).not.toBeInTheDocument();
        });

        it('should show which reaction this reader already gave', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[{ ...quoteItem, viewerReactionLabel: 'Joy' }]}
                    contentItemSettingCollection={defaultSettings}
                    reactionOptions={reactionOptions}
                    onReacted={vi.fn()} />);

            expect(screen.getByRole('button', { name: 'Joy' }))
                .toHaveAttribute('aria-pressed', 'true');

            expect(screen.getByRole('button', { name: 'Love' }))
                .toHaveAttribute('aria-pressed', 'false');
        });
    });

    describe('commenting', () => {
        it('should send a comment on a quote into the detail view', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByRole('link', { name: /9 comments/ }))
                .toHaveAttribute('href', '/posts/quote-1');
        });

        it('should send a comment on every other type into the detail view too', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByRole('link', { name: /5 comments/ }))
                .toHaveAttribute('href', '/posts/devotional-1');
        });

        it('should say nothing about comments on a type that does not show them', () => {
            const noComments = [
                settingFor(ContentType.Devotional, 'Devotional', { showComments: false })
            ];

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    contentItemSettingCollection={noComments} />);

            expect(screen.queryByText(/comments/)).not.toBeInTheDocument();
        });

        it('should claim no count it was not given', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[{ ...devotionalItem, commentCount: undefined }]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.queryByText(/comments/)).not.toBeInTheDocument();
        });
    });

    describe('pills', () => {
        it('should render the tags and references it was handed', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[{
                        ...devotionalItem,
                        tags: ['grace', 'discipleship'],
                        bibleReferences: ['Ephesians 2:8-9']
                    }]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByText('#grace')).toBeInTheDocument();
            expect(screen.getByText('#discipleship')).toBeInTheDocument();
            expect(screen.getByText('Ephesians 2:8-9')).toBeInTheDocument();
        });

        it('should render no pill row for a type that hides them', () => {
            const hidden = [
                settingFor(ContentType.Devotional, 'Devotional', {
                    showTags: false,
                    showBibleReferences: false
                })
            ];

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[{
                        ...devotionalItem,
                        tags: ['grace'],
                        bibleReferences: ['Ephesians 2:8-9']
                    }]}
                    contentItemSettingCollection={hidden} />);

            expect(screen.queryByText('#grace')).not.toBeInTheDocument();
            expect(screen.queryByText('Ephesians 2:8-9')).not.toBeInTheDocument();
        });
    });

    describe('searching', () => {
        it('should raise onSearch with everything the boxes were set to', async () => {
            const onSearch = vi.fn();

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings}
                    onSearch={onSearch} />);

            await userEvent.type(
                screen.getByRole('searchbox', { name: 'Search posts, authors and topics' }),
                'grace');

            await userEvent.click(
                screen.getByRole('button', { name: 'Advanced search options' }));

            await userEvent.selectOptions(
                screen.getByLabelText('Category'), String(ContentType.Devotional));

            await userEvent.type(screen.getByLabelText('Author'), 'Moody');
            await userEvent.click(screen.getByRole('button', { name: /Search/ }));

            expect(onSearch).toHaveBeenCalledWith({
                query: 'grace',
                contentType: ContentType.Devotional,
                author: 'Moody'
            });
        });

        it('should offer every default type in the order the administrator set', async () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    contentItemSettingCollection={[
                        settingFor(ContentType.BibleStudy, 'Bible Study', { sortOrder: 9 }),
                        settingFor(ContentType.Quote, 'Quote', { sortOrder: 1 }),
                        settingFor(ContentType.Devotional, 'Devotional', { sortOrder: 5 })
                    ]} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Advanced search options' }));

            const options = within(screen.getByLabelText('Category'))
                .getAllByRole('option')
                .map((option) => option.textContent);

            expect(options).toEqual(['Any category', 'Quote', 'Devotional', 'Bible Study']);
        });

        // Searching is not contributing: a reader must be able to narrow to a type they may not
        // write one of.
        it('should offer a type nobody may contribute under', async () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    contentItemSettingCollection={[
                        settingFor(ContentType.BlogPost, 'Blog Post', {
                            isAvailableAsGeneralUserContribution: false
                        })
                    ]} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Advanced search options' }));

            expect(within(screen.getByLabelText('Category'))
                .getByRole('option', { name: 'Blog Post' })).toBeInTheDocument();
        });

        it('should offer no override as a category', async () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    contentItemSettingCollection={[
                        settingFor(ContentType.Quote, 'Quote'),
                        settingFor(ContentType.Devotional, 'Advent Note', {
                            id: 'override-1',
                            contentItemId: 'devotional-1'
                        })
                    ]} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Advanced search options' }));

            expect(within(screen.getByLabelText('Category'))
                .queryByRole('option', { name: 'Advent Note' })).not.toBeInTheDocument();
        });

        // Associations have no HTTP exposer yet (#318), and a tag filter over the pages already
        // loaded would quietly lie on an infinite scroll.
        it('should offer no tag filter until associations can be read', async () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    contentItemSettingCollection={defaultSettings} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Advanced search options' }));

            expect(screen.queryByLabelText(/tag/i)).not.toBeInTheDocument();
        });

        it('should seed the boxes from the criteria it was landed with', async () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    contentItemSettingCollection={defaultSettings}
                    criteria={{
                        query: 'moody',
                        contentType: ContentType.Quote,
                        author: 'D. L.'
                    }} />);

            expect(screen.getByRole('searchbox')).toHaveValue('moody');

            await userEvent.click(
                screen.getByRole('button', { name: 'Advanced search options' }));

            expect(screen.getByLabelText('Category')).toHaveValue(String(ContentType.Quote));
            expect(screen.getByLabelText('Author')).toHaveValue('D. L.');
        });

        it('should follow the criteria when something else navigates here', () => {
            const rendered = renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    contentItemSettingCollection={defaultSettings}
                    criteria={{ query: 'grace', contentType: null, author: '' }} />);

            rendered.rerender(wrapped(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    contentItemSettingCollection={defaultSettings}
                    criteria={{ query: 'mercy', contentType: null, author: '' }} />));

            expect(screen.getByRole('searchbox')).toHaveValue('mercy');
        });

        it('should leave the list alone when the bar is switched off', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings}
                    showSearchBar={false} />);

            expect(screen.queryByRole('searchbox')).not.toBeInTheDocument();

            expect(screen.getByText('Character is what you are in the dark.'))
                .toBeInTheDocument();
        });
    });

    describe('loading and emptiness', () => {
        // A re-search must not flash "nothing found" on its way to results.
        it('should hold the list back rather than emptying it while the first page loads', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    contentItemSettingCollection={defaultSettings}
                    isLoading />);

            expect(screen.getByText('Loading…')).toBeInTheDocument();
            expect(screen.queryByText('Nothing matched that search.')).not.toBeInTheDocument();
        });

        it('should say so when nothing matched', () => {
            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    contentItemSettingCollection={defaultSettings} />);

            expect(screen.getByText('Nothing matched that search.')).toBeInTheDocument();
        });
    });

    describe('scrolling', () => {
        it('should ask for the next page when the foot of the list comes into view', async () => {
            const observer = installIntersectionObserver();
            const onLoadMore = vi.fn();

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem, devotionalItem]}
                    contentItemSettingCollection={defaultSettings}
                    hasMore
                    onLoadMore={onLoadMore} />);

            observer.intersect();

            await waitFor(() => expect(onLoadMore).toHaveBeenCalledTimes(1));
        });

        it('should ask for nothing once there is nothing left', () => {
            const observer = installIntersectionObserver();
            const onLoadMore = vi.fn();

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings}
                    hasMore={false}
                    onLoadMore={onLoadMore} />);

            observer.intersect();

            expect(onLoadMore).not.toHaveBeenCalled();
        });

        // One scroll is one fetch: the observer is torn down while a page is in flight.
        it('should ask for nothing while a page is already on its way', () => {
            const observer = installIntersectionObserver();
            const onLoadMore = vi.fn();

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings}
                    hasMore
                    isLoadingMore
                    onLoadMore={onLoadMore} />);

            observer.intersect();

            expect(onLoadMore).not.toHaveBeenCalled();
            expect(screen.getByText('Loading more…')).toBeInTheDocument();
        });

        it('should offer a button where the observer is not available', async () => {
            removeIntersectionObserver();
            const onLoadMore = vi.fn();

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings}
                    hasMore
                    onLoadMore={onLoadMore} />);

            await userEvent.click(screen.getByRole('button', { name: 'Load more' }));

            expect(onLoadMore).toHaveBeenCalledTimes(1);
        });

        it('should offer no button where the observer does the asking', () => {
            installIntersectionObserver();

            renderPanel(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    contentItemSettingCollection={defaultSettings}
                    hasMore
                    onLoadMore={vi.fn()} />);

            expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument();
        });
    });
});
