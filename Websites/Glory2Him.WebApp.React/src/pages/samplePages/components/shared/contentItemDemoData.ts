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

// LONG ON PURPOSE: the truncation demos need a body that actually overruns the default
// 400-character cut, so the read-more affordances have something honest to do.
export const demoStoryItem: ContentItemSearchItem = {
    id: 'demo-story',
    contentType: ContentType.Story,
    contentItemSetting: demoSettings[0],
    title: 'NASA Proves The Bible Is True',
    author: 'Harold Hill',
    content:
        'For all the scientists out there, and for all the students who have a hard time '
        + 'convincing people of the truth of the Bible — here is something that shows '
        + "God's awesome creation, and that He is still in control.\n\n"
        + 'Did you know that the space program is busy proving that what has been called '
        + '"myth" in the Bible is true? Mr. Harold Hill, President of the Curtis Engine '
        + 'Company in Baltimore, Maryland, and a consultant in the space program, relates '
        + 'the following development.\n\n'
        + 'Our astronauts and space scientists at Green Belt, Maryland were checking the '
        + 'position of the sun, moon, and planets out in space — where they would be 100 '
        + 'years and 1,000 years from now. Orbits must be laid out in terms of the life of '
        + 'the satellite, so the whole thing does not bog down.\n\n'
        + 'They ran the computer measurement back and forth over the centuries and it came '
        + 'to a halt. The computer stopped and put up a red signal: something was wrong '
        + 'with either the information fed into it, or the results as compared to the '
        + 'standards. The service department found there is a day missing in space in '
        + 'elapsed time. There was no answer.\n\n'
        + 'Finally, a Christian man on the team remembered Sunday School and the sun '
        + 'standing still. In the book of Joshua they found the Lord saying, "Fear them '
        + 'not, I have delivered them into thy hand." Joshua, surrounded by the enemy and '
        + 'fearing darkness, asked the Lord to make the sun stand still.\n\n'
        + '"The sun stood still and the moon stayed — and hasted not to go down about a '
        + 'whole day." - Joshua 10:13\n\n'
        + 'There was the missing day! They checked the computers back to the time it was '
        + 'written — close, but not close enough. The elapsed time missing in Joshua\'s day '
        + 'was 23 hours and 20 minutes, not a whole day. The Bible says "about '
        + '(approximately) a day" — but 40 minutes still had to be found, because it '
        + 'multiplies many times over in orbits.\n\n'
        + 'The Christian employee remembered the sun going backwards. In 2 Kings, '
        + 'Hezekiah, on his death-bed, asked for a sign. Isaiah said, "Do you want the sun '
        + 'to go ahead 10 degrees?" Hezekiah answered, "Let the shadow return backward 10 '
        + 'degrees." And the Lord brought the shadow 10 degrees backward!\n\n'
        + 'Ten degrees is exactly 40 minutes. Twenty-three hours and 20 minutes in Joshua, '
        + 'plus 40 minutes in Second Kings, make the missing day in the universe!',
    imageUrl: '/assets/images/blog/4by3/01.jpg',
    shareabilityBasis: 3,
    sharePermission: '',
    approvalStatus: ApprovalStatus.Draft,
    submittedById: 'demo-user',
    submittedByName: 'Grace Abara',
    publishedDate: new Date(2026, 6, 15),
    tags: ['creation', 'science', 'faith'],
    bibleReferences: ['Joshua 10:12-13', '2 Kings 20:9-11'],
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
