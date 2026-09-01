import { useDocumentTitle } from '../../useDocumentTitle';

import {
    contentItemElementShape
} from './shared/contentItemShapeSamples';
import { demoStoryItem } from './shared/contentItemDemoData';
import { ContentItemPanelPlayground } from './shared/contentItemPanelPlayground';

import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DocSection,
    PropsTable
} from './shared/componentDoc';

const familySample = `
ContentItemPanel
├── ContentItemAddPanel
├── ContentItemEditPanel
├── ContentItemDefaultPanel       ◄ this page (the view template most types use)
└── ContentItem{ContentType}Panel   overrides DERIVE from this one, via contentSlot
`;

const templateProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'contentItem',
        type: 'ContentItemSearchItem',
        description: 'The self-contained element: the item and its winning setting travel '
            + 'together. The template reads nothing beyond it.'
    },
    {
        name: 'contentSlot',
        type: 'ReactNode?',
        description: 'THE DERIVATION POINT. Absent, the default content block renders '
            + '(thumbnail, badge and title, the truncated content, read-more). An override — '
            + 'Quotes, Verse Images — renders THIS template with contentSlot replaced, so the meta '
            + 'row, the pills and the engagement row are written once and carried identically.'
    },
    {
        name: 'showsEditButton / showsModerateButton',
        type: 'boolean',
        description: 'Decided ONCE in ContentItemPanel — ownership for Edit, the moderation '
            + 'tier for Moderate — and handed over decided. A template only renders them; '
            + 'moderateButtonIconCss and moderateButtonLabel arrive resolved the same way.'
    },
    {
        name: 'areReactionCountsExpanded / isReactionPickerOpen',
        type: 'boolean',
        description: 'The two per-card render toggles, owned by the dispatching panel’s '
            + 'state and handed over decided — the assigned cluster’s compact⇄counts face, '
            + 'and whether the Like picker stands open.'
    },
    {
        name: 'showApprovalStatusRibbon / showApprovalStatus',
        type: 'boolean',
        description: 'The status pair, threaded from the surface: the corner ribbon on the '
            + 'card ROOT (every derived template wears it identically) and the status pill '
            + 'beside the type chip — each showing every status once asked, Approved '
            + 'included.'
    },
    {
        name: 'truncateAt / allowInPlaceExpansion / isContentExpanded',
        type: 'number / boolean / boolean',
        description: 'The content length, decided by the dispatcher: cut at truncateAt with '
            + 'an ellipsis and the read-more affordance while collapsed, whole when '
            + 'expanded — and read-more either raises the page’s onReadMore or toggles '
            + 'the expansion in place, never both.'
    },
    {
        name: 'showTagSection / showBibleReferenceSection / showReactionSection / '
            + 'showCommentsSection / showShareSection / showSaveSection',
        type: 'boolean',
        defaultValue: 'true',
        description: 'The section switches, ANDed with the setting on the element: the '
            + 'setting says what the type shows, the switch says what this surface has room '
            + 'for. A section renders only when both agree.'
    },
    {
        name: 'onTitleClick / onReadMore / …',
        type: '(item) => void',
        description: 'The event hooks, unchanged across the family — a control renders only '
            + 'where somebody is listening: no onTitleClick and the title stands as plain '
            + 'heading text.'
    }
];

export function ContentItemDefaultPanelDoc() {
    useDocumentTitle('Content Item Default Panel — Components — Glory 2 Him');

    return (
        <ComponentDoc
            name="Content Item Default Panel"
            filePath="src/components/contentItems/contentItemDefaultPanel.tsx"
            summary="The view template most content types render through: type badge and
                status pill, content block, meta row, tag and reference pills, and the
                engagement row. The per-type overrides derive from it by replacing
                contentSlot alone.">

            <DocSection
                title="Where it stands in the family"
                lead={
                    <>
                        A template renders a FULLY DECIDED bundle: ownership, the moderation
                        tier, the reaction gating, the status pair and the content length are
                        all <code>ContentItemPanel</code>&rsquo;s decisions, handed over made.
                        The playground below therefore drives the DISPATCHER — the same
                        control surface every page in the family carries — and this template
                        renders what it decides.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={templateProps} />
            </DocSection>

            <DocSection
                title="The shapes"
                lead={
                    <>
                        The element this template renders from — including the winning setting that rides on it, which every gate below reads.
                    </>
                }>
                <CodeSample
                    code={contentItemElementShape}
                    caption="contentItem — the self-contained element" />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        The full family control surface — security context, ribbon status and
                        every threaded switch — over a story rendering through this template.
                        As an owner, Edit swaps the card for the editor in place and Save
                        swaps the element back with the amendments.
                    </>
                }>
                <ContentItemPanelPlayground contentItem={demoStoryItem} />
            </DocSection>
        </ComponentDoc>
    );
}
