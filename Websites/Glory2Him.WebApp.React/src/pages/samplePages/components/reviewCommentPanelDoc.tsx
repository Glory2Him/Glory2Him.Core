import { useState } from 'react';
import { ReviewCommentPanel } from '../../../components/approvals/reviewCommentPanel';
import { useDocumentTitle } from '../../useDocumentTitle';

import {
    ApprovalCommentType,
    ReviewCommentItem
} from '../../../models/components/approvals/reviewCommentItem';

import {
    DemoSecurityContext,
    SecurityContextOption
} from './shared/securityContextDemo';

import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DemoControls,
    DemoRadioGroup,
    DocSection,
    LiveDemo,
    PropsTable
} from './shared/componentDoc';

const familySample = `
ReviewCommentPanel                 the thread, and who may do what to it
├── ReviewCommentAddPanel          the box, the Comment/Question radios, Clear / Save
└── ReviewCommentResultsPanel      the rows, newest first, infinite scroll
    ├── ReviewCommentViewPanel     READ:  author, chip, timestamp, resolve tick, Edit / Delete
    └── ReviewCommentEditPanel     EDIT:  the words and the type, Save / Cancel
`;

const minimalSample = `
import { ReviewCommentPanel } from '../../components/approvals/reviewCommentPanel';

// The round already carries the thread: useApprovalRound reads the verdict, learns the
// approval id from it, and projects the comments against the round's own name read.
const {
    approvalVerdict,
    reviewCommentCollection,
    reviewComments,
    areReviewCommentsLoading
} = useApprovalRound(EntityTypeName.ContentItem, contentItemId);

<ReviewCommentPanel
    approvalId={approvalVerdict?.approvalId ?? ''}
    reviewComments={reviewCommentCollection}
    entityType="ContentItem"
    contentType={ContentType[contentItem.contentType] ?? ''}
    isLoading={areReviewCommentsLoading}
    isSubmitting={addComment.isPending || resolveComment.isPending}
    onSave={(draft) => void saveAsync(draft)}
    onModified={(item) => void modifyAsync(item)}
    onRemoveRequested={setCommentToRemove}
    onResolvedChanged={(item, isResolved) => void resolveAsync(item, isResolved)}
    showBorder />
`;

const settledSample = `
// IsResolved MEANS SETTLED, NOT ANSWERED (design §7.8), and the whole surface turns on it:
//
//   Comment   asks for nothing      → born IsResolved = true     never blocks
//   Question  asks for something    → born IsResolved = false    holds the approval shut
//
// The two are stored SEPARATELY on purpose. The flag MOVES — the moment a question is
// settled it carries exactly what an informational comment was born with — so without
// ApprovalCommentType a thread cannot tell them apart afterwards, and the resolve tick
// cannot be offered on questions alone.
//
// Settling runs BOTH WAYS. A remark that turns out to need action must be able to block,
// and one settled prematurely must be able to block again — so one control raises both
// directions and the server takes the flag as bind-required.
`;

const gatesSample = `
// EVERY GATE DECIDES WHAT TO RENDER AND NOTHING MORE. The foundation re-decides add,
// modify, remove and resolve against the stored row (§14.6).
//
//   add               any authenticated reader (§12.3.1 rule 5 — submitters converse
//                     in review threads), blocked only by the GLOBAL ReadOnly
//   edit / delete     the AUTHOR alone. No role widens amending somebody else's words
//   resolve           the author, OR the publisher tier for this entity —
//                     Administrators, Publishers, {Entity}-Publishers,
//                     {Entity}-{ContentType}-Publishers
//
// THE VETO IS WIDER ON RESOLVE THAN ON THE WORDS, and that asymmetry is the design's.
// §18.6 rule 3 exempts the comment thread from the scoped ReadOnly — a comment is speech
// about the content, not a write to it — and names IsResolved as the place that reasoning
// strains, because settling a comment clears a §8.5 gate. So the scoped block counts on
// the tick and not on the box, and it is asked FIRST, ahead of the author branch.
`;

// THE PEOPLE THIS PANEL TELLS APART. Its own list rather than the shared six, because the
// distinctions here are different: ownership decides amending, the PUBLISHER tier decides
// settling, the review tier decides neither, and the sanction reaches the tick at three scopes
// and the box at only one. The demo item is a Quote, so the narrow names are spelled for it.
const threadSecurityContextOptions: ReadonlyArray<SecurityContextOption> = [
    {
        key: 'author',
        label: 'I wrote the question below (the author)',
        roles: [],
        isOwner: true
    },
    {
        key: 'contributor',
        label: 'I am another contributor, holding no role',
        roles: [],
        isOwner: false
    },
    {
        key: 'reviewer',
        label: 'I am a reviewer',
        roles: ['Reviewers'],
        isOwner: false
    },
    {
        key: 'publisher',
        label: 'I am a publisher',
        roles: ['Publishers'],
        isOwner: false
    },
    {
        key: 'content-item-publisher',
        label: 'I am a publisher of content items',
        roles: ['ContentItem-Publishers'],
        isOwner: false
    },
    {
        key: 'quote-publisher',
        label: 'I am a publisher of quotes (this demo’s type)',
        roles: ['ContentItem-Quote-Publishers'],
        isOwner: false
    },
    {
        key: 'devotional-publisher',
        label: 'I am a publisher of devotionals (another type)',
        roles: ['ContentItem-Devotional-Publishers'],
        isOwner: false
    },
    {
        key: 'administrator',
        label: 'I am an administrator',
        roles: ['Administrators'],
        isOwner: false
    },
    {
        key: 'sanctioned-publisher',
        label: 'I publish quotes but am sanctioned on them (ContentItem-Quote-ReadOnly)',
        roles: ['ContentItem-Quote-Publishers', 'ContentItem-Quote-ReadOnly'],
        isOwner: false
    },
    {
        key: 'sanctioned-globally',
        label: 'I am an administrator holding the global ReadOnly',
        roles: ['Administrators', 'ReadOnly'],
        isOwner: false
    }
];

// AuthContextOverride stands the demo viewer up as this id, so a row carrying it is "mine".
const demoViewerId = 'demo-viewer';
const demoApprovalId = 'demo-approval-1';

const demoComment = (
    overrides: Partial<ReviewCommentItem> = {}): ReviewCommentItem => ({
        id: 'demo-comment-susan',
        approvalId: demoApprovalId,
        comment: 'I think it works — being moved to tears is exactly the feeling this captures.',
        commentType: ApprovalCommentType.Comment,
        isResolved: true,
        authorId: 'somebody-else',
        authorDisplayName: 'Susan',
        authorUserName: 'susan',
        createdWhen: '2026-08-26T11:20:00Z',
        ...overrides
    });

export function ReviewCommentPanelDoc() {
    useDocumentTitle('Review Comment Panel — Components — Glory 2 Him');

    const [securityContext, setSecurityContext] =
        useState<SecurityContextOption>(threadSecurityContextOptions[0]);

    const [defaultTypeKey, setDefaultTypeKey] = useState('comment');
    const [showsBorder, setShowsBorder] = useState(true);
    const [isLoading, setIsLoading] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [hasMore, setHasMore] = useState(false);
    const [hasThread, setHasThread] = useState(true);
    const [hasApproval, setHasApproval] = useState(true);
    const [lastEvent, setLastEvent] = useState('');

    // WHO WROTE THE QUESTION is the demo's other real switch. Ownership is a property of the
    // ROW, so stepping into "the author" has to move the row's authorId — a persona alone
    // cannot express it, which is why this list carries isOwner at all.
    const demoQuestion = demoComment({
        id: 'demo-comment-john',
        comment: 'Does a crying face read as "moved" or as sadness? '
            + 'Could it be misread on encouraging posts?',
        commentType: ApprovalCommentType.Question,
        isResolved: false,
        authorId: securityContext.isOwner ? demoViewerId : 'user-john',
        authorDisplayName: securityContext.isOwner ? 'Demo Viewer' : 'John',
        authorUserName: securityContext.isOwner ? 'demo' : 'john',
        createdWhen: '2026-08-27T16:05:00Z'
    });

    return (
        <ComponentDoc
            name="Review Comment Panel"
            filePath="src/components/approvals/reviewCommentPanel.tsx"
            summary="The conversation a review round is actually made of — questions that hold an
                approval shut, remarks that do not, and the settling of the first kind. Built for
                the moderation surface, beneath the facts attached to the submission.">

            <DocSection
                title="The family"
                lead={
                    <>
                        One dispatcher, one add face and a results list that swaps each row
                        between a read and an edit template — exactly as{' '}
                        <code>ContentItemPanel</code> is built.{' '}
                        <code>ReviewCommentPanel</code> owns what every face shares — the
                        ordering, the ownership gate, the resolve tier and the{' '}
                        <code>ReadOnly</code> veto — and the templates render what it decides.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, four faces" />
                <CodeSample code={minimalSample} caption="The wiring" />
            </DocSection>

            <DocSection
                title="Settled, not answered"
                lead={
                    <>
                        The distinction the whole surface turns on, and the reason the type is
                        stored beside the flag rather than derived from it.
                    </>
                }>
                <CodeSample code={settledSample} caption="§7.8, and why the type is its own column" />
            </DocSection>

            <DocSection
                title="Who may do what"
                lead={
                    <>
                        Three different answers, and the sanction reaches them differently. Every
                        one of them decides <b>rendering</b> only.
                    </>
                }>
                <CodeSample code={gatesSample} caption="§12.3.1 rule 5, §14.7 rule 5, §18.6" />
            </DocSection>

            <DocSection
                title="What the consumer owns"
                lead={
                    <>
                        A pure presentation component: props in, events out, no fetching and no
                        mutation. The page owns the reads, the writes, the confirmation dialog{' '}
                        <b>and the freshness</b> — a thread two moderators are working moves
                        while it is being read, so{' '}
                        <code>useGetApprovalComments</code> polls and refetches on focus. The last
                        event this page received:{' '}
                        <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
                    </>
                }>
                <PropsTable rows={panelProps} />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        Two rows: a <b>remark</b> from Susan, settled from birth, and an{' '}
                        <b>outstanding question</b> from John. Step the demo through the personas
                        and watch three separate things move — the box (any authenticated reader,
                        stopped only by the global <code>ReadOnly</code>), <b>Edit / Delete</b>{' '}
                        (the author alone), and the <b>settled tick</b> (the author or the
                        publisher tier for this quote, stopped by a sanction at any scope). A
                        publisher of <i>devotionals</i> gets nothing here. The panel decides
                        rendering only, so showing a reader what somebody else would be offered
                        grants nothing — the foundation re-decides every write against the stored
                        row (§14.6), and your own session is untouched.
                    </>
                }>

                <DemoRadioGroup
                    title="Security context"
                    name="review-comment-security-context"
                    selectedKey={securityContext.key}
                    onChange={(key) => setSecurityContext(
                        threadSecurityContextOptions.find((option) => option.key === key)
                        ?? threadSecurityContextOptions[0])}
                    options={threadSecurityContextOptions} />

                <DemoRadioGroup
                    title="defaultType"
                    name="review-comment-default-type"
                    selectedKey={defaultTypeKey}
                    onChange={setDefaultTypeKey}
                    options={[
                        { key: 'comment', label: 'Comment — the default' },
                        { key: 'question', label: 'Question — opens on the blocking kind' }
                    ]} />

                <DemoControls
                    toggles={[
                        {
                            name: 'showBorder',
                            label: 'showBorder',
                            defaultValue: true,
                            value: showsBorder,
                            onChange: setShowsBorder
                        },
                        {
                            name: 'isLoading',
                            label: 'isLoading',
                            defaultValue: false,
                            value: isLoading,
                            onChange: setIsLoading
                        },
                        {
                            name: 'isSubmitting',
                            label: 'isSubmitting',
                            defaultValue: false,
                            value: isSubmitting,
                            onChange: setIsSubmitting
                        },
                        {
                            name: 'hasMore',
                            label: 'hasMore — the infinite-scroll sentinel',
                            defaultValue: false,
                            value: hasMore,
                            onChange: setHasMore
                        },
                        {
                            name: 'hasThread',
                            label: 'The thread has comments',
                            value: hasThread,
                            onChange: setHasThread
                        },
                        {
                            name: 'hasApproval',
                            label: 'approvalId — the item has an approval round',
                            value: hasApproval,
                            onChange: setHasApproval
                        }
                    ]} />

                <DemoSecurityContext option={securityContext}>
                    <LiveDemo title="The thread">
                        <ReviewCommentPanel
                            approvalId={hasApproval ? demoApprovalId : ''}
                            reviewComments={hasThread ? [demoComment(), demoQuestion] : []}
                            defaultType={defaultTypeKey === 'question'
                                ? ApprovalCommentType.Question
                                : ApprovalCommentType.Comment}
                            entityType="ContentItem"
                            contentType="Quote"
                            showBorder={showsBorder}
                            isLoading={isLoading}
                            isSubmitting={isSubmitting}
                            hasMore={hasMore}
                            onLoadMore={() => setLastEvent('onLoadMore')}
                            onSave={(draft) => setLastEvent(
                                `onSave — ${draft.commentType === ApprovalCommentType.Question
                                    ? 'Question'
                                    : 'Comment'}: "${draft.comment}"`)}
                            onClear={() => setLastEvent('onClear')}
                            onModified={(item) => setLastEvent(
                                `onModified — ${item.id}: "${item.comment}"`)}
                            onRemoveRequested={(item) => setLastEvent(
                                `onRemoveRequested — ${item.id} (the page asks "Are you sure?")`)}
                            onResolvedChanged={(item, isResolved) => setLastEvent(
                                `onResolvedChanged — ${item.id} → ${String(isResolved)}`)} />
                    </LiveDemo>
                </DemoSecurityContext>
            </DocSection>
        </ComponentDoc>
    );
}

const panelProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'approvalId',
        type: 'string',
        defaultValue: '—',
        description: 'The approval every comment on this thread is tied to, stamped onto a save. '
            + 'EMPTY MEANS NO THREAD: an item with no approval row, or a caller the verdict '
            + 'refused (§14.5 rule 1), has nothing to comment on — the panel says so rather than '
            + 'offering a box that cannot post.'
    },
    {
        name: 'reviewComments',
        type: 'ReviewCommentItem[]',
        defaultValue: '[]',
        description: 'The thread as the consumer holds it, already projected with its author '
            + 'names. Sorted DESCENDING inside the panel rather than trusted in the order it '
            + 'arrives, so no consumer can hand it over the wrong way round. It must be kept '
            + 'MOVING by the consumer — the panel does no fetching, and a thread two moderators '
            + 'are working changes under both of them.'
    },
    {
        name: 'defaultType',
        type: "ApprovalCommentType ('Comment' | 'Question')",
        defaultValue: 'Comment',
        description: 'Which radio the box opens on, and what Clear returns it to. Comment by '
            + 'default: most of what is written on a round is an observation, and opening on '
            + 'Question would make every absent-minded save block the approval.'
    },
    {
        name: 'showBorder',
        type: 'boolean',
        defaultValue: 'true',
        description: 'Whether the panel is drawn as a bordered card. ON by default, like '
            + 'ContentItemSettingsPanel and unlike the content item family: it stands under '
            + 'other panels in a column rather than in a feed of its own.'
    },
    {
        name: 'entityType / contentType',
        type: 'string',
        defaultValue: 'ContentItem / —',
        description: 'Names the entity under approval so the resolve tier and the ReadOnly veto '
            + 'can be composed (§18.6, capability-last and plural). NOTHING is fetched from '
            + 'them. contentType is the enum MEMBER NAME, never the setting’s editable '
            + 'ContentTypeName — a renamed type must not silently shed its role names.'
    },
    {
        name: 'isLoading / isLoadingMore / hasMore / onLoadMore',
        type: 'boolean / boolean / boolean / () => void',
        defaultValue: 'false / false / false / —',
        description: 'The paging contract ContentItemResultsPanel already defines: a first-page '
            + 'spinner that replaces the list rather than emptying it, an IntersectionObserver '
            + 'sentinel, and a Load more button where the observer is unavailable.'
    },
    {
        name: 'isSubmitting',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Freezes every button and the tick while the consumer is persisting, so one '
            + 'click is one write.'
    },
    {
        name: 'cssClass / titleText / emptyText',
        type: 'string',
        defaultValue: '— / Review Comments / Nothing has been said…',
        description: 'The family’s presentation props. There is deliberately no ariaLabel: the '
            + 'panel names itself with aria-labelledby off its own heading, which outranks '
            + 'aria-label in the accessible-name algorithm — so titleText IS the accessible '
            + 'name, and a consumer telling two threads apart sets that.'
    },
    {
        name: 'onSave',
        type: '(draft: ReviewCommentDraft) => void',
        description: 'A new comment: the words, the chosen type and the approvalId. Raised only '
            + 'once the panel has refused a blank one and cleared its box.'
    },
    {
        name: 'onClear',
        type: '() => void',
        description: 'Notification only — the box empties and the radios go back to defaultType '
            + 'internally, the way ContentItemPanel closes its own editor.'
    },
    {
        name: 'onModified',
        type: '(item: ReviewCommentItem) => void',
        description: 'An edited row committed. Carries the WHOLE row with only the words and the '
            + 'type moved — an amend is a PUT of the row that was read, and the consumer merges '
            + 'it onto the stored one because the foundation pins CreatedBy, CreatedWhen, '
            + 'ApprovalId and UpdatedWhen against storage. isResolved is never touched here.'
    },
    {
        name: 'onRemoveRequested',
        type: '(item: ReviewCommentItem) => void',
        description: 'The reader took Delete. The CONSUMER asks “Are you sure?” — a panel that '
            + 'owned the dialog would own page chrome it cannot place. Wire this, and onRemoved '
            + 'fires only after the answer.'
    },
    {
        name: 'onRemoved',
        type: '(item: ReviewCommentItem) => void',
        description: 'Delete, CONFIRMED. On a surface with nothing to ask — a doc page, a test — '
            + 'wiring this alone makes Delete raise it directly. Never both: one click is one '
            + 'event.'
    },
    {
        name: 'onResolvedChanged',
        type: '(item: ReviewCommentItem, isResolved: boolean) => void',
        description: 'The settled tick, both ways. Rendered on a QUESTION only, and only to the '
            + 'author or the publisher tier. The panel does NOT flip the tick itself — the row '
            + 'it renders is the consumer’s, so an un-persisted click must not look like a '
            + 'settled comment.'
    }
];
