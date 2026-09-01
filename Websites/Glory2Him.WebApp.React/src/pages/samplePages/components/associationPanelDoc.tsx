import { useState } from 'react';
import { AssociationPanel } from '../../../components/associations/associationPanel';
import { useAuth } from '../../../components/securitys/authProvider';
import {
    ApprovalStatus,
    AssociationItem
} from '../../../models/components/associations/associationItem';
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
import { AssociationPanel } from '../../components/associations/associationPanel';

<AssociationPanel
    title="Tags"
    associationCollection={items}
    chipHrefFor={(item) => \`/Search?q=\${item.value}\`}
    showAdd={true}
    onAdd={(value) => addTagAsync(value)} />
`;

const moderationSample = `
// The DEFAULT posture — showModerationActions omitted, so it is off. Chips render by the
// visibility gates and the panel is read-only, except that the contributor may still withdraw
// their own unapproved suggestion. This is what an ordinary page wants.
<AssociationPanel
    title="Tags"
    associationCollection={items}
    showAdd={true}
    onAdd={addTagAsync}
    onRemove={removeTagAsync} />

// A moderation surface switches the actions on. Now removeRoles and moderationRoles decide,
// and a moderator sees Remove, Reject and Approve on somebody else's submission.
<AssociationPanel
    title="Tags"
    associationCollection={items}
    showModerationActions={true}
    onRemove={removeTagAsync}
    onReject={rejectTagAsync}
    onApprove={approveTagAsync} />
`;

const rolesSample = `
// [OWNER] resolves per item — the contributor of THAT row — so it cannot be an ordinary
// role name. Any other listed role allows the action outright.
<AssociationPanel
    title="Tags"
    removeRoles="[OWNER], Administrators, Tag-Reviewers"
    moderationRoles="Administrators, Tag-Reviewers"
    addRoles=""
    showModerationActions={true}
    onApprove={approveAsync}
    onReject={rejectAsync}
    onRemove={removeAsync} />
`;

const modelSample = `
// Project whatever you hold — a Tag, a BibleReference, an Association row — down to this.
export type AssociationItem = {
    value: string;              // what the chip reads
    createdBy?: string;         // the account id, for the [OWNER] rule
    approvalStatus?: ApprovalStatus;
    isDeleted?: boolean;        // soft deletion — never rendered, whatever the role
    id?: string;                // stable React key; falls back to value
};
`;

const propRows: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'title', type: 'string', defaultValue: '(required)',
        description: 'Panel heading. Also names the add box for assistive tech.'
    },
    {
        name: 'showBorder', type: 'boolean', defaultValue: 'false',
        description: 'Wraps the panel in the bordered card ContributionPrompt uses.'
    },
    {
        name: 'associationCollection', type: 'AssociationItem[]', defaultValue: '[]',
        description: 'The chips. The parent owns this list — the panel never mutates it.'
    },
    {
        name: 'chipCssClass', type: 'string', defaultValue: "'btn-success-soft'",
        description: 'Theme class carrying the chip look. Use a class, not a colour, so the chip follows light and dark mode.'
    },
    {
        name: 'chipPrefixText', type: 'string', defaultValue: "''",
        description: 'Literal prefix in front of every value, such as a hash. Not part of the stored value.'
    },
    {
        name: 'chipIconCssClass', type: 'string', defaultValue: '—',
        description: 'Leading icon used when no status-specific icon applies.'
    },
    {
        name: 'approvedIconCssClass', type: 'string', defaultValue: '—',
        description: 'Leading icon on an Approved chip.'
    },
    {
        name: 'pendingIconCssClass', type: 'string', defaultValue: "'bi-hourglass-split'",
        description: 'Leading icon on a Draft or Submitted chip.'
    },
    {
        name: 'rejectedIconCssClass', type: 'string', defaultValue: "'bi-slash-circle'",
        description: 'Leading icon on a Rejected or Dismissed chip.'
    },
    {
        name: 'chipHrefFor', type: '(item) => string', defaultValue: '—',
        description: 'Renders the chip label as a link. Preferred over chipOnClick for navigation — a real link can be middle-clicked and is announced as a destination.'
    },
    {
        name: 'chipOnClick', type: '(item) => void', defaultValue: '—',
        description: 'Renders the chip label as a button. Ignored when chipHrefFor is set.'
    },
    {
        name: 'isLoading', type: 'boolean', defaultValue: 'false',
        description: 'Shows a loading line instead of the chips or the empty text.'
    },
    {
        name: 'emptyText', type: 'string', defaultValue: "''",
        description: 'Shown when nothing is visible. Empty renders nothing at all.'
    },
    {
        name: 'viewAllRoles', type: 'string (csv)', defaultValue: "'Administrators'",
        description: 'Roles that may see an item in any status — a draft and a refusal included. The widest grant, hence administrators-only by default.'
    },
    {
        name: 'showModerationActions', type: 'boolean', defaultValue: 'false',
        description: 'The single switch over Remove, Reject and Approve. Off is the safe posture: read-only, except that a contributor may still withdraw their own unapproved item. Turn it on for a moderation surface only.'
    },
    {
        name: 'removeRoles', type: 'string (csv)', defaultValue: "'[OWNER], Administrators'",
        description: 'Who may remove. Empty means any authenticated reader. [OWNER] is resolved per item.'
    },
    {
        name: 'onRemove', type: '(item) => void', defaultValue: '—',
        description: 'Raised with the item when Remove is used.'
    },
    {
        name: 'removeTooltip', type: 'string', defaultValue: "'Remove'",
        description: 'Tooltip and accessible name prefix on the Remove control.'
    },
    {
        name: 'removeButtonCssClass', type: 'string', defaultValue: "'btn-danger'",
        description: 'Theme class for the Remove block.'
    },
    {
        name: 'moderationRoles', type: 'string (csv)',
        defaultValue: "'Reviewers, Publishers, Administrators'",
        description: 'Who may decide. [OWNER] is ignored — owning the item suppresses the pair rather than granting it.'
    },
    {
        name: 'onApprove / onReject', type: '(item) => void', defaultValue: '—',
        description: 'Raised with the item when a decision is made.'
    },
    {
        name: 'approveButtonCssClass / rejectButtonCssClass', type: 'string',
        defaultValue: "'btn-success' / 'btn-warning'",
        description: 'Theme classes for the two decision blocks.'
    },
    {
        name: 'showAdd', type: 'boolean', defaultValue: 'false',
        description: 'Visibility gate for the suggestion box. Signed-out readers get the login prompt instead.'
    },
    {
        name: 'addRoles', type: 'string (csv)', defaultValue: "''",
        description: 'Further restricts who may suggest. Empty means any authenticated reader.'
    },
    {
        name: 'suggestTitle / suggestDescription', type: 'string', defaultValue: "''",
        description: 'Heading and prompt above the suggestion box. Each renders only when set.'
    },
    {
        name: 'addPlaceholderText', type: 'string', defaultValue: "''",
        description: 'Placeholder inside the suggestion box.'
    },
    {
        name: 'addMaxLength', type: 'number', defaultValue: '100',
        description: 'Caps what can be typed, matching the storage cap.'
    },
    {
        name: 'showAddButton / addButtonText', type: 'boolean / string',
        defaultValue: "false / 'Add'",
        description: 'An explicit add button beside the box. Enter commits either way.'
    },
    {
        name: 'onAdd', type: '(value: string) => void', defaultValue: '—',
        description: 'Raised once with the normalized value, after the duplicate and empty checks. Nothing is appended internally.'
    },
    {
        name: 'normalizeAddedValue', type: '(raw: string) => string', defaultValue: 'trim',
        description: 'Applied before the duplicate check and before onAdd.'
    },
    {
        name: 'loginHref', type: 'string', defaultValue: 'current path',
        description: 'Where the login prompt points, carrying a returnUrl by default.'
    },
    {
        name: 'loginButtonText / loginButtonCssClass', type: 'string',
        defaultValue: "'Login to suggest' / 'btn-outline-primary'",
        description: 'Label and theme class for the login prompt.'
    },
    {
        name: 'loginButtonOnClick', type: '() => void', defaultValue: '—',
        description: 'Renders the prompt as a button instead of a link.'
    }
];

// Matrix cells. A tick or a cross carries further at a glance than "Yes" / "No", and a coloured
// dot ties each action back to the button the reader will actually see on the chip. The word is
// kept alongside so the tables still read correctly to a screen reader and in a copy-paste.
const Yes = () => <td className="text-nowrap"><span aria-hidden="true">✅</span> Yes</td>;
const No = () => <td className="text-nowrap"><span aria-hidden="true">❌</span> No</td>;

const None = () => <td className="text-nowrap text-body-secondary">—</td>;

const Remove = () => <td className="text-nowrap"><span aria-hidden="true">🔴</span> Remove</td>;

const Verdict = () => (
    <td className="text-nowrap">
        <span aria-hidden="true">🟡</span> Reject + <span aria-hidden="true">🟢</span> Approve
    </td>
);

const RemoveAndVerdict = () => (
    <td>
        <span className="text-nowrap"><span aria-hidden="true">🔴</span> Remove</span>
        {' + '}
        <span className="text-nowrap"><span aria-hidden="true">🟡</span> Reject</span>
        {' + '}
        <span className="text-nowrap"><span aria-hidden="true">🟢</span> Approve</span>
    </td>
);

export const AssociationPanelDoc = () => {
    useDocumentTitle('Association Panel — Glory 2 Him');

    const { user } = useAuth();
    const viewerId = user?.userId ?? 'demo-viewer';

    const [items, setItems] = useState<ReadonlyArray<AssociationItem>>([
        {
            id: '1', value: 'creation',
            createdBy: 'another-user', approvalStatus: ApprovalStatus.Approved
        },
        {
            id: '2', value: 'science',
            createdBy: 'another-user', approvalStatus: ApprovalStatus.Approved
        },
        {
            id: '3', value: 'awaiting-someone-elses',
            createdBy: 'another-user', approvalStatus: ApprovalStatus.Submitted
        },
        {
            id: '4', value: 'awaiting-mine',
            createdBy: viewerId, approvalStatus: ApprovalStatus.Submitted
        }
    ]);

    const withStatus = (item: AssociationItem, approvalStatus: ApprovalStatus) =>
        setItems(items.map((existing) =>
            existing.id === item.id ? { ...existing, approvalStatus } : existing));

    return (
        <ComponentDoc
            name="Association Panel"
            filePath="src/components/associations/associationPanel.tsx"
            summary={
                <>
                    A labelled set of association chips — a post's tags, its bible references,
                    anything projected to <code>AssociationItem</code> — with an optional box
                    beneath for suggesting another. Every gate below decides what to
                    <strong> render</strong>; the foundation services re-decide add, delete and
                    approval against the stored row themselves, so a hidden button is a courtesy
                    to the reader and never an authorization boundary.
                </>
            }>

            <DocSection
                title="Live"
                lead={
                    <>
                        Wired to local state, so adding, removing and deciding all take effect
                        here. What you can do depends on the roles you hold and on who
                        contributed each chip — the last one is yours.
                    </>
                }>
                <LiveDemo>
                    <AssociationPanel
                        title="Tags"
                        associationCollection={items}
                        chipPrefixText="#"
                        chipHrefFor={(item) => `/Search?q=${encodeURIComponent(item.value)}`}
                        showAdd={true}
                        showModerationActions={true}
                        suggestTitle="Suggest a tag"
                        suggestDescription="Think a tag is missing? Suggest one and help others find this post."
                        addPlaceholderText="Start typing a tag…"
                        emptyText="No tags yet."
                        normalizeAddedValue={(raw) => raw.trim().replace(/^#+/, '')}
                        onAdd={(value) => setItems([...items, {
                            id: value,
                            value,
                            createdBy: viewerId,
                            approvalStatus: ApprovalStatus.Submitted
                        }])}
                        onRemove={(item) =>
                            setItems(items.filter((existing) => existing.id !== item.id))}
                        onApprove={(item) => withStatus(item, ApprovalStatus.Approved)}
                        onReject={(item) => withStatus(item, ApprovalStatus.Rejected)} />
                </LiveDemo>
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={propRows} />
            </DocSection>

            <DocSection title="Minimal usage">
                <CodeSample code={minimalSample} />
            </DocSection>

            <DocSection
                title="The item model"
                lead="The panel never depends on any one entity — a page projects whatever it holds down to this shape.">
                <CodeSample code={modelSample} />
            </DocSection>

            <DocSection
                title="Which chips render"
                lead={
                    <>
                        With <code>hideUnapprovedFromOthers</code> on (the default), and the
                        defaults <code>viewAllRoles="Administrators"</code> and{' '}
                        <code>moderationRoles="Reviewers, Publishers, Administrators"</code>.
                    </>
                }>
                <div className="table-responsive">
                    <table className="table table-sm align-middle">
                        <thead>
                            <tr>
                                <th scope="col">Status</th>
                                <th scope="col">Anonymous</th>
                                <th scope="col">Signed-in reader</th>
                                <th scope="col">Owner</th>
                                <th scope="col">Reviewer</th>
                                <th scope="col">Publisher</th>
                                <th scope="col">Administrator</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td>Draft</td>
                                <No /><No /><Yes />
                                <No /><No /><Yes />
                            </tr>
                            <tr>
                                <td>Submitted</td>
                                <No /><No /><Yes />
                                <Yes /><Yes /><Yes />
                            </tr>
                            <tr>
                                <td>Approved</td>
                                <Yes /><Yes /><Yes />
                                <Yes /><Yes /><Yes />
                            </tr>
                            <tr>
                                <td>Rejected / Dismissed</td>
                                <No /><No /><No />
                                <No /><No /><Yes />
                            </tr>
                            <tr>
                                <td>Removed (soft deleted)</td>
                                <No /><No /><No />
                                <No /><No /><No />
                            </tr>
                        </tbody>
                    </table>
                </div>

                <p className="small text-body-secondary">
                    Three grants, widest first. <strong>Removal outranks everything</strong>,
                    approval included — an <code>isDeleted</code> row is gone even to an
                    administrator, and even with the filter switched off.{' '}
                    <code>viewAllRoles</code> opens every status, a draft and a refusal included,
                    which is why it is administrators-only. <code>moderationRoles</code> stops at
                    submissions: a draft was never put forward for anyone to judge, and a refusal
                    has already been judged. And the contributor follows their own suggestion until
                    it is decided, not past it.
                </p>

                <p className="small text-body-secondary">
                    Every cell obeys one rule, with no exceptions: <strong>an unapproved chip is
                    visible only to someone who has an action available on it</strong>. A rejected
                    suggestion never lingers on the post that refused it, and a draft stays between
                    its author and the administrators who could clear it.
                </p>
            </DocSection>

            <DocSection
                title="Which action appears"
                lead={
                    <>
                        Using the defaults <code>removeRoles="[OWNER], Administrators"</code>{' '}
                        and <code>moderationRoles="Reviewers, Publishers, Administrators"</code>. The
                        owner branch is resolved <strong>first</strong>, so an administrator who
                        contributed the item gets a removal rather than a verdict — nobody waves
                        through their own submission.
                    </>
                }>
                <div className="table-responsive">
                    <table className="table table-sm align-middle">
                        <thead>
                            <tr>
                                <th scope="col">Status</th>
                                <th scope="col">Anonymous</th>
                                <th scope="col">Reader</th>
                                <th scope="col">Owner</th>
                                <th scope="col">Reviewer</th>
                                <th scope="col">Publisher</th>
                                <th scope="col">Administrator (not owner)</th>
                                <th scope="col">Administrator who owns it</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td>Draft</td>
                                <None /><None /><Remove />
                                <None /><None />
                                <Remove /><Remove />
                            </tr>
                            <tr>
                                <td>Submitted</td>
                                <None /><None /><Remove />
                                <Verdict /><Verdict />
                                <RemoveAndVerdict /><Remove />
                            </tr>
                            <tr>
                                <td>Approved</td>
                                <None /><None /><None />
                                <None /><None />
                                <Remove /><Remove />
                            </tr>
                            <tr>
                                <td>Rejected / Dismissed</td>
                                <None /><None /><None />
                                <None /><None />
                                <Remove /><Remove />
                            </tr>
                            <tr>
                                <td>Removed (soft deleted)</td>
                                <None /><None /><None />
                                <None /><None />
                                <None /><None />
                            </tr>
                        </tbody>
                    </table>
                </div>

                <p className="small text-body-secondary">
                    Reviewer and Publisher decide but cannot remove, because Remove is
                    <code> removeRoles</code>' to grant and that list is Administrators-only by
                    default. <code>createdBy</code> is matched against the account id only — the
                    same value <code>/api/accounts/me</code> returns as <code>userId</code> — never
                    a display name, which two accounts can share.
                </p>
            </DocSection>

            <DocSection
                title="Moderation is independent of deletion"
                lead="Turning decisions off while leaving withdrawal on is the normal shape for a panel whose approvals happen elsewhere.">
                <CodeSample code={moderationSample} />
            </DocSection>

            <DocSection title="Roles">
                <CodeSample code={rolesSample} />
            </DocSection>
        </ComponentDoc>
    );
};
