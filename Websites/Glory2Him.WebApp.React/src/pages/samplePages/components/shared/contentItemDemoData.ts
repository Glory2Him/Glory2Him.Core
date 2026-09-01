import { ContentItemSetting } from '../../../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../../../models/foundations/contentItemSettings/contentType';

import {
    ApprovalStatus,
    ContentItemReactionOption,
    ContentItemSearchItem
} from '../../../../models/components/contentItems/contentItemSearchItem';

// ONE demo dataset for the whole ContentItemPanel family's reference pages, so every page in
// the tree demonstrates against the same rows and a reader walking the family sees the same
// items wearing different faces — rather than nine pages inventing nine worlds.

export const demoSettingFor = (
    contentType: ContentType,
    contentTypeName: string,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
    id: `demo-setting-${contentTypeName.toLowerCase().replace(/\s+/g, '-')}`,
    contentItemId: null,
    contentType,
    contentTypeName,
    contentTypeDescription: `A ${contentTypeName.toLowerCase()}.`,
    contentTypeIconCssClass: 'bi-journal-text',
    sortOrder: contentType,
    isAvailableAsGeneralUserContribution: true,
    hasTitle: true,
    hasAuthor: true,
    tagsAllowed: true,
    showTags: true,
    commentsAllowed: true,
    showComments: true,
    reactionsAllowed: true,
    showReactions: true,
    limitReactionsToLoveOnly: false,
    linksAllowed: false,
    showLinks: false,
    attachmentsAllowed: false,
    showAttachments: false,
    bibleReferenceAllowed: true,
    showBibleReferences: true,
    isDeleted: false,
    createdBy: 'seed',
    createdWhen: '2026-01-01T00:00:00+00:00',
    updatedBy: 'seed',
    updatedWhen: '2026-01-01T00:00:00+00:00',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null,
    ...overrides
});

export const demoSettings: ReadonlyArray<ContentItemSetting> = [
    demoSettingFor(ContentType.Story, 'Story', { contentTypeIconCssClass: 'bi-book' }),
    demoSettingFor(ContentType.Quote, 'Quote', {
        contentTypeIconCssClass: 'bi-quote', hasTitle: false
    }),
    demoSettingFor(ContentType.Devotional, 'Devotional', {
        contentTypeIconCssClass: 'bi-sunrise'
    }),
    demoSettingFor(ContentType.Verses, 'Verse Image', {
        contentTypeIconCssClass: 'bi-card-image', hasTitle: false
    })
];

export const demoStoryItem: ContentItemSearchItem = {
    id: 'demo-story',
    contentType: ContentType.Story,
    contentItemSetting: demoSettings[0],
    title: 'He carried me through',
    author: 'Grace Abara',
    content:
        'When the diagnosis came, I could not pray. But every morning there was bread '
        + 'on the table and a verse in my inbox, and looking back I can see that He was '
        + 'carrying me the whole way through it.',
    imageUrl: '/assets/images/blog/4by3/01.jpg',
    shareabilityBasis: 3,
    sharePermission: '',
    approvalStatus: ApprovalStatus.Draft,
    submittedById: 'demo-user',
    submittedByName: 'Grace Abara',
    publishedDate: new Date(2026, 6, 15),
    tags: ['providence', 'healing'],
    bibleReferences: ['Deuteronomy 31:6'],
    reactionSummary: [
        { label: 'Amen', glyph: '🙏', count: 12 },
        { label: 'Love', glyph: '❤️', count: 5 }
    ],
    commentCount: 4
};

export const demoQuoteItem: ContentItemSearchItem = {
    id: 'demo-quote',
    contentType: ContentType.Quote,
    contentItemSetting: demoSettings[1],
    author: 'Dwight L. Moody',
    content: 'Character is what you are in the dark.',
    shareabilityBasis: 2,
    approvalStatus: ApprovalStatus.Approved,
    submittedById: 'demo-user',
    submittedByName: 'Grace Abara',
    publishedDate: new Date(2026, 5, 2)
};

export const demoVersesItem: ContentItemSearchItem = {
    id: 'demo-verses',
    contentType: ContentType.Verses,
    contentItemSetting: demoSettings[3],
    author: 'The Bible',
    content:
        'For God so loved the world, that he gave his only begotten Son, that whosoever '
        + 'believeth in him should not perish, but have everlasting life. — John 3:16',
    shareabilityBasis: 2,
    approvalStatus: ApprovalStatus.Approved,
    submittedById: 'demo-user',
    publishedDate: new Date(2026, 4, 20),
    bibleReferences: ['John 3:16']
};

export const demoItems: ReadonlyArray<ContentItemSearchItem> =
    [demoStoryItem, demoQuoteItem, demoVersesItem];

export const demoReactionOptions: ReadonlyArray<ContentItemReactionOption> = [
    { label: 'Amen', glyph: '🙏' },
    { label: 'Love', glyph: '❤️', isLove: true },
    { label: 'Joy', glyph: '😊' }
];
