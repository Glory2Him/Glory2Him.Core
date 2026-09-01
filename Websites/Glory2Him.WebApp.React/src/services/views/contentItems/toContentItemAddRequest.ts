import { ContentItemAddRequest } from '../../../models/foundations/contentItems/contentItem';
import { ContentItemFormItem } from '../../../models/components/contentItems/contentItemFormItem';

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
    sharePermission: asOptionalText(formItem.sharePermission)
});

const asOptionalText = (value: string | undefined): string | null => {
    const trimmed = (value ?? '').trim();

    return trimmed.length === 0 ? null : trimmed;
};
