import { scopedModerationRoles } from './associationRoles';
import { AssociationPanel, AssociationPanelProps } from './associationPanel';
import { AssociationItem } from '../../models/components/associations/associationItem';
import { bibleReferenceHref } from '../../services/views/bibleReferences/toUsfmReference';

// AssociationPanel dressed as the bible reference panel: blue chips carrying a book icon once
// approved, each addressing the passage itself. The chip reads as the post cites it
// ("Joshua 10:8, 12–13") and links as the deep-link route parses it, which is why the href comes
// from bibleReferenceHref rather than from the label.
//
// The book is set as approvedIconCssClass rather than as a flat chip icon so a reference still
// waiting on a decision shows the hourglass instead — one icon slot, filled by whichever status
// applies.
//
// Defaults are destructured rather than spread, for the same undefined-safety reason as
// TagAssociationPanel.
export type BibleReferenceAssociationPanelProps = Partial<AssociationPanelProps>;

const referenceHref = (item: AssociationItem): string => bibleReferenceHref(item.value);

export function BibleReferenceAssociationPanel({
    title = 'Bible references',
    suggestTitle = 'Suggest a bible reference',
    suggestDescription = 'Know a matching verse? Suggest it below.',
    addPlaceholderText = 'e.g. Romans 3:23…',
    chipCssClass = 'btn-primary-soft',
    approvedIconCssClass = 'bi-book',
    chipHrefFor = referenceHref,
    loginButtonText = 'Login to suggest a bible reference',
    showAdd = true,
    // §18.6: the global tier plus the BibleReference-scoped pair, so a moderator trusted with
    // bible references alone — without holding the global Reviewers/Publishers role — can still
    // decide on one.
    moderationRoles = scopedModerationRoles('BibleReference'),
    ...rest
}: BibleReferenceAssociationPanelProps) {
    return (
        <AssociationPanel
            title={title}
            suggestTitle={suggestTitle}
            suggestDescription={suggestDescription}
            addPlaceholderText={addPlaceholderText}
            chipCssClass={chipCssClass}
            approvedIconCssClass={approvedIconCssClass}
            chipHrefFor={chipHrefFor}
            loginButtonText={loginButtonText}
            showAdd={showAdd}
            moderationRoles={moderationRoles}
            {...rest} />
    );
}
