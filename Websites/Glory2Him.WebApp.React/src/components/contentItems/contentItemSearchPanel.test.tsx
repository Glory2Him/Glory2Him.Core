import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ContentItemSearchPanel } from './contentItemSearchPanel';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    ContentItemSearchItem,
    ShareabilityBasis,
    emptyContentItemSearchCriteria
} from '../../models/components/contentItems/contentItemSearchItem';

// The composition: the bar, the results and the filter semantics of the card hooks. The card's
// own rendering is contentItemPanel.test.tsx's subject; here the cards matter only as the
// places the filter clicks come from. No router — the family navigates nowhere itself.
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

const defaultSettings: ReadonlyArray<ContentItemSetting> = [
    settingFor(ContentType.Quote, 'Quote'),
    settingFor(ContentType.Devotional, 'Devotional'),
    settingFor(ContentType.BibleStudy, 'Bible Study')
];

const devotionalItem: ContentItemSearchItem = {
    id: 'devotional-1',
    contentType: ContentType.Devotional,
    contentItemSetting: settingFor(ContentType.Devotional, 'Devotional'),
    title: 'Walking daily in grace',
    author: 'Miriam Vale',
    content: 'Grace is the daily air.',
    excerpt: 'Grace is the daily air.',
    submittedById: 'account-joan',
    submittedByName: 'Joan',
    tags: ['grace'],
    publishedDate: new Date(2026, 6, 3)
};

const quoteItem: ContentItemSearchItem = {
    id: 'quote-1',
    contentType: ContentType.Quote,
    contentItemSetting: settingFor(ContentType.Quote, 'Quote'),
    author: 'D. L. Moody',
    content: 'Character is what you are in the dark.',
    publishedDate: new Date(2026, 6, 18)
};

// jsdom DOES define IntersectionObserver, and it never intersects — so a test that wants the
// observer path installs one it can fire, and a test that wants the fallback takes the global
// away. Both paths matter: the button is what a browser without the observer gets.
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

const removeIntersectionObserver = () =>
    vi.stubGlobal('IntersectionObserver', undefined);

afterEach(() => {
    vi.unstubAllGlobals();
});

describe('ContentItemSearchPanel', () => {
    describe('the bar', () => {
        it('should raise onSearch with everything the boxes were set to', async () => {
            const onSearch = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    categorySettingCollection={defaultSettings}
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
                author: 'Moody',
                submittedBy: null,
                tags: [],
                tagMatchMode: 'any',
                shareabilityBasis: null
            });
        });

        it('should offer every default type in the order the administrator set', async () => {
            render(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    categorySettingCollection={[
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
        // write one of, and an override belongs to one item and is never a category.
        it('should offer non-contributable types and never an override', async () => {
            render(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    categorySettingCollection={[
                        settingFor(ContentType.BlogPost, 'Blog Post', {
                            isAvailableAsGeneralUserContribution: false
                        }),
                        settingFor(ContentType.Devotional, 'Advent Note', {
                            id: 'override-1',
                            contentItemId: 'devotional-1'
                        })
                    ]} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Advanced search options' }));

            const category = screen.getByLabelText('Category');

            expect(within(category).getByRole('option', { name: 'Blog Post' }))
                .toBeInTheDocument();

            expect(within(category).queryByRole('option', { name: 'Advent Note' }))
                .not.toBeInTheDocument();
        });

        it('should offer the full advanced grid — the two people, the basis, the tags', async () => {
            render(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    categorySettingCollection={defaultSettings} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Advanced search options' }));

            expect(screen.getByLabelText('Category')).toBeInTheDocument();
            expect(screen.getByLabelText('Author')).toBeInTheDocument();
            expect(screen.getByLabelText('Submitted by')).toBeInTheDocument();
            expect(screen.getByLabelText('Shareability')).toBeInTheDocument();

            expect(screen.getByLabelText('Type a tag and press Enter'))
                .toBeInTheDocument();
        });

        it('should commit the typed submitted-by, basis and entered tags on Search', async () => {
            const onSearch = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    categorySettingCollection={defaultSettings}
                    onSearch={onSearch} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Advanced search options' }));

            await userEvent.type(screen.getByLabelText('Submitted by'), 'Joan');

            await userEvent.selectOptions(
                screen.getByLabelText('Shareability'),
                String(ShareabilityBasis.PublicDomain));

            const tagBox = screen.getByLabelText('Type a tag and press Enter');
            await userEvent.type(tagBox, 'grace{Enter}');
            await userEvent.type(tagBox, 'healing{Enter}');

            await userEvent.click(screen.getByRole('button', { name: 'All' }));
            await userEvent.click(screen.getByRole('button', { name: /Search/ }));

            // A TYPED submitted-by carries no account id — only a pill click can — so the
            // read narrows on the name's id only when there is one.
            expect(onSearch).toHaveBeenCalledWith(expect.objectContaining({
                submittedBy: { id: '', name: 'Joan' },
                shareabilityBasis: ShareabilityBasis.PublicDomain,
                tags: ['grace', 'healing'],
                tagMatchMode: 'all'
            }));
        });

        it('should seed the boxes from the criteria it was landed with', async () => {
            render(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    categorySettingCollection={defaultSettings}
                    criteria={{
                        ...emptyContentItemSearchCriteria,
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
            const rendered = render(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    categorySettingCollection={defaultSettings}
                    criteria={{ ...emptyContentItemSearchCriteria, query: 'grace' }} />);

            rendered.rerender(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    categorySettingCollection={defaultSettings}
                    criteria={{ ...emptyContentItemSearchCriteria, query: 'mercy' }} />);

            expect(screen.getByRole('searchbox')).toHaveValue('mercy');
        });

        it('should leave the list alone when the bar is switched off', () => {
            render(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    categorySettingCollection={defaultSettings}
                    showSearchBar={false} />);

            expect(screen.queryByRole('searchbox')).not.toBeInTheDocument();

            expect(screen.getByText(new RegExp('Character is what you are')))
                .toBeInTheDocument();
        });
    });

    describe('the filter hooks', () => {
        it('should toggle the category on when the type badge is clicked', async () => {
            const onSearch = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    categorySettingCollection={defaultSettings}
                    criteria={emptyContentItemSearchCriteria}
                    onSearch={onSearch} />);

            await userEvent.click(screen.getByRole('button', { name: /Devotional/ }));

            expect(onSearch).toHaveBeenCalledWith(
                expect.objectContaining({ contentType: ContentType.Devotional }));
        });

        it('should toggle the category back off when it is already this type', async () => {
            const onSearch = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    categorySettingCollection={defaultSettings}
                    criteria={{
                        ...emptyContentItemSearchCriteria,
                        contentType: ContentType.Devotional
                    }}
                    onSearch={onSearch} />);

            await userEvent.click(screen.getByRole('button', { name: /Devotional/ }));

            expect(onSearch).toHaveBeenCalledWith(
                expect.objectContaining({ contentType: null }));
        });

        it('should set the submitted-by criterion from the byline', async () => {
            const onSearch = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    categorySettingCollection={defaultSettings}
                    criteria={emptyContentItemSearchCriteria}
                    onSearch={onSearch} />);

            await userEvent.click(screen.getByRole('button', { name: /Submitted by/ }));

            expect(onSearch).toHaveBeenCalledWith(
                expect.objectContaining({
                    submittedBy: { id: 'account-joan', name: 'Joan' }
                }));
        });

        it('should set the author criterion from the meta row', async () => {
            const onSearch = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    categorySettingCollection={defaultSettings}
                    criteria={emptyContentItemSearchCriteria}
                    onSearch={onSearch} />);

            await userEvent.click(screen.getByRole('button', { name: /Author/ }));

            expect(onSearch).toHaveBeenCalledWith(
                expect.objectContaining({ author: 'Miriam Vale' }));
        });

        it('should set the tag criterion from a pill', async () => {
            const onSearch = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    categorySettingCollection={defaultSettings}
                    criteria={emptyContentItemSearchCriteria}
                    onSearch={onSearch} />);

            await userEvent.click(screen.getByRole('button', { name: '#grace' }));

            expect(onSearch).toHaveBeenCalledWith(
                expect.objectContaining({ tags: ['grace'] }));
        });

        // Invisible state is the failure mode: a narrowed list with nothing on screen saying
        // why. The clicked criteria wear chips, and the chips take them off again.
        it('should wear the clicked criteria as chips and clear them from there', async () => {
            const onSearch = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[devotionalItem]}
                    categorySettingCollection={defaultSettings}
                    criteria={{
                        ...emptyContentItemSearchCriteria,
                        submittedBy: { id: 'account-joan', name: 'Joan' },
                        tags: ['grace']
                    }}
                    onSearch={onSearch} />);

            // By its remove affordance, because the card's own byline reads the same words —
            // the chip is the one that offers to take the filter off.
            const chips = screen.getAllByTitle('Remove this filter');

            expect(chips.map((chip) => chip.textContent)).toEqual([
                'Submitted by Joan',
                'Tag #grace'
            ]);

            await userEvent.click(screen.getByRole('button', { name: /Tag #grace/ }));

            expect(onSearch).toHaveBeenCalledWith(
                expect.objectContaining({
                    tags: [],
                    submittedBy: { id: 'account-joan', name: 'Joan' }
                }));
        });
    });

    describe('loading and emptiness', () => {
        // A re-search must not flash "nothing found" on its way to results.
        it('should hold the list back rather than emptying it while the first page loads', () => {
            render(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    categorySettingCollection={defaultSettings}
                    isLoading />);

            expect(screen.getByText('Loading…')).toBeInTheDocument();
            expect(screen.queryByText('Nothing matched that search.')).not.toBeInTheDocument();
        });

        it('should say so when nothing matched', () => {
            render(
                <ContentItemSearchPanel
                    contentItemCollection={[]}
                    categorySettingCollection={defaultSettings} />);

            expect(screen.getByText('Nothing matched that search.')).toBeInTheDocument();
        });
    });

    describe('scrolling', () => {
        it('should ask for the next page when the foot of the list comes into view', async () => {
            const observer = installIntersectionObserver();
            const onLoadMore = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem, devotionalItem]}
                    categorySettingCollection={defaultSettings}
                    hasMore
                    onLoadMore={onLoadMore} />);

            observer.intersect();

            await waitFor(() => expect(onLoadMore).toHaveBeenCalledTimes(1));
        });

        it('should ask for nothing once there is nothing left', () => {
            const observer = installIntersectionObserver();
            const onLoadMore = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    categorySettingCollection={defaultSettings}
                    hasMore={false}
                    onLoadMore={onLoadMore} />);

            observer.intersect();

            expect(onLoadMore).not.toHaveBeenCalled();
        });

        // One scroll is one fetch: the observer is torn down while a page is in flight.
        it('should ask for nothing while a page is already on its way', () => {
            const observer = installIntersectionObserver();
            const onLoadMore = vi.fn();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    categorySettingCollection={defaultSettings}
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

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    categorySettingCollection={defaultSettings}
                    hasMore
                    onLoadMore={onLoadMore} />);

            await userEvent.click(screen.getByRole('button', { name: 'Load more' }));

            expect(onLoadMore).toHaveBeenCalledTimes(1);
        });

        it('should offer no button where the observer does the asking', () => {
            installIntersectionObserver();

            render(
                <ContentItemSearchPanel
                    contentItemCollection={[quoteItem]}
                    categorySettingCollection={defaultSettings}
                    hasMore
                    onLoadMore={vi.fn()} />);

            expect(screen.queryByRole('button', { name: 'Load more' })).not.toBeInTheDocument();
        });
    });
});
