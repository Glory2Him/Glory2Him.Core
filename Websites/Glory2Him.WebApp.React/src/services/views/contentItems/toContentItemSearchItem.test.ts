import { describe, expect, it } from 'vitest';
import { ContentItem } from '../../../models/foundations/contentItems/contentItem';
import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';
import { ApprovalStatus } from '../../../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../../../models/components/contentItems/contentItemFormItem';

import {
    placeholderImageUrlFor,
    toContentItemSearchItem,
    toExcerpt
} from './toContentItemSearchItem';

const contentItemFor = (overrides: Partial<ContentItem> = {}): ContentItem => ({
    id: 'devotional-1',
    contentType: ContentType.Devotional,
    title: 'Grace for the ordinary Tuesday',
    author: 'Miriam Vale',
    content: 'Grace is not a one-time event but the daily air the believer breathes.',
    shareabilityBasis: ShareabilityBasis.Owned,
    sharePermission: null,
    contentHash: 'hash-1',
    groupId: 'group-1',
    version: 1,
    publishDate: '2026-07-03T00:00:00Z',
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy: 'account-1',
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: 'account-1',
    updatedWhen: '2026-07-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null,
    ...overrides
});

describe('toExcerpt', () => {
    it('should leave a short body exactly as it stands', () => {
        // when
        const excerpt = toExcerpt('Grace is the daily air the believer breathes.');

        // then: no ellipsis on something that was never cut
        expect(excerpt).toBe('Grace is the daily air the believer breathes.');
    });

    // Contributed text is typed into a textarea, so its paragraph breaks are newlines. A row is
    // one line of prose, and the panel's clamp cannot collapse what the markup still carries.
    it('should collapse the paragraph breaks a textarea left behind', () => {
        // when
        const excerpt = toExcerpt('  First line.\n\n   Second line.  ');

        // then
        expect(excerpt).toBe('First line. Second line.');
    });

    it('should cut a long body on a word boundary rather than mid-word', () => {
        // given
        const body = `${'word '.repeat(80)}end`;

        // when
        const excerpt = toExcerpt(body);

        // then
        expect(excerpt.endsWith('…')).toBe(true);
        expect(excerpt).not.toContain('wor…');
        expect(excerpt.length).toBeLessThanOrEqual(221);
    });
});

describe('toContentItemSearchItem', () => {
    it('should carry the members a card renders', () => {
        // when
        const item = toContentItemSearchItem(contentItemFor());

        // then
        expect(item.id).toBe('devotional-1');
        expect(item.contentType).toBe(ContentType.Devotional);
        expect(item.title).toBe('Grace for the ordinary Tuesday');
        expect(item.author).toBe('Miriam Vale');
        expect(item.approvalStatus).toBe(ApprovalStatus.Approved);
    });

    // The wire carries null where a type has no title or author; the projection carries
    // undefined, which is what "absent" means to every optional member of the card.
    it('should turn an absent title or author into absence rather than into null', () => {
        // when
        const item = toContentItemSearchItem(
            contentItemFor({ title: null, author: null }));

        // then
        expect(item.title).toBeUndefined();
        expect(item.author).toBeUndefined();
    });

    it('should date a published row by when it was published', () => {
        // when
        const item = toContentItemSearchItem(contentItemFor());

        // then
        expect(item.publishedDate?.toISOString()).toBe('2026-07-03T00:00:00.000Z');
    });

    // A draft has no publish date, and a card with no date at all reads as broken.
    it('should date a draft by when it was written', () => {
        // when
        const item = toContentItemSearchItem(
            contentItemFor({ publishDate: null, approvalStatus: ApprovalStatus.Draft }));

        // then
        expect(item.publishedDate?.toISOString()).toBe('2026-07-01T00:00:00.000Z');
    });

    // ContentItem carries no image column and Attachment has no exposer, so the picture is a
    // placeholder chosen by TYPE — the same type always looks the same, and a card never changes
    // picture between renders.
    it('should give every content type a stable placeholder of its own', () => {
        // given
        const contentTypes = [
            ContentType.Quote,
            ContentType.Story,
            ContentType.Testimony,
            ContentType.Devotional,
            ContentType.BibleStudy,
            ContentType.BlogPost,
            ContentType.Series,
            ContentType.Topic
        ];

        // when
        const imageUrls = contentTypes.map(placeholderImageUrlFor);

        // then
        expect(imageUrls.every((imageUrl) => (imageUrl ?? '').length > 0)).toBe(true);
        expect(new Set(imageUrls).size).toBe(contentTypes.length);

        expect(placeholderImageUrlFor(ContentType.Devotional))
            .toBe(placeholderImageUrlFor(ContentType.Devotional));
    });

    // Each of these is blocked on an association read the host does not expose yet (#318), and
    // the templates leave an absent figure out rather than rendering a zero.
    it('should claim nothing the api cannot answer', () => {
        // when
        const item = toContentItemSearchItem(contentItemFor());

        // then
        expect(item.submittedByName).toBeUndefined();
        expect(item.tags).toBeUndefined();
        expect(item.bibleReferences).toBeUndefined();
        expect(item.reactionSummary).toBeUndefined();
        expect(item.commentCount).toBeUndefined();
    });

    it('should carry the shareability basis for the meta row', () => {
        // when
        const item = toContentItemSearchItem(contentItemFor());

        // then
        expect(item.shareabilityBasis).toBe(ShareabilityBasis.Owned);
    });

    // CreatedBy is an ACCOUNT ID: it travels as the FILTER half of "Submitted by" — the read
    // matches createdBy exactly on it — while the NAME half stays unset until a resolver exists,
    // and the card renders no segment without a name, so the id itself never shows.
    it('should carry the account id for filtering and no name to render it as', () => {
        // when
        const item = toContentItemSearchItem(contentItemFor({ createdBy: 'account-secret' }));

        // then
        expect(item.submittedById).toBe('account-secret');
        expect(item.submittedByName).toBeUndefined();
    });
});
