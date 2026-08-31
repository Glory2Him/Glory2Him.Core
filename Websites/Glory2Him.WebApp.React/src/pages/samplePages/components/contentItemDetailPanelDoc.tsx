import { useState } from 'react';
import { ContentItemDetailPanel } from '../../../components/contentItems/contentItemDetailPanel';
import { useAuth } from '../../../components/securitys/authProvider';

import {
    ContentItemSetting
} from '../../../models/foundations/contentItemSettings/contentItemSetting';

import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';

import {
    ApprovalStatus,
    ContentItemFormItem,
    ShareabilityBasis
} from '../../../models/components/contentItems/contentItemFormItem';

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
import { ContentItemDetailPanel } from '../../components/contentItems/contentItemDetailPanel';

// add — no item yet, so the panel offers the picker and the editable fields
<ContentItemDetailPanel
    contentItemSettingCollection={contributableSettings}
    validationIssues={validationIssues}
    isSubmitting={addContentItem.isPending}
    onAdded={(item) => addContentItemAsync(item)}
    onCancelled={() => navigate('/')} />

// read — an item, and no way to turn the surface into an edit one
<ContentItemDetailPanel
    contentItem={formItem}
    contentItemSettingCollection={defaultSettings} />

// read + edit — the roles now decide, per action, what is actually shown
<ContentItemDetailPanel
    contentItem={formItem}
    contentItemSettingCollection={defaultSettings}
    isEditingAllowed
    onModified={(item) => saveAsync(item)}
    onRemoved={(item) => removeAsync(item)} />
`;

const persistenceSample = `
// THE CONSUMER OWNS PERSISTENCE AND FRESHNESS (design §20.6.2).
//
// The panel never fetches, never mutates and never subscribes. It shows the world as of the last
// props it was handed, so the page does all of this:

const addContentItemAsync = async (formItem) => {
    setValidationIssues(undefined);

    try {
        const added = await addContentItem.mutateAsync(toContentItemAddRequest(formItem));
        navigate(\`/posts/\${added.id}\`);
    } catch (error) {
        // 400 → the API's own field messages go straight back onto the form, and its reason
        // goes to the toast. The panel judges nothing itself; the server is the authority.
        const failure = toContentItemApiFailure(error, 'Your contribution could not be submitted.');

        setValidationIssues(failure.validationIssues);
        toastError(failure.message);
    }
};

// onModified is NOT necessarily a PUT. Amending a terminal item forks a new version
// (§3.4 rule 16) — the panel raises the event, the consumer decides which write it is.
`;

const rolesSample = `
// Composed from entityType + the content type IN PLAY — the selected type while adding, the
// item's own type when reading or editing. Capability LAST and PLURAL (§18.6, #368); ReadOnly
// stays singular, because it names a state its holder is in rather than a group of people.

blockRoles  = "ReadOnly, ContentItem-ReadOnly, ContentItem-{ContentType}-ReadOnly"
addRoles    = ""                       // any authenticated reader: there is no Contributor role
editRoles   = "[OWNER], Publishers, ContentItem-Publishers,
               ContentItem-{ContentType}-Publishers, Administrators"
deleteRoles = "[OWNER], Administrators"

// {ContentType} resolves against the type in play, so one override still expresses the narrow
// tier. [OWNER] is the item's contributor, matched on the ACCOUNT ID and never on a display name.
// The block set is asked FIRST and outranks every grant, [OWNER] included (#366).
`;

const settingFor = (
    contentType: ContentType,
    contentTypeName: string,
    contentTypeDescription: string,
    contentTypeIconCssClass: string,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
        id: `setting-${contentType}`,
        contentType,
        contentItemId: null,
        contentTypeName,
        contentTypeDescription,
        contentTypeIconCssClass,
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
        createdWhen: '2026-01-01T00:00:00+00:00',
        updatedBy: 'seed',
        updatedWhen: '2026-01-01T00:00:00+00:00',
        deletedBy: null,
        deletedWhen: null,
        isDeleted: false,
        deletionReason: null,
        ...overrides
    });

const demoSettings: ReadonlyArray<ContentItemSetting> = [
    settingFor(ContentType.Story, 'Story', 'Something He did', 'bi-book'),
    settingFor(ContentType.Testimony, 'Testimony', 'What He has done for you', 'bi-chat-heart'),

    settingFor(ContentType.Quote, 'Quote', 'Words worth passing on', 'bi-quote', {
        hasTitle: false
    }),

    settingFor(ContentType.Devotional, 'Devotional', 'A word for today', 'bi-sunrise')
];

// The item-level override for the story below: same content type, keyed to that one item, and
// it drops the author field the Story default carries. Handing the panel BOTH rows is the point
// of the resolution demo — the most specific one wins.
const storyOverride: ContentItemSetting = settingFor(
    ContentType.Story, 'Story', 'Something He did', 'bi-book', {
    id: 'setting-story-override',
    contentItemId: 'content-item-1',
    hasAuthor: false,
    isAvailableAsGeneralUserContribution: false
});

const storyByAnother: ContentItemFormItem = {
    id: 'content-item-1',
    contentType: ContentType.Story,
    title: 'He met me on the ward',
    author: 'Grace Abara',
    content:
        'I had run out of anything to say by the third night.\n\n'
        + 'What I had left was a sentence I had been given as a child, and it turned out to be '
        + 'enough to hold on to until morning.',
    shareabilityBasis: ShareabilityBasis.PermissionGranted,
    sharePermission: 'Permission granted by the author by email, 12 Jan 2026',
    createdBy: 'another-user',
    createdWhen: '2026-07-15T09:14:00+00:00',
    approvalStatus: ApprovalStatus.Submitted
};

// The same contribution, told by the person it happened to, and signed with the name the form
// prefilled for her. The Author column disappears on the read surface — not because an owned basis
// removes it, but because it would be printing "Grace Abara" twice.
const storyReleasedByItsAuthor: ContentItemFormItem = {
    ...storyByAnother,
    id: 'content-item-3',
    author: 'Grace Abara',
    shareabilityBasis: ShareabilityBasis.OwnedPublicDomain,
    sharePermission: '',
    createdWhen: '2026-07-15T09:14:00+00:00'
};

// The same again, published under a pen name. Now the Author column has something to say that
// "Submitted by" does not, so it comes back.
const storyUnderAPenName: ContentItemFormItem = {
    ...storyReleasedByItsAuthor,
    id: 'content-item-4',
    author: 'A. Pilgrim'
};

const approvedStory: ContentItemFormItem = {
    ...storyByAnother,
    id: 'content-item-2',
    title: 'The long way round',
    approvalStatus: ApprovalStatus.Approved
};

const propRows: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'contentItem',
        type: 'ContentItemFormItem?',
        description: 'The item. Absent puts the panel in add. Hand over a STABLE object — the '
            + 'editor is seeded from it whenever its identity changes.'
    },
    {
        name: 'mode',
        type: "'add' | 'read' | 'edit'?",
        description: 'Overrides the mode derived from contentItem, so a consumer can land '
            + 'straight on an edit surface. A CHANGE to it also overrules whatever the reader '
            + 'last chose, so a page driving the panel from its own state can close and reopen '
            + 'the editor. edit is refused back to read when isEditingAllowed is off or the '
            + 'roles do not allow it.'
    },
    {
        name: 'contentItemSettingCollection',
        type: 'ContentItemSetting[]',
        defaultValue: '[]',
        description: 'Which fields exist, per content type — passed in, never fetched. Hand over '
            + 'whatever rows you hold; the panel resolves the effective one (see below).'
    },
    {
        name: 'contentItemSettingCollection (resolution)',
        type: '—',
        description: 'The panel resolves the EFFECTIVE row itself (§6.4, §12.5.2 rules 1–2): an '
            + 'item-level override beats the content type default, matched on the item as well '
            + 'as the type; a soft-deleted row is out of resolution entirely (§6.6). The picker '
            + 'offers the defaults that carry isAvailableAsGeneralUserContribution.'
    },
    {
        name: 'showItemTitle',
        type: 'boolean',
        defaultValue: 'true',
        description: 'Whether the read surface renders the item\u2019s own title. Off for a '
            + 'consumer that has already stated the title itself, so the heading is not said '
            + 'twice.'
    },
    {
        name: 'titleHeadingLevel',
        type: "'h1' | 'h2' | 'h3' | 'h4'",
        defaultValue: "'h3'",
        description: 'Which heading the read surface renders the title as. h3 suits a panel '
            + 'sitting among other content; a page whose whole subject is this item raises it to '
            + 'h1 and keeps the title where the design puts it \u2014 under the type chip \u2014 rather '
            + 'than duplicating it above the panel to get the outline right. /posts/{id} does '
            + 'exactly that.'
    },
    {
        name: 'submittedByDisplayName / submittedByImageUrl / submittedByHref',
        type: 'string?',
        description: 'The byline identity. The item carries only createdBy \u2014 an ACCOUNT ID, '
            + 'because two accounts can share a name \u2014 so the consumer resolves it '
            + '(GET /api/contributors/{id}) and hands it over; the panel fetches nothing. Absent, '
            + 'the block is omitted rather than showing a placeholder under somebody\u2019s '
            + 'testimony. No image url falls back to the initials avatar; no href renders the '
            + 'name as plain text.'
    },
    {
        name: 'readingTimeMinutes / reactionCount / commentCount / viewCount',
        type: 'number?',
        description: 'The figures reading under the byline. Each is INDEPENDENTLY optional and '
            + 'undefined leaves it out rather than rendering a zero \u2014 \u201c0 comments\u201d asserts '
            + 'that the conversation is empty, which is not the same as a surface with nothing to '
            + 'report. None of them are computed here: reading time is a function of the content '
            + '(readingTimeMinutesOf), and the counts are separate reads the consumer gathers.'
    },
    {
        name: 'isLoading',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Shows a loading line instead of a half-built surface while the consumer is '
            + 'still fetching.'
    },
    {
        name: 'titleText',
        type: 'string',
        defaultValue: "''",
        description: 'A heading above the panel. Empty renders no heading at all, which is what '
            + 'both shipped consumers want \u2014 the page already has one.'
    },
    {
        name: 'ariaLabel',
        type: 'string',
        defaultValue: "'Content item'",
        description: 'Names the section for a screen reader when titleText is empty, so the '
            + 'landmark is never anonymous.'
    },
    {
        name: 'showBorder',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Wraps the panel in the bordered card the association panels use.'
    },
    {
        name: 'cssClass',
        type: 'string',
        defaultValue: "''",
        description: 'Appended to the panel\u2019s own class, for spacing in whatever it sits in.'
    },
    {
        name: 'loginHref / loginButtonText / loginButtonCssClass',
        type: 'string',
        defaultValue: '/Account/Login?returnUrl={path}',
        description: 'The way in offered instead of the add form when nobody is signed in. The '
            + 'default returns the reader to the current path, exactly as AssociationPanel and '
            + 'SecuredRoute build it.'
    },
    {
        name: 'text overrides',
        type: 'string',
        description: 'Every visible string is a prop \u2014 typePickerTitleText, titleLabelText, '
            + 'contentLabelText, shareabilityLabelText, submitButtonText, saveButtonText, '
            + 'cancelButtonText, editButtonText, deleteButtonText, blockedText, typeBlockedText, '
            + 'noTypesText, validationSummaryText, the deleteConfirm trio, and the rest.'
    },
    {
        name: 'theme class overrides',
        type: 'string',
        description: 'submitButtonCssClass, editButtonCssClass, deleteButtonCssClass \u2014 CSS '
            + 'classes rather than colours, so every control follows the light/dark theme.'
    },
    {
        name: 'the type chip\u2019s colour',
        type: '\u2014',
        description: 'Not a prop. The chip carries the type\u2019s enum MEMBER NAME in '
            + 'data-content-type and sets no colour at all; contentItems.css keys the palette off '
            + 'that attribute. So recolouring a type is a stylesheet edit, a type nobody has '
            + 'chosen a colour for arrives neutral rather than borrowing another\u2019s, and the '
            + 'picker is live \u2014 the attribute IS the selector, so moving the selection '
            + 're-resolves the cascade with nothing wired between them. The member name rather '
            + 'than the editable contentTypeName, so a rename cannot detach a type from its '
            + 'colour.'
    },
    {
        name: 'the author field, under an owned basis',
        type: '\u2014',
        description: 'Not a prop either. An owned basis PREFILLS this field with the '
            + 'contributor\u2019s display name rather than removing it \u2014 the basis says who wrote '
            + 'it, not what they want to be called for it. It fills an empty field only, and only '
            + 'until the contributor touches it, so an author already on the item is never '
            + 'overwritten and a pen name is never reverted; on an amendment the name comes from '
            + 'the submitter, not the editor. The type\u2019s own hasAuthor still decides whether '
            + 'the field exists at all.'
    },
    {
        name: 'the author column, on the read surface',
        type: '\u2014',
        description: 'Rendered unless it would print ONE PERSON TWICE \u2014 the author and the '
            + 'submitter being the same name, compared case- and accent-insensitively. A pen name '
            + 'says something \u201cSubmitted by\u201d does not, so it shows; and while the submitter is '
            + 'still unresolved nothing is known to be duplicated, so it shows then too.'
    },
    {
        name: 'isEditingAllowed',
        type: 'boolean',
        defaultValue: 'false',
        description: 'The surface switch, ahead of every role check. Off, the panel renders no '
            + 'Edit, no Delete and no route into edit, however the roles fall. It only ever '
            + 'subtracts.'
    },
    {
        name: 'validationIssues',
        type: 'Record<string, string[]>?',
        description: 'What the API said was wrong, keyed by ITS parameter names. Matched '
            + 'case-insensitively onto the fields; anything unplaceable renders in a summary '
            + 'rather than being dropped.'
    },
    {
        name: 'isSubmitting',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Freezes the buttons while the consumer is persisting, so one click is '
            + 'one write.'
    },
    {
        name: 'onAdded',
        type: '(item) => void',
        description: 'Submit for review, in add. The consumer POSTs it.'
    },
    {
        name: 'onModified',
        type: '(item) => void',
        description: 'Save, in edit. NOT necessarily a PUT — amending a terminal item forks a '
            + 'new version (§3.4 rule 16), and that is the consumer’s decision.'
    },
    {
        name: 'onRemoved',
        type: '(item) => void',
        description: 'Raised after the ConfirmDialog is accepted, never before it.'
    },
    {
        name: 'onCancelled',
        type: '() => void',
        description: 'Cancel, in add or edit. In edit the panel has already returned to read '
            + 'with the original values by the time this fires.'
    },
    {
        name: 'onModeChanged',
        type: '(mode) => void',
        description: 'Raised when the reader moves between read and edit.'
    },
    {
        name: 'entityType',
        type: 'string',
        defaultValue: "'ContentItem'",
        description: 'Names the entity so the §18.6 role names can be composed. Only ContentItem '
            + 'carries the content-type tier (rule 5).'
    },
    {
        name: 'blockRoles',
        type: 'string',
        defaultValue: 'ReadOnly, ContentItem-ReadOnly, ContentItem-{ContentType}-ReadOnly',
        description: 'Asked FIRST on every gate and outranking every grant, [OWNER] included '
            + '(#366). The narrow tier lands on the picker, so one blocked type disables its '
            + 'tile and leaves the rest of the form live.'
    },
    {
        name: 'addRoles',
        type: 'string',
        defaultValue: "''",
        description: 'Empty means any authenticated reader may contribute — there is no '
            + 'Contributor role (§18.6). [OWNER] is meaningless here and is ignored.'
    },
    {
        name: 'editRoles',
        type: 'string',
        defaultValue: '[OWNER], Publishers, …-Publishers, Administrators',
        description: 'The owner at any status; the rest of the tier only while the item is '
            + 'Draft or Submitted. The Reviewers tier appears in none of the sets.'
    },
    {
        name: 'deleteRoles',
        type: 'string',
        defaultValue: '[OWNER], Administrators',
        description: 'Removal is a takedown, not a moderation step (§14.7 posture A.3), so the '
            + 'Publisher tier does not get it.'
    },
];

export function ContentItemDetailPanelDoc() {
    useDocumentTitle('Content Item Detail Panel — Glory 2 Him');
    const { userRoles } = useAuth();
    const [lastEvent, setLastEvent] = useState('—');

    return (
        <ComponentDoc
            name="Content Item Detail Panel"
            filePath="src/components/contentItems/contentItemDetailPanel.tsx"
            summary={
                <>
                    One content item, in the three states it has: contributed, read, and amended.
                    A <strong>pure presentation component</strong> — props in, events out, no
                    fetching and no mutation — carrying the field shaping its content type
                    dictates and the render gates &sect;18.6 composes.
                </>
            }>

            <DocSection
                title="Modes"
                lead={
                    <>
                        The mode is derived from whether an item was handed over, and{' '}
                        <code>mode</code> overrides that. The <strong>content type is
                        create-only</strong> (&sect;12.4.1 rule 7a): an item may not be relabelled
                        into a type its content was never checked against, so the picker renders in{' '}
                        <code>add</code> alone and <code>edit</code> shows a frozen label.
                    </>
                }>
                <div className="table-responsive">
                    <table className="table table-sm align-middle">
                        <thead>
                            <tr>
                                <th scope="col">Mode</th>
                                <th scope="col">Entered when</th>
                                <th scope="col">Shows</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td><code>add</code></td>
                                <td>no <code>contentItem</code> prop</td>
                                <td>the type picker, the editable fields, Submit for review /
                                    Cancel</td>
                            </tr>
                            <tr>
                                <td><code>read</code></td>
                                <td>a <code>contentItem</code> prop is given</td>
                                <td>the rendered item, plus Edit / Delete where the roles allow</td>
                            </tr>
                            <tr>
                                <td><code>edit</code></td>
                                <td>the reader presses Edit, or <code>mode=&quot;edit&quot;</code></td>
                                <td>the editable fields with Save / Cancel; Cancel returns to read
                                    with the original values</td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <CodeSample code={minimalSample} />
            </DocSection>

            <DocSection
                title="Add"
                lead={
                    <>
                        The surface <code>/posts/contribute</code> renders. Each tile is one{' '}
                        <code>ContentItemSetting</code> the consumer handed over, and the chosen
                        one decides which fields exist — a quote below carries no title of its own.
                    </>
                }>
                <LiveDemo>
                    <ContentItemDetailPanel
                        contentItemSettingCollection={demoSettings}
                        onAdded={(item) =>
                            setLastEvent(`onAdded(${ContentType[item.contentType]})`)}
                        onCancelled={() => setLastEvent('onCancelled()')} />
                </LiveDemo>

                <p className="small text-body-secondary">
                    Last event: <code>{lastEvent}</code>
                </p>
            </DocSection>

            <DocSection
                title="Validation comes back from the API, not from here"
                lead={
                    <>
                        The panel judges nothing. The submission goes, and the{' '}
                        <code>errors</code> dictionary of whatever{' '}
                        <code>ValidationProblemDetails</code> comes back marks up the form —
                        matched onto the fields case-insensitively, because the keys are the
                        server&rsquo;s parameter names. A message it cannot place on a field is
                        summarised rather than dropped.
                    </>
                }>
                <LiveDemo>
                    <ContentItemDetailPanel
                        contentItemSettingCollection={demoSettings}
                        validationIssues={{
                            Content: ['Text is required'],
                            Title: ['Text is required'],
                            ContentHash: ['A content item already exists with the same content.']
                        }} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="The read surface, whole"
                lead={
                    <>
                        The type&rsquo;s chip, the title at the heading level the page asked for,
                        and the byline: who contributed it, who wrote it, and what may be done
                        with it, with the article&rsquo;s figures reading underneath.
                        {' '}
                        <strong>None of it is fetched here.</strong> The item carries only{' '}
                        <code>createdBy</code>, an account id — the consumer resolves that against{' '}
                        <code>/api/contributors/{'{id}'}</code> and hands over the name and the
                        avatar, and the counts are its to gather too.
                        {' '}
                        Every part is optional: hand over nothing and the block disappears rather
                        than showing a placeholder under somebody&rsquo;s testimony.
                    </>
                }>
                <LiveDemo>
                    <ContentItemDetailPanel
                        contentItem={storyByAnother}
                        contentItemSettingCollection={demoSettings}
                        titleHeadingLevel="h2"
                        submittedByDisplayName="Louis Ferguson"
                        readingTimeMinutes={5}
                        reactionCount={257}
                        commentCount={4}
                        viewCount={2344} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="An owned basis prefills the author, it does not remove it"
                lead={
                    <>
                        Choosing one of the <em>It&rsquo;s my own</em> options puts the
                        contributor&rsquo;s own display name in the Author field —{' '}
                        <strong>the field stays</strong>, because the basis says who wrote it but
                        not what they want to be called for it, and a contributor may publish
                        under a pen name, an initial, or a maiden name.
                        {' '}
                        The prefill fills an <em>empty</em> field and only until they touch it, so
                        opening an existing contribution never overwrites the author already on
                        it, and a name they typed is never reverted. On an amendment the name
                        comes from the <em>submitter</em>, not from whoever opened the editor.
                        {' '}
                        Try the add demo at the top of this page: the Author box arrives filled
                        in, and switching to <em>It&rsquo;s public domain</em> empties it again.
                    </>
                }>
                <LiveDemo title="Read — the author IS the submitter, so it is said once">
                    <ContentItemDetailPanel
                        contentItem={storyReleasedByItsAuthor}
                        contentItemSettingCollection={demoSettings}
                        titleHeadingLevel="h2"
                        submittedByDisplayName="Grace Abara"
                        readingTimeMinutes={5}
                        reactionCount={1}
                        commentCount={0} />
                </LiveDemo>

                <LiveDemo title="Read — a pen name, which the submitter column does not say">
                    <ContentItemDetailPanel
                        contentItem={storyUnderAPenName}
                        contentItemSettingCollection={demoSettings}
                        titleHeadingLevel="h2"
                        submittedByDisplayName="Grace Abara"
                        readingTimeMinutes={5}
                        reactionCount={1}
                        commentCount={0} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Read, with editing switched off"
                lead={
                    <>
                        <code>isEditingAllowed</code> is <strong>the surface switch, ahead of
                        every role check</strong>, and it is off by default. However the
                        reader&rsquo;s roles fall — you are currently{' '}
                        <code>{userRoles.join(', ') || 'no roles'}</code> — this demo offers no
                        Edit, no Delete and no route into the edit mode. A public page renders the
                        panel exactly like this and cannot accidentally become an edit surface.
                    </>
                }>
                <LiveDemo>
                    <ContentItemDetailPanel
                        contentItem={storyByAnother}
                        contentItemSettingCollection={demoSettings} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Read, with editing switched on"
                lead={
                    <>
                        The same panel with the switch thrown: the role gates now decide, per
                        action, what is shown. The item below is <code>Submitted</code> and
                        belongs to somebody else, so an administrator gets both actions and a
                        plain reader still gets none — the switch only ever subtracts.
                    </>
                }>
                <LiveDemo>
                    <ContentItemDetailPanel
                        contentItem={storyByAnother}
                        contentItemSettingCollection={demoSettings}
                        isEditingAllowed
                        onModified={(item) => setLastEvent(`onModified(${item.id})`)}
                        onRemoved={(item) => setLastEvent(`onRemoved(${item.id})`)}
                        onModeChanged={(mode) => setLastEvent(`onModeChanged(${mode})`)} />
                </LiveDemo>

                <p className="small text-body-secondary">
                    <strong>Delete confirms first.</strong>{' '}
                    <code>ConfirmDialog</code> asks before <code>onRemoved</code> is raised —
                    removal is a takedown, and it is offered to the owner or an administrator
                    only.
                </p>
            </DocSection>

            <DocSection
                title="A decided item is terminal to everyone but its owner"
                lead={
                    <>
                        The same panel over an <code>Approved</code> item. The publisher tier and{' '}
                        <code>Administrators</code> lose the Edit affordance here: amending a
                        decided item is a fork of a new version, not an in-place edit (&sect;3.4
                        rule 16). Its owner keeps it at any status — and the consumer decides
                        whether that raises a <code>PUT</code> or a fork.
                    </>
                }>
                <LiveDemo>
                    <ContentItemDetailPanel
                        contentItem={approvedStory}
                        contentItemSettingCollection={demoSettings}
                        isEditingAllowed
                        onModified={(item) => setLastEvent(`onModified(${item.id})`)}
                        onRemoved={(item) => setLastEvent(`onRemoved(${item.id})`)} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Settings resolve here — most specific wins"
                lead={
                    <>
                        Hand over whatever rows you hold. The panel resolves the{' '}
                        <strong>effective</strong> setting itself, as &sect;6.4 and &sect;12.5.2
                        rules 1&ndash;2 require: an <strong>item-level override</strong> (the row
                        whose <code>ContentItemId</code> is this item&rsquo;s) takes{' '}
                        <strong>full precedence</strong> over the content type default, and a
                        soft-deleted row is excluded from resolution entirely (&sect;6.6). The
                        override is matched on the <em>item</em> as well as the type, so a mixed
                        collection is safe &mdash; one item&rsquo;s override is never applied to
                        another&rsquo;s.
                    </>
                }>
                <LiveDemo title="Live — default only">
                    {/* The Story default has hasAuthor: true, so the byline renders. */}
                    <ContentItemDetailPanel
                        contentItem={storyByAnother}
                        contentItemSettingCollection={demoSettings} />
                </LiveDemo>

                <LiveDemo title="Live — the same item, with its override in the collection">
                    {/* Same item, same props, one extra row: the override drops the author. */}
                    <ContentItemDetailPanel
                        contentItem={storyByAnother}
                        contentItemSettingCollection={[...demoSettings, storyOverride]} />
                </LiveDemo>

                <p className="small text-body-secondary">
                    <code>add</code> can only ever resolve a <strong>default</strong> &mdash; an
                    override belongs to one existing item, and there is no item yet. That is also
                    why an override is never a tile in the picker, however the collection arrived.
                    The picker offers the defaults carrying{' '}
                    <code>isAvailableAsGeneralUserContribution</code>, which is exactly the
                    question a tile asks, and orders them by each row&rsquo;s own{' '}
                    <code>sortOrder</code> &mdash; lowest first, ties keeping the order they
                    arrived in. Hand the collection over in any order; the panel sorts it, and
                    lands on the first tile in that order.
                </p>

                <p className="small text-body-secondary">
                    What the panel reads off the resolved row is the <strong>field shaping and
                    the type&rsquo;s presentation</strong>: <code>hasTitle</code>,{' '}
                    <code>hasAuthor</code>, <code>contentTypeName</code>,{' '}
                    <code>contentTypeDescription</code>,{' '}
                    <code>contentTypeIconCssClass</code>. <code>hasTitle</code> and{' '}
                    <code>hasAuthor</code> govern all three surfaces &mdash; the input in{' '}
                    <code>add</code> and <code>edit</code>, the heading and byline in{' '}
                    <code>read</code>. The facet pairs (&sect;6.5 &mdash;
                    tags, comments, reactions, links, attachments, bible references) govern
                    surfaces this panel does not own; the panels that render <em>beside</em> it
                    read those, against this same effective row.
                </p>

                <p className="small text-body-secondary">
                    <strong>A field the reader cannot see contributes nothing, and the row keeps
                    whatever it already had.</strong> On an <code>edit</code> that means hiding is
                    never destructive: a title already on the row survives an amendment it was not
                    shown for, so a setting changed after the item was written cannot silently
                    blank it. On an <code>add</code> it means the opposite is equally true &mdash;
                    a title typed under Story and then abandoned by picking Quote is{' '}
                    <em>not</em> posted, because the contributor can no longer see it, the type is
                    create-only, and no read surface would ever show it again. Switch back to
                    Story before submitting and what was typed is still there. Where no row
                    resolves at all there is no flag to obey &mdash; the panel then shows
                    whichever of the two the item actually carries.
                </p>

                <p className="small text-body-secondary">
                    <strong><code>sharePermission</code> is the exception, and drops rather than
                    persisting.</strong> It is hidden by the contributor&rsquo;s own answer to a
                    question in front of them, not by a setting they never chose &mdash; so a note
                    reading <em>permission granted by the author</em>, left on an item they have
                    just declared <code>Owned</code>, is a claim they withdrew. Nothing correlates
                    the two server-side and no read surface shows it once the basis has moved, so
                    keeping it would file a contradiction nobody can see or clear.
                </p>
            </DocSection>

            <DocSection
                title="Security posture"
                lead={
                    <>
                        <strong>Every gate decides what to RENDER and nothing more.</strong> The
                        foundation and processing services re-decide add, modify and remove
                        against the stored row (&sect;14.6, &sect;14.7 posture A), and must — a
                        hidden button is a courtesy to the reader, never an authorization
                        boundary.
                    </>
                }>
                <ul className="small text-body-secondary">
                    <li>
                        <strong>Anonymous</strong> gets no form, and a{' '}
                        <em>Login to contribute</em> button in its place, returning to the current
                        path after sign-in. Never simply an empty panel — otherwise a reader
                        cannot tell the page accepts contributions at all.
                    </li>
                    <li>
                        <strong>Blocked</strong> gets a message saying contributions are not open
                        to this account, for the same reason.
                    </li>
                    <li>
                        <strong>Blocked for one content type</strong> keeps the rest of the form
                        live: the tile renders disabled with its reason on it, and only a reader
                        blocked from <em>every</em> available type loses the form.
                    </li>
                    <li>
                        <strong>Read</strong> is everyone&rsquo;s, anonymous included. The actions
                        inside it are gated separately.
                    </li>
                </ul>

                <CodeSample code={persistenceSample} />
            </DocSection>

            <DocSection
                title="Roles"
                lead={
                    <>
                        Composed from <code>entityType</code> and the content type in play,
                        capability <strong>last</strong> and <strong>plural</strong> (&sect;18.6,
                        #368) — <code>ReadOnly</code> alone stays singular, because it names a
                        state its holder is in rather than a group of people. The{' '}
                        <code>Reviewers</code> tier appears in none of the sets: a reviewer
                        reviews.
                    </>
                }>
                <CodeSample code={rolesSample} />
            </DocSection>

            <DocSection
                title="What it deliberately leaves out"
                lead={
                    <>
                        <strong>Tags and bible references.</strong>{' '}
                        <code>AssociationPanel</code> and its two wrappers already own that
                        surface with their own approval and role rules, and they render{' '}
                        <em>beside</em> this panel on the page rather than within it. Approval
                        controls belong to <code>ReviewPanel</code>. There is no{' '}
                        <code>useQuery</code>, no <code>useMutation</code> and no broker call
                        anywhere inside.
                    </>
                } />

            <DocSection title="Props">
                <PropsTable rows={propRows} />
            </DocSection>
        </ComponentDoc>
    );
}
