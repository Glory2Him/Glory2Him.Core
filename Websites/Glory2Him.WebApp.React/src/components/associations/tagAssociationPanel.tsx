import { AssociationPanel, AssociationPanelProps } from './associationPanel';
import { AssociationItem } from '../../models/components/associations/associationItem';

// AssociationPanel dressed as the tag panel: green chips, a hash in front of each, and a search
// for anything clicked. Every base prop stays overridable — the defaults below only fill the gaps
// — so <TagAssociationPanel associationCollection={tags} /> matches the post-detail layout, and
// <TagAssociationPanel title="Topics" /> re-labels it without giving up the rest.
//
// Defaults are applied by DESTRUCTURING rather than by spreading a defaults object, because
// default parameters kick in on undefined: a caller passing a value that happens to be undefined
// (title={maybeTitle}) still lands on the default rather than blanking the heading.
export type TagAssociationPanelProps = Partial<AssociationPanelProps>;

// A leading hash is how people write tags, but it is not part of the tag itself.
const normalizeTag = (rawValue: string): string => rawValue.trim().replace(/^#+/, '');

const tagSearchHref = (item: AssociationItem): string =>
    `/Search?q=${encodeURIComponent(item.value)}`;

export function TagAssociationPanel({
    title = 'Tags',
    suggestTitle = 'Suggest a tag',
    suggestDescription = 'Think a tag is missing? Suggest one and help others find this post.',
    addPlaceholderText = 'Start typing a tag…',
    chipCssClass = 'btn-success-soft',
    chipPrefixText = '#',
    chipHrefFor = tagSearchHref,
    normalizeAddedValue = normalizeTag,
    loginButtonText = 'Login to suggest a tag',
    showAdd = true,
    // §18.6: the global tier plus the Tag-scoped pair, so a moderator trusted with tags alone
    // — without holding the global Reviewers/Publishers role — can still decide on one.
    moderationRoles = 'Reviewers, Publishers, Administrators, Tag-Reviewers, Tag-Publishers',
    ...rest
}: TagAssociationPanelProps) {
    return (
        <AssociationPanel
            title={title}
            suggestTitle={suggestTitle}
            suggestDescription={suggestDescription}
            addPlaceholderText={addPlaceholderText}
            chipCssClass={chipCssClass}
            chipPrefixText={chipPrefixText}
            chipHrefFor={chipHrefFor}
            normalizeAddedValue={normalizeAddedValue}
            loginButtonText={loginButtonText}
            showAdd={showAdd}
            moderationRoles={moderationRoles}
            {...rest} />
    );
}
