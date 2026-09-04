import { ContentItem } from '../../../models/foundations/contentItems/contentItem';

import {
    ContentItemFormItem
} from '../../../models/components/contentItems/contentItemFormItem';

// THE STORED ROW, SEEDED INTO THE EDITOR. The form item is the panel family's own shape, so a
// page that has fetched a ContentItem hands it through here rather than teaching the editor the
// wire model.
//
// `id` is deliberately absent from the form item: it is the row's identity, not a field anybody
// edits, and the page holds it already. What IS carried beyond the editable text is the trio the
// panel's gates read — createdBy for the owner rule, approvalStatus for the edit gate and the
// Submit-as row, and createdWhen for the byline.
export const toContentItemFormItem = (contentItem: ContentItem): ContentItemFormItem => ({
    contentType: contentItem.contentType,
    title: contentItem.title ?? undefined,
    author: contentItem.author ?? undefined,
    content: contentItem.content,
    shareabilityBasis: contentItem.shareabilityBasis,
    sharePermission: contentItem.sharePermission ?? undefined,
    createdBy: contentItem.createdBy,
    createdWhen: contentItem.createdWhen,
    approvalStatus: contentItem.approvalStatus,
    isDeleted: contentItem.isDeleted
});

// THE EDIT, LAID BACK OVER THE STORED ROW. PUT api/ContentItems takes the whole entity and the
// server maps the permitted fields off it (§9.7.1 rule 2), pinning everything else against
// storage — so what goes back is the row as fetched with the caller's edits over the top, never
// a fresh object. Sending a partial would put a default into every field the form does not
// carry, and `default` is a legal value for most of them.
//
// ApprovalStatus rides along because the Draft ↔ Submitted carve-out is a legitimate part of a
// modify (§9.2 rules 3-6) — the foundation validates the pair and refuses anything else.
export const toContentItemModifyRequest = (
    contentItem: ContentItem,
    formItem: ContentItemFormItem): ContentItem => ({
        ...contentItem,
        title: asOptionalText(formItem.title),
        author: asOptionalText(formItem.author),
        content: formItem.content,
        shareabilityBasis: formItem.shareabilityBasis,
        sharePermission: asOptionalText(formItem.sharePermission),
        approvalStatus: formItem.approvalStatus ?? contentItem.approvalStatus
    });

const asOptionalText = (value: string | undefined): string | null => {
    const trimmed = (value ?? '').trim();

    return trimmed.length === 0 ? null : trimmed;
};
