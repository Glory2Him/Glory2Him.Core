import { ContentItemAddRequest } from '../../../models/foundations/contentItems/contentItem';
import {
    ContentItemFormItem,
    defaultContributorApprovalStatus
} from '../../../models/components/contentItems/contentItemFormItem';

// Panel → wire, for the add. The wire→panel direction lives in toContentItemSearchItem now:
// since the merge there is ONE projection for the whole family, and ContentItemPanel derives
// its editor seed from that same element.
// An empty optional field goes as null rather than as "": the
// foundation's length rules run on the string it is given, and a blank Title is absent rather
// than a title of no characters.
export const toContentItemAddRequest = (
    formItem: ContentItemFormItem
): ContentItemAddRequest => ({
    contentType: formItem.contentType,
    title: asOptionalText(formItem.title),
    author: asOptionalText(formItem.author),
    content: formItem.content,
    shareabilityBasis: formItem.shareabilityBasis,
    sharePermission: asOptionalText(formItem.sharePermission),

    // The status the contributor filed under, not a status this projection decides. A form
    // that never rendered the "Submit as" row leaves it unset, and the offerable default —
    // Submitted, what the contribution page is for — is what travels then.
    approvalStatus: formItem.approvalStatus ?? defaultContributorApprovalStatus
});

const asOptionalText = (value: string | undefined): string | null => {
    const trimmed = (value ?? '').trim();

    return trimmed.length === 0 ? null : trimmed;
};
