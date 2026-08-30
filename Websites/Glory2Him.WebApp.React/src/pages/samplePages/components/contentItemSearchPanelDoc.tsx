import { useMemo, useState } from 'react';
import { ContentItemSearchPanel } from '../../../components/contentItems/contentItemSearchPanel';

import {
    ContentItemSetting
} from '../../../models/foundations/contentItemSettings/contentItemSetting';

import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';

import {
    ApprovalStatus,
    ContentItemReactionOption,
    ContentItemSearchCriteria,
    ContentItemSearchItem,
    emptyContentItemSearchCriteria
} from '../../../models/components/contentItems/contentItemSearchItem';

import { useDocumentTitle } from '../../useDocumentTitle';

import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DocSection,
    LiveDemo,
    PropsTable
} from './shared/componentDoc';

const minimalSample = `
import { ContentItemSearchPanel } from '../../components/contentItems/contentItemSearchPanel';

// A public feed. The PAGE owns the read, the paging and the persistence.
<ContentItemSearchPanel
    contentItemCollection={items}
    contentItemSettingCollection={defaultSettings}
    criteria={criteria}
    onSearch={setCriteria}
    isLoading={isLoading}
    isLoadingMore={isFetchingNextPage}
    hasMore={hasNextPage}
    onLoadMore={fetchNextPage}
    reactionOptions={reactionOptions}
    onReacted={(item, reaction) => reactAsync(item, reaction)} />

// A surface that has already decided what it is showing — a topic's children,
// a contributor's own rows — turns the bar off and keeps the list.
<ContentItemSearchPanel
    contentItemCollection={myContributions}
    contentItemSettingCollection={defaultSettings}
    showSearchBar={false} />
`;

const pagingSample = `
// THE PANEL DOES NOT FETCH. It raises onLoadMore when the foot of the list comes into view and
// renders what it is handed next. The page owns the query:

const { data, isLoading, hasNextPage, isFetchingNextPage, fetchNextPage } =
    contentItemService.useSearchContentItems(criteria);

// ...and the broker pages with OData, asking for ONE ROW BEYOND the page and dropping it. The
// reads answer with a plain array and no total in it, so the extra row is the only thing that
// separates a full last page from a page with more behind it. The host caps [EnableQuery] at
// OData:PageSize = 50, so a page size stays well under it.

parameters.set('$orderby', 'createdWhen desc');
parameters.set('$skip', String(pageIndex * pageSize));
parameters.set('$top', String(pageSize + 1));

// $filter parses the enum MEMBER NAME, while the JSON body carries the number:
//     contentType eq 'Quote'
`;

const engagementSample = `
// WHERE AN OPINION MAY BE GIVEN follows from what the reader can see.
//
//   Quote            whole content on screen  →  react in place (onReacted)
//   Everything else  an excerpt on screen     →  "Read and react" into /posts/{id}
//   Comments         both renders             →  the count links into /posts/{id}
//
// And every one of those is still gated on the item's own effective ContentItemSetting:
//
//   reactionsAllowed && showReactions   →  a reaction row at all
//   limitReactionsToLoveOnly            →  the isLove option and nothing else
//   showComments                        →  the comment count
//   showTags / showBibleReferences      →  the pill row
//
// onReacted is RAISED, never posted. The consumer writes it, decides whether a repeat click is
// a retraction, and hands back a new collection.
`;

const settingFor = (
    contentType: ContentType,
    contentTypeName: string,
    contentTypeIconCssClass: string,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
        id: `setting-${contentType}`,
        contentType,
        contentItemId: null,
        contentTypeName,
        contentTypeDescription: contentTypeName,
        contentTypeIconCssClass,
        sortOrder: contentType,
        hasTitle: contentType !== ContentType.Quote,
        hasAuthor: true,
        isAvailableAsGeneralUserContribution: true,
        tagsAllowed: true,
        showTags: true,
        reactionsAllowed: true,
        showReactions: true,
        linksAllowed: false,
        showLinks: false,
        attachmentsAllowed: false,
        showAttachments: false,
        commentsAllowed: true,
        showComments: true,
        bibleReferenceAllowed: true,
        showBibleReferences: true,
        limitReactionsToLoveOnly: false,
        createdBy: 'system-seed',
        createdWhen: '2026-01-01T00:00:00Z',
        updatedBy: 'system-seed',
        updatedWhen: '2026-01-01T00:00:00Z',
        deletedBy: null,
        deletedWhen: null,
        isDeleted: false,
        deletionReason: null,
        ...overrides
    });

const demoSettings: ReadonlyArray<ContentItemSetting> = [
    settingFor(ContentType.Quote, 'Quote', 'bi-quote'),
    settingFor(ContentType.Testimony, 'Testimony', 'bi-chat-heart'),
    settingFor(ContentType.Devotional, 'Devotional', 'bi-brightness-high'),
    settingFor(ContentType.BibleStudy, 'Bible Study', 'bi-book')
];

// A love-only override for one item, so the §6.5 narrowing is demonstrated rather than described.
const loveOnlyOverride: ContentItemSetting = settingFor(
    ContentType.Quote, 'Quote', 'bi-quote', {
    id: 'setting-quote-override',
    contentItemId: 'quote-2',
    limitReactionsToLoveOnly: true
});

const reactionOptions: ReadonlyArray<ContentItemReactionOption> = [
    { label: 'Amen', glyph: '🙌' },
    { label: 'Love', glyph: '❤️', isLove: true },
    { label: 'Joy', glyph: '😄' }
];

const demoItems: ReadonlyArray<ContentItemSearchItem> = [
    {
        id: 'quote-1',
        contentType: ContentType.Quote,
        author: 'D. L. Moody',
        content: 'Character is what you are in the dark.',
        contributorName: 'Bryan',
        publishedDate: new Date(2026, 6, 18),
        reactionCount: 142,
        commentCount: 9,
        tags: ['character', 'integrity']
    },
    {
        id: 'testimony-1',
        contentType: ContentType.Testimony,
        title: 'I stopped running in a hospital car park',
        content:
            'I had been busy for eleven years, and busy is a very good place to hide. It took a '
            + 'waiting room and a diagnosis that turned out to be nothing to make me sit still '
            + 'long enough to be found.',
        excerpt: 'Busy is a very good place to hide, until a waiting room takes it away.',
        contributorName: 'Louis',
        publishedDate: new Date(2026, 6, 15),
        reactionCount: 266,
        commentCount: 18,
        tags: ['testimony', 'faith'],
        bibleReferences: ['Psalm 46:10']
    },
    {
        id: 'devotional-1',
        contentType: ContentType.Devotional,
        title: 'Walking daily in grace',
        content:
            'Grace is not a one-time event but the daily air the believer breathes. It is given '
            + 'for the Tuesday you will not remember, as freely as for the day everything '
            + 'changed.',
        excerpt: 'Grace is not a one-time event but the daily air the believer breathes.',
        contributorName: 'Joan',
        publishedDate: new Date(2026, 6, 3),
        reactionCount: 87,
        commentCount: 5,
        tags: ['grace', 'discipleship'],
        bibleReferences: ['Ephesians 2:8-9']
    },
    {
        id: 'biblestudy-1',
        contentType: ContentType.BibleStudy,
        title: 'The armour of God, piece by piece',
        content:
            'A six-part walk through Paul’s picture of the believer’s equipment for '
            + 'the fight.',
        excerpt:
            'A six-part walk through Paul’s picture of the believer’s equipment for '
            + 'the fight.',
        contributorName: 'Amanda',
        publishedDate: new Date(2026, 5, 28),
        reactionCount: 54,
        commentCount: 12,
        tags: ['prayer', 'spiritual-warfare'],
        bibleReferences: ['Ephesians 6:10-18']
    }
];

const statusItems: ReadonlyArray<ContentItemSearchItem> = [
    {
        ...demoItems[2],
        id: 'devotional-draft',
        title: 'When the answer is wait',
        approvalStatus: ApprovalStatus.Draft
    },
    {
        ...demoItems[2],
        id: 'devotional-submitted',
        title: 'Small obediences',
        approvalStatus: ApprovalStatus.Submitted
    },
    {
        ...demoItems[2],
        id: 'devotional-approved',
        title: 'The lamp at your feet',
        approvalStatus: ApprovalStatus.Approved
    }
];

const propRows: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'contentItemCollection',
        type: 'ContentItemSearchItem[]',
        defaultValue: '[]',
        description: 'The results as they stand. On an infinite scroll this is the ACCUMULATED '
            + 'list, not the last page — the panel appends nothing of its own and holds no '
            + 'results of its own.'
    },
    {
        name: 'contentItemSettingCollection',
        type: 'ContentItemSetting[]',
        defaultValue: '[]',
        description: 'Every card resolves ITS OWN effective row (§6.4, §12.5.2 rules 1–2), so a '
            + 'mixed collection is safe and a soft-deleted row is out of resolution (§6.6). The '
            + 'Category box is built from the DEFAULT rows among them.'
    },
    {
        name: 'showSearchBar',
        type: 'boolean',
        defaultValue: 'true',
        description: 'Off leaves the list alone — right for a surface that has already decided '
            + 'what it is showing.'
    },
    {
        name: 'criteria',
        type: 'ContentItemSearchCriteria?',
        description: 'What Search was last pressed with. Seeds the boxes and reseeds them when '
            + 'it changes, so a page landing from ?q= shows what it searched for. The '
            + 'half-typed version lives inside the panel.'
    },
    {
        name: 'onSearch',
        type: '(criteria) => void',
        description: 'Raised when Search is pressed — never on a keystroke, and never when an '
            + 'advanced option changes. The panel does not filter: what the criteria mean is '
            + 'the consumer’s decision.'
    },
    {
        name: 'isLoading',
        type: 'boolean',
        defaultValue: 'false',
        description: 'The FIRST page. Holds the list back rather than emptying it, so a '
            + 're-search does not flash “nothing found” on its way to results.'
    },
    {
        name: 'isLoadingMore',
        type: 'boolean',
        defaultValue: 'false',
        description: 'A further page, on its way. The sentinel is held back while it is on, so '
            + 'one scroll is one fetch.'
    },
    {
        name: 'hasMore',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Whether anything is left. The consumer knows this from its own paging — '
            + 'the OData reads answer with no total, so a page asks for one row beyond the page.'
    },
    {
        name: 'onLoadMore',
        type: '() => void',
        description: 'Raised when the foot of the list comes into view, and by the fallback '
            + 'button where IntersectionObserver is unavailable. Never raised while '
            + 'isLoadingMore is on.'
    },
    {
        name: 'reactionOptions',
        type: 'ContentItemReactionOption[]',
        defaultValue: '[]',
        description: 'The reactions a reader may give. Empty means no card offers one, whatever '
            + 'the settings say: a surface that cannot persist a reaction must not appear to '
            + 'accept one. isLove marks the one a love-only type keeps.'
    },
    {
        name: 'onReacted',
        type: '(item, reaction) => void',
        description: 'Raised on a QUOTE only, and only when the effective setting allows it. The '
            + 'consumer posts it and hands back a new collection — the panel holds no optimistic '
            + 'state.'
    },
    {
        name: 'titleText / ariaLabel / cssClass',
        type: 'string',
        description: 'A heading above the list, the landmark name used when there is none, and a '
            + 'class appended for spacing in whatever it sits in.'
    },
    {
        name: 'text overrides',
        type: 'string',
        description: 'Every visible string is a prop — searchPlaceholderText, categoryLabelText, '
            + 'anyCategoryText, authorLabelText, authorPlaceholderText, loadingText, '
            + 'loadingMoreText, loadMoreButtonText, emptyText, commentsLinkText, readMoreText '
            + 'and authorByText.'
    }
];

export function ContentItemSearchPanelDoc() {
    useDocumentTitle('Content Item Search Panel — Components — Glory 2 Him');

    const [lastEvent, setLastEvent] = useState('—');

    const [criteria, setCriteria] =
        useState<ContentItemSearchCriteria>(emptyContentItemSearchCriteria);

    // The demo reacts for real, in the only way a presentation component can: the page owns the
    // state and hands a new collection back, exactly as a real consumer would after its write.
    const [reactedBy, setReactedBy] = useState<Readonly<Record<string, string>>>({});

    const quoteItems = useMemo(
        () => [
            demoItems[0],
            {
                ...demoItems[0],
                id: 'quote-2',
                author: 'George Müller',
                content:
                    'The beginning of anxiety is the end of faith, and the beginning of true '
                    + 'faith is the end of anxiety.',
                contributorName: 'Amanda',
                reactionCount: 61,
                commentCount: 3
            }
        ].map((item) => ({ ...item, viewerReactionLabel: reactedBy[item.id] })),
        [reactedBy]);

    return (
        <ComponentDoc
            name="Content Item Search Panel"
            filePath="src/components/contentItems/contentItemSearchPanel.tsx"
            summary={
                <>
                    Many content items, searched and scrolled — the sibling of{' '}
                    <code>ContentItemDetailPanel</code>, which renders one. A{' '}
                    <strong>pure presentation component</strong>: props in, events out, no
                    fetching, no mutation, no sockets.
                </>
            }>

            <DocSection
                title="It does not know what is behind the collection"
                lead={
                    <>
                        The same panel serves a public feed, a contributor&rsquo;s own rows and a
                        moderation queue &mdash; three pages over one component. So it{' '}
                        <strong>never filters</strong>, never decides visibility and never turns a
                        search into a request: it raises <code>onSearch</code> and renders whatever
                        comes back. The page chooses the read, and the server decides what that
                        caller may see against the stored row.
                    </>
                }>
                <CodeSample code={minimalSample} />
            </DocSection>

            <DocSection
                title="Two renders, chosen by content type"
                lead={
                    <>
                        A <code>Quote</code> gets the hero card and shows the quote{' '}
                        <strong>whole</strong>, because a quote is short enough to fit and to form
                        an opinion on. Every other type gets the horizontal row: thumbnail, title,
                        excerpt, pills, byline. The split is on the <em>type</em> rather than on
                        position &mdash; the hero is what a quote looks like, so a page of quotes
                        is a page of heroes and not one hero above a list.
                    </>
                }>
                <LiveDemo title="Live — a mixed page">
                    <ContentItemSearchPanel
                        contentItemCollection={demoItems}
                        contentItemSettingCollection={demoSettings}
                        showSearchBar={false} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Where an opinion may be given"
                lead={
                    <>
                        A quote may be reacted to <strong>in place</strong> &mdash; its whole
                        content is on screen. Everything else routes into the detail view first,
                        because you cannot form an opinion on an excerpt, and a like offered
                        beside three sentences of a six-part study invites exactly that.
                        Commenting always routes into the detail view, on both renders: there is
                        no room for a thread here and no honest way to show one.
                    </>
                }>
                <LiveDemo title="Live — react without leaving the list">
                    <ContentItemSearchPanel
                        contentItemCollection={quoteItems}
                        contentItemSettingCollection={[...demoSettings, loveOnlyOverride]}
                        showSearchBar={false}
                        reactionOptions={reactionOptions}
                        onReacted={(item, reaction) => {
                            setReactedBy((given) => ({ ...given, [item.id]: reaction.label }));
                            setLastEvent(`onReacted(${item.id}, ${reaction.label})`);
                        }} />
                </LiveDemo>

                <p className="small text-body-secondary">
                    Last event: <code>{lastEvent}</code>.{' '}
                    <strong>The second quote carries an item-level override</strong> with{' '}
                    <code>limitReactionsToLoveOnly</code>, so it offers the one option and the
                    first offers all three &mdash; the &sect;6.4 resolution running per card, on
                    one collection.
                </p>

                <CodeSample code={engagementSample} />
            </DocSection>

            <DocSection
                title="A row that is not yet public wears its status"
                lead={
                    <>
                        A public feed leaves <code>approvalStatus</code> unset and no badge
                        appears. A moderation surface or a &ldquo;my contributions&rdquo; page
                        sets it, because a draft that looks published is the one thing a
                        contributor must never be shown. <code>Approved</code> shows nothing: it
                        is the ordinary case, and a badge on every card would say nothing.
                    </>
                }>
                <LiveDemo title="Live — draft, in review, approved">
                    <ContentItemSearchPanel
                        contentItemCollection={statusItems}
                        contentItemSettingCollection={demoSettings}
                        showSearchBar={false} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="The search bar, and the box that is deliberately missing"
                lead={
                    <>
                        <code>SearchBarComponent</code> with the chevron folded out:{' '}
                        <strong>Category</strong> from the default settings, in the
                        administrator&rsquo;s own <code>SortOrder</code>, and{' '}
                        <strong>Author</strong> as free text against the author of the{' '}
                        <em>words</em> rather than whoever contributed the row. Changing an
                        advanced option does not re-run the search until the button is pressed.
                        <br />
                        <br />
                        The <strong>Tags</strong> box the search page carries is not here.
                        Associations have no HTTP exposer yet (<code>#318</code>), so it would be
                        a control that does nothing &mdash; and one that could only ever narrow
                        the pages already loaded, which on an infinite scroll is a filter that
                        quietly lies.
                    </>
                }>
                <LiveDemo title="Live — press Search">
                    <ContentItemSearchPanel
                        contentItemCollection={demoItems}
                        contentItemSettingCollection={demoSettings}
                        criteria={criteria}
                        onSearch={(searched) => {
                            setCriteria(searched);

                            setLastEvent(
                                `onSearch(${JSON.stringify(searched)})`);
                        }} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Infinite scroll, and the panel still does not fetch"
                lead={
                    <>
                        The panel owns an <code>IntersectionObserver</code> over a sentinel at the
                        foot of the list and raises <code>onLoadMore</code>. It is torn down while
                        a page is in flight and rebuilt when it lands, so one scroll is one fetch
                        and a sentinel still on screen asks for the next page. Where the observer
                        is unavailable a visible <strong>Load more</strong> button takes its
                        place, so the list is never a dead end.
                    </>
                }>
                <CodeSample code={pagingSample} />
            </DocSection>

            <DocSection
                title="What it deliberately leaves out"
                lead={
                    <>
                        <strong>The image.</strong> <code>ContentItem</code> carries no image
                        column and <code>Attachment</code> has no exposer, so nothing here fetches
                        one: the consumer supplies <code>imageUrl</code>, today a per-content-type
                        placeholder, and a card without one drops the thumbnail rather than
                        rendering a broken image.{' '}
                        <strong>Tags and bible references</strong> are rendered from the
                        projection and supplied by nobody until <code>#318</code> lands.{' '}
                        <strong>Approval controls</strong> belong to <code>ReviewPanel</code>, and{' '}
                        <strong>the item itself</strong> to <code>ContentItemDetailPanel</code>.
                        There is no <code>useQuery</code>, no <code>useMutation</code> and no
                        broker call anywhere inside.
                    </>
                } />

            <DocSection title="Props">
                <PropsTable rows={propRows} />
            </DocSection>
        </ComponentDoc>
    );
}
