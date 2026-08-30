import {
    ContentItem,
    ContentItemAddRequest
} from '../../../models/foundations/contentItems/contentItem';

import { ContentItemFormItem } from '../../../models/components/contentItems/contentItemFormItem';

// The two projections between the wire entity and the shape ContentItemDetailPanel renders. Shared
// rather than repeated per page: the contribute page and the post page both cross this boundary,
// and a projection that drifted between them would show the same item differently on each.

// Wire → panel. Only what the panel renders or gates on travels: the control fields (ContentHash,
// GroupId, Version, IsPublished, the bypass pair) are the workflow's, not the reader's, and
// carrying them would invite a consumer to send them back.
export const toContentItemFormItem = (contentItem: ContentItem): ContentItemFormItem => ({
    id: contentItem.id,
    contentType: contentItem.contentType,
    title: contentItem.title ?? '',
    author: contentItem.author ?? '',
    content: contentItem.content,
    shareabilityBasis: contentItem.shareabilityBasis,
    sharePermission: contentItem.sharePermission ?? '',
    createdBy: contentItem.createdBy,
    approvalStatus: contentItem.approvalStatus,
    isDeleted: contentItem.isDeleted
});

// Panel → wire, for the add. An empty optional field goes as null rather than as "": the
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
