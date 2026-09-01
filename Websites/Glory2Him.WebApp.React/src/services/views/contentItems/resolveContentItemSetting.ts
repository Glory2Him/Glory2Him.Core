import { ContentItemSetting } from '../../../models/foundations/contentItemSettings/contentItemSetting';

import {
    ContentType,
    contentTypeLabels
} from '../../../models/foundations/contentItemSettings/contentType';

// THE EFFECTIVE SETTING, resolved as §6.4 and §12.5.2 rules 1-2 require: an item-level override
// takes FULL precedence over the content type default, and the default applies when there is no
// override. A soft-deleted row is excluded from active policy resolution entirely (§6.6).
//
// The override is matched on the ITEM as well as the type — a caller handing over a mixed
// collection must not have one item's override applied to another's — which is also why a surface
// with no item yet can only ever resolve a default.
//
// SHARED rather than written twice. ContentItemFormPanel resolves this to shape its fields, and the
// page above it resolves the same row to name itself; when the page had its own copy it drifted
// immediately — it lost the soft-delete filter, could not see an override at all, and fell back
// to a literal instead of the type's name.
//
// Falling through to `undefined` is deliberate. When neither a matching override nor a default is
// present, the caller shapes itself from the item it holds rather than adopting a policy row that
// was written for somebody else.
export const resolveContentItemSetting = (
    contentItemSettingCollection: ReadonlyArray<ContentItemSetting>,
    contentType: ContentType | null,
    contentItemId?: string
): ContentItemSetting | undefined => {
    if (contentType == null) {
        return undefined;
    }

    const candidates = contentItemSettingCollection.filter(
        (setting) => setting.isDeleted !== true && setting.contentType === contentType);

    const override = contentItemId == null
        ? undefined
        : candidates.find((setting) => setting.contentItemId === contentItemId);

    return override ?? candidates.find((setting) => setting.contentItemId == null);
};

// What a visitor should read as the type's name. The SETTING's own ContentTypeName where one
// resolves — it is editable per row and is what visitors see — falling back to the fixed enum
// member label, which exists for every member and so is never empty. That matters: this is used
// for a page heading, and a heading must not be blank while the settings are still arriving.
export const contentTypeNameOf = (
    contentItemSettingCollection: ReadonlyArray<ContentItemSetting>,
    contentType: ContentType | null,
    contentItemId?: string
): string => {
    if (contentType == null) {
        return '';
    }

    const setting = resolveContentItemSetting(
        contentItemSettingCollection, contentType, contentItemId);

    return setting?.contentTypeName ?? contentTypeLabels[contentType] ?? '';
};
