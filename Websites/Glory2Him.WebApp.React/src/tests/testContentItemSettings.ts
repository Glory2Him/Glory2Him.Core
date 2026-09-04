import { ContentType } from '../models/foundations/contentItemSettings/contentType';

import {
    ContentItemSetting
} from '../models/foundations/contentItemSettings/contentItemSetting';

// A CONTENT TYPE'S SETTING, for tests that need the editor to have something to shape itself
// from. Which fields exist at all is the setting's call (§6.4), so a page test mocking the
// settings read as an empty list opens a form with nothing in it and proves nothing — the
// failure looks like a broken editor rather than a missing fixture.
//
// Everything is permissive by default so a test opts OUT of what it wants to prove absent
// (hasTitle: false for a quote) rather than opting in to the eight flags it does not care about.
export const testContentItemSetting = (
    contentType: ContentType,
    contentTypeName: string,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
        id: `setting-${contentType}`,
        contentType,
        contentItemId: null,
        contentTypeName,
        contentTypeDescription: `A ${contentTypeName.toLowerCase()}`,
        contentTypeIconCssClass: 'bi-chat-quote',
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
