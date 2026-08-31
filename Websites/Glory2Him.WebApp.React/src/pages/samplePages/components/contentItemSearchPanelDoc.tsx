import { useState } from 'react';
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
    ShareabilityBasis,
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

const structureSample = `
ContentItemSearchPanel                     composes the two below
├── ContentItemSearchBarPanel              search bar + advanced options + filter chips
└── ContentItemResultsPanel                the results, infinite scroll
    └── ContentItemItemPanel               ONE result — resolves the item's own effective
        │                                  ContentItemSetting, then dispatches to a template
        ├── ContentItemItemDefaultPanel    the template most types use
        └── ContentItemItem{ContentType}Panel   overrides, by ContentType:
              ContentItemItemQuotesPanel        (derives from the default template)
              ContentItemItemVersesPanel        (the verse whole — purple chip)
`;

const wiringSample = `
import { ContentItemSearchPanel } from '../../components/contentItems/contentItemSearchPanel';

// A feed page. The PAGE owns the read, the paging, the redirects and the persistence.
<ContentItemSearchPanel
    contentItemCollection={items}
    categorySettingCollection={defaultSettings}
    criteria={criteria}
    onSearch={setCriteria}
    isLoading={isLoading}
    isLoadingMore={isFetchingNextPage}
    hasMore={hasNextPage}
    onLoadMore={fetchNextPage}
    reactionOptions={reactionOptions}
    onReactionSelected={(item, reaction) => reactAsync(item, reaction)}
    onTitleClick={(item) => navigate(\`/posts/\${item.id}\`, { state: { from } })}
    onReadMoreClick={(item) => navigate(\`/posts/\${item.id}\`, { state: { from } })}
    onCommentsClick={(item) => navigate(\`/posts/\${item.id}#comments\`, { state: { from } })}
    onBibleReferenceClick={(item, ref) => navigate(bibleReferenceHref(ref), { state: { from } })} />

// A surface that has already decided what it shows turns the bar off and keeps the list.
// …and needs no settings at all: each element already carries its own.
<ContentItemSearchPanel
    contentItemCollection={myContributions}
    showSearchBar={false} />
`;

const hooksSample = `
// FILTER HOOKS — handled by ContentItemSearchPanel itself: each rewrites the committed
// criteria and raises onSearch, so the consumer sees one search signal however the reader asked.
onContentTypeClick      toggle the Category criterion (set if clear, clear if already this type)
onSubmittedByClick      set the submitted-by criterion ({ id, name } — the id filters, the name chips)
onAuthorClick           set the author criterion
onTagClick              set the tag criterion (servable once #318 lands)

// NAVIGATION HOOKS — bubble to the page, which owns every redirect and stamps the origin into
// router state so the destination can offer a true way back.
onTitleClick            the detail surface — public, my-content or moderation: the page decides
onReadMoreClick         same destination as onTitleClick
onCommentsClick         the detail's comment section
onBibleReferenceClick   wherever a reference leads on this surface
onEditClick             SUBMITTER ONLY (submittedById is the viewer's account id): detail-in-edit
onModerateClick         MODERATION TIER ONLY (Administrators, Reviewers, Publishers and their
                        ContentItem- / ContentItem-{ContentType}- scopes; ReadOnly vetoes):
                        the moderation detail. isModeratedView=true renders Moderate ALONE,
                        wearing Edit's pencil and label

// RENDER TOGGLES — the item panel keeps these for itself:
onAssignedReactionsClick   compact glyphs + total  ⇄  per-reaction counts
onReactionClick            open / close the reaction choices

// PERSISTENCE — bubbles; the consumer posts and hands back a refreshed collection:
onReactionSelected      create — or remove, when it is the one the reader already holds
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
    settingFor(ContentType.Quote, 'Quotes', 'bi-quote'),
    settingFor(ContentType.Story, 'Story', 'bi-journal-text'),
    settingFor(ContentType.Testimony, 'Testimony', 'bi-chat-heart'),
    settingFor(ContentType.Devotional, 'Devotional', 'bi-brightness-high'),
    settingFor(ContentType.BibleStudy, 'Bible Study', 'bi-book')
];

// A love-only setting for ONE element: each item CARRIES its winning row, so making one
// quote love-only is nothing but handing that one element a different setting — the
// one-element-swap the projection model is built for.
const loveOnlySetting: ContentItemSetting = settingFor(
    ContentType.Quote, 'Quotes', 'bi-quote', {
    id: 'setting-quote-override',
    contentItemId: 'quote-2',
    limitReactionsToLoveOnly: true
});

const reactionOptions: ReadonlyArray<ContentItemReactionOption> = [
    { label: 'Amen', glyph: '👍' },
    { label: 'Love', glyph: '❤️', isLove: true },
    { label: 'Joy', glyph: '😄' },
    { label: 'Praying', glyph: '🙏' }
];

const demoItems: ReadonlyArray<ContentItemSearchItem> = [
    {
        id: 'quote-1',
        contentType: ContentType.Quote,
        author: 'William Temple',
        content: "When I pray, coincidences happen; when I don't, they don't",
        imageUrl: '/assets/images/blog/16by9/big/01.jpg',
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
    },
    {
        id: 'story-1',
        contentType: ContentType.Story,
        title: 'NASA Proves The Bible Is True',
        author: 'Harold Hill',
        content:
            'Did you know that the space program is busy proving that what has been called '
            + '"myth" in the Bible is true? Scientists at Green Belt, Maryland were checking '
            + 'the position of the sun, moon, and planets out in space when the computer '
            + 'stopped and put up a red signal: a day was missing in elapsed time.',
        excerpt:
            'Did you know that the space program is busy proving that what has been called '
            + '"myth" in the Bible is true? Scientists at Green Belt, Maryland were checking '
            + 'the position of the sun, moon, and planets…',
        submittedById: 'account-louis',
        submittedByName: 'Louis',
        shareabilityBasis: ShareabilityBasis.PermissionGranted,
        publishedDate: new Date(2026, 6, 15),
        tags: ['creation', 'science', 'faith'],
        bibleReferences: ['Joshua 10:12-13', '2 Kings 20:9-11'],
        reactionSummary: [
            { label: 'Amen', glyph: '👍', count: 180 },
            { label: 'Love', glyph: '❤️', count: 60 },
            { label: 'Joy', glyph: '😄', count: 26 }
        ],
        commentCount: 18
    },
    {
        id: 'devotional-1',
        contentType: ContentType.Devotional,
        title: 'Walking daily in grace',
        author: 'Joan',
        content:
            'Grace is not a one-time event but the daily air the believer breathes. We wake '
            + 'to mercies that are new every morning, walk through the day leaning on strength '
            + 'that is not our own, and lie down at night forgiven.',
        excerpt:
            'Grace is not a one-time event but the daily air the believer breathes. We wake '
            + 'to mercies that are new every morning…',
        imageUrl: '/assets/images/blog/4by3/03.jpg',
        submittedById: 'account-joan',
        submittedByName: 'Joan',
        shareabilityBasis: ShareabilityBasis.Owned,
        publishedDate: new Date(2026, 6, 3),
        tags: ['grace', 'discipleship'],
        bibleReferences: ['Ephesians 2:8-9'],
        reactionSummary: [
            { label: 'Amen', glyph: '👍', count: 60 },
            { label: 'Love', glyph: '❤️', count: 27 }
        ],
        commentCount: 5
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
        description: 'The ACCUMULATED results — the consumer’s infinite query keeps the '
            + 'pages; the family appends nothing of its own.'
    },
    {
        name: 'categorySettingCollection',
        type: 'ContentItemSetting[]',
        defaultValue: '[]',
        description: 'FOR THE CATEGORY BOX ALONE: the per-type default rows the bar offers '
            + 'as choices. The cards never read it — each element carries its own winning '
            + 'setting, resolved by the projection (§6.4: the item’s override beats its '
            + 'type default), so updating one item is one element swapped by the consumer, '
            + 'never a refetch of the list.'
    },
    {
        name: 'showSearchBar',
        type: 'boolean',
        defaultValue: 'true',
        description: 'Off leaves the list alone — right for a surface that has already decided '
            + 'what it is showing.'
    },
    {
        name: 'criteria / onSearch',
        type: 'ContentItemSearchCriteria / (criteria) => void',
        description: 'The committed search. Typed boxes commit on Search; the clicked criteria '
            + '(type badge, submitted-by, author, tag) commit immediately and wear removable '
            + 'chips. onSearch is the ONE search signal the consumer sees, however the reader '
            + 'asked.'
    },
    {
        name: 'isLoading / isLoadingMore / hasMore / onLoadMore',
        type: 'paging',
        description: 'The infinite scroll: the results panel owns the sentinel and raises '
            + 'onLoadMore; the page owns useInfiniteQuery and the OData $skip/$top+1 paging. A '
            + 'visible Load more button takes over where IntersectionObserver is unavailable.'
    },
    {
        name: 'reactionOptions',
        type: 'ContentItemReactionOption[]',
        defaultValue: '[]',
        description: 'The choices behind Like — pulled by the page from GET api/Reactions '
            + '(approved rows only). Empty means no card offers one, whatever the settings say: '
            + 'a surface that cannot persist a reaction must not appear to accept one.'
    },
    {
        name: 'onReactionSelected',
        type: '(item, reaction) => void',
        description: 'The reader chose. The consumer posts the create — or the remove, when it '
            + 'is the one they already hold — and hands back a refreshed collection; the panel '
            + 'holds no optimistic state.'
    },
    {
        name: 'onTitleClick / onReadMoreClick / onCommentsClick / onBibleReferenceClick',
        type: '(item, …) => void',
        description: 'The navigation hooks — they bubble, the page routes, and the redirect '
            + 'carries { state: { from } } so the destination can offer a true back button. '
            + 'Share and Save render only where wired.'
    },
    {
        name: 'onEditClick',
        type: '(item) => void',
        description: 'The way to the surface where the item can be modified. Renders ONLY '
            + 'for the person who submitted the item (submittedById equals the signed-in '
            + 'account id) — a render gate: the server re-decides against the stored row.'
    },
    {
        name: 'onModerateClick',
        type: '(item) => void',
        description: 'The way to the moderation detail. Renders only for the moderation '
            + 'tier — Administrators, Reviewers and Publishers at every §18.6 scope '
            + '(global, ContentItem-, ContentItem-{ContentType}- for the item’s own type) '
            + '— with the ReadOnly veto asked first. Wears the shield and “Moderate”.'
    },
    {
        name: 'isModeratedView',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Marks the whole panel as a MODERATED surface. Off, cards offer Edit '
            + '(submitter) and Moderate (tier) side by side. On, Moderate stands alone on '
            + 'every card, wearing Edit’s pencil and label — on a surface that IS '
            + 'moderation, the moderation action is simply what editing means there.'
    },
    {
        name: 'onContentTypeClick / onSubmittedByClick / onAuthorClick / onTagClick',
        type: '(item, …) => void',
        description: 'The filter hooks. The family rewrites the criteria itself; a same-named '
            + 'consumer hook still fires afterwards for a page that wants to know.'
    },
    {
        name: 'text overrides',
        type: 'string',
        description: 'Every visible string is a prop, threaded once through the family — '
            + 'searchPlaceholderText, categoryLabelText, submittedByLabelText, likeButtonText, '
            + 'commentsText, editButtonText, readMoreText, emptyText and the rest.'
    }
];

export function ContentItemSearchPanelDoc() {
    useDocumentTitle('Content Item Search Panel — Components — Glory 2 Him');

    const [lastEvent, setLastEvent] = useState('—');

    const [criteria, setCriteria] =
        useState<ContentItemSearchCriteria>(emptyContentItemSearchCriteria);

    // The demo reacts for real, in the only way a presentation family can: the page owns the
    // state and hands a new collection back, exactly as a real consumer does after its write.
    const [reactedBy, setReactedBy] = useState<Readonly<Record<string, string>>>({});

    const quoteItems: ReadonlyArray<ContentItemSearchItem> = [
        demoItems[0],
        {
            ...demoItems[0],
            id: 'quote-2',
            contentItemSetting: loveOnlySetting,
            author: 'George Müller',
            content:
                'The beginning of anxiety is the end of faith, and the beginning of true '
                + 'faith is the end of anxiety.',
            imageUrl: undefined,
            submittedByName: 'Amanda',
            submittedById: 'account-amanda',
            reactionSummary: [{ label: 'Love', glyph: '❤️', count: 61 }],
            commentCount: 3
        }
    ].map((item) => ({ ...item, viewerReactionLabel: reactedBy[item.id] }));

    return (
        <ComponentDoc
            name="Content Item Search Panel"
            filePath="src/components/contentItems/contentItemSearchPanel.tsx"
            summary={
                <>
                    Many content items, searched and scrolled — a <strong>family</strong> of
                    presentation components: props in, events out, no fetching, no mutation, no
                    sockets. The same family serves the public home feed, /MyPosts and the
                    /Admin/Posts moderation queue.
                </>
            }>

            <DocSection
                title="The family"
                lead={
                    <>
                        Templates are resolved by <code>ContentType</code>: an override renders
                        where one is registered, the default otherwise, and an override{' '}
                        <strong>derives from</strong> <code>ContentItemItemDefaultPanel</code> by
                        rendering it with only the content slot replaced — the meta row, the
                        pills and the engagement row are written once.{' '}
                        <code>ContentItemItemVerseImagePanel</code> is designed but blocked:
                        there is no <code>ContentType.VerseImage</code> member yet, and the enum
                        is append-only (&sect;3.6) with three seeds riding on it.
                    </>
                }>
                <CodeSample code={structureSample} />
                <CodeSample code={wiringSample} />
            </DocSection>

            <DocSection
                title="It does not know what is behind the collection"
                lead={
                    <>
                        The panel never filters, never decides visibility and never turns a
                        search into a request: it raises <code>onSearch</code> and renders
                        whatever comes back. The page chooses the read —{' '}
                        <code>GET api/ContentItems/Public</code> for the home feed,{' '}
                        <code>GET api/ContentItems</code> pinned to the caller for /MyPosts,
                        pinned to Draft + Submitted for the moderation queue — and the server
                        decides what that caller may see against the stored row.
                    </>
                } />

            <DocSection
                title="Two templates, live"
                lead={
                    <>
                        A quote renders <strong>whole</strong> through the Quotes override — a
                        dark hero where the item carries an image, a quiet block where it does
                        not — because a quote is short enough to form an opinion on. Every other
                        type renders through the default template: title, excerpt,
                        read&nbsp;more. Both carry the same meta row, pills and engagement row,
                        because the override derives from the default.
                    </>
                }>
                <LiveDemo title="Live — a mixed page">
                    <ContentItemSearchPanel
                        contentItemCollection={demoItems}
                        showSearchBar={false}
                        onTitleClick={(item) => setLastEvent(`onTitleClick(${item.id})`)}
                        onReadMoreClick={(item) => setLastEvent(`onReadMoreClick(${item.id})`)}
                        onCommentsClick={(item) => setLastEvent(`onCommentsClick(${item.id})`)}
                        onBibleReferenceClick={(item, reference) =>
                            setLastEvent(`onBibleReferenceClick(${item.id}, ${reference})`)}
                        onEditClick={(item) => setLastEvent(`onEditClick(${item.id})`)} />
                </LiveDemo>

                <p className="small text-body-secondary">
                    Last event: <code>{lastEvent}</code> — every affordance is an event, and the
                    page decides where each one leads.
                </p>
            </DocSection>

            <DocSection
                title="The event hooks"
                lead={
                    <>
                        Three kinds, and the kind decides who handles it: filter hooks rewrite
                        the criteria inside the family, navigation hooks bubble to the page,
                        and the two render toggles never leave the card.
                    </>
                }>
                <CodeSample code={hooksSample} />
            </DocSection>

            <DocSection
                title="Reacting, and the per-item settings"
                lead={
                    <>
                        The assigned-reactions cluster toggles between its compact face and the
                        per-reaction counts. <strong>Like</strong> opens the choices —{' '}
                        <code>GET api/Reactions</code>, approved rows only, supplied by the page
                        — and choosing raises <code>onReactionSelected</code>; this demo
                        persists it into page state, exactly the shape of a real consumer.{' '}
                        <strong>The second quote’s ELEMENT carries a love-only setting</strong>{' '}
                        (<code>limitReactionsToLoveOnly</code>), so it offers one choice where
                        the first offers four — each element is self-contained, and making one
                        card differ is nothing but swapping that one element.
                    </>
                }>
                <LiveDemo title="Live — react without leaving the list">
                    <ContentItemSearchPanel
                        contentItemCollection={quoteItems}
                        showSearchBar={false}
                        reactionOptions={reactionOptions}
                        onReactionSelected={(item, reaction) => {
                            setReactedBy((given) => ({ ...given, [item.id]: reaction.label }));
                            setLastEvent(`onReactionSelected(${item.id}, ${reaction.label})`);
                        }}
                        onTitleClick={(item) => setLastEvent(`onTitleClick(${item.id})`)} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="The search bar, the filter clicks and the chips"
                lead={
                    <>
                        The typed boxes commit when Search is pressed. The clicked criteria —
                        the type badge toggling the category, Submitted&nbsp;by, Author, a tag
                        pill — commit <strong>immediately</strong> and wear removable chips, so
                        a narrowed list always says why it narrowed. There is deliberately no
                        Tags <em>box</em>: associations have no HTTP exposer yet (#318), and a
                        typed tag would be a control that does nothing.
                    </>
                }>
                <LiveDemo title="Live — click a badge, a byline or a pill">
                    <ContentItemSearchPanel
                        contentItemCollection={demoItems}
                        categorySettingCollection={demoSettings}
                        criteria={criteria}
                        onSearch={(searched) => {
                            setCriteria(searched);
                            setLastEvent(`onSearch(${JSON.stringify(searched)})`);
                        }} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="A row that is not yet public wears its status"
                lead={
                    <>
                        A public feed leaves <code>approvalStatus</code> unset and no badge
                        appears. /MyPosts and the moderation queue set it, because a draft that
                        looks published is the one thing a contributor must never be shown.{' '}
                        <code>Approved</code> shows nothing — it is the ordinary case.
                    </>
                }>
                <LiveDemo title="Live — draft, in review, approved">
                    <ContentItemSearchPanel
                        contentItemCollection={statusItems}
                        showSearchBar={false} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="What it deliberately leaves out"
                lead={
                    <>
                        <strong>The image</strong>: <code>ContentItem</code> carries no image
                        column and <code>Attachment</code> has no exposer, so the consumer
                        supplies <code>imageUrl</code> — today a per-type placeholder — and a
                        card without one simply drops it. <strong>Tags, references, reaction
                        summaries, comment counts and submitted-by names</strong> are
                        association- or resolver-shaped reads the host does not expose yet
                        (#318, &sect;16.7.4); cards claim no figure they were not given.{' '}
                        <strong>Approval controls</strong> belong to <code>ReviewPanel</code>,
                        and <strong>the item itself</strong> to{' '}
                        <code>ContentItemDetailPanel</code>. There is no <code>useQuery</code>,
                        no <code>useMutation</code> and no broker call anywhere inside.
                    </>
                } />

            <DocSection title="Props">
                <PropsTable rows={propRows} />
            </DocSection>
        </ComponentDoc>
    );
}
