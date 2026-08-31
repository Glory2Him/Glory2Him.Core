import { ContentItem } from '../../../models/foundations/contentItems/contentItem';
import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';

import {
    ContentItemSearchItem
} from '../../../models/components/contentItems/contentItemSearchItem';

// The projection between the wire entity and the shape ContentItemSearchPanel renders — the
// sibling of toContentItemFormItem, which does the same for the detail panel.

// How much of a body a row shows before the panel's own clamp takes over. Cut on a word boundary
// so an excerpt never ends mid-word, and only when there is enough over the limit to be worth
// cutting — trimming four characters off a paragraph buys nothing and costs an ellipsis.
const excerptLength = 220;

export const toExcerpt = (content: string): string => {
    const normalized = content.replace(/\s+/g, ' ').trim();

    if (normalized.length <= excerptLength) {
        return normalized;
    }

    const cut = normalized.slice(0, excerptLength);
    const lastSpace = cut.lastIndexOf(' ');

    return `${(lastSpace > 0 ? cut.slice(0, lastSpace) : cut).trimEnd()}…`;
};

// THE PLACEHOLDER, and it is a placeholder rather than a picture of anything. ContentItem carries
// no image column and Attachment has no HTTP exposer, so there is nothing to fetch — these are
// the theme's own stock images, chosen by CONTENT TYPE so the same type always looks the same and
// a card never changes picture between renders.
//
// When real header images land (§4.9), this is the one function that changes: the panel takes
// whatever imageUrl it is handed and knows nothing about where it came from.
// QUOTE IS DELIBERATELY ABSENT. Its template stands the quote large over the image when one
// exists, and a stock photo underneath somebody's words reads as a claim about them — so a quote
// ships on the quiet light block until real header images land, and the hero face waits ready.
const placeholderImageUrls: Partial<Readonly<Record<ContentType, string>>> = {
    [ContentType.Story]: '/assets/images/blog/4by3/01.jpg',
    [ContentType.Testimony]: '/assets/images/blog/4by3/02.jpg',
    [ContentType.Devotional]: '/assets/images/blog/4by3/03.jpg',
    [ContentType.BibleStudy]: '/assets/images/blog/4by3/04.jpg',
    [ContentType.BlogPost]: '/assets/images/blog/4by3/05.jpg',
    [ContentType.Series]: '/assets/images/blog/4by3/06.jpg',
    [ContentType.Topic]: '/assets/images/blog/4by3/07.jpg'
};

export const placeholderImageUrlFor = (contentType: ContentType): string | undefined =>
    placeholderImageUrls[contentType];

export const toContentItemSearchItem = (contentItem: ContentItem): ContentItemSearchItem => ({
    id: contentItem.id,
    contentType: contentItem.contentType,
    title: contentItem.title ?? undefined,
    author: contentItem.author ?? undefined,
    content: contentItem.content,
    excerpt: toExcerpt(contentItem.content),
    imageUrl: placeholderImageUrlFor(contentItem.contentType),
    shareabilityBasis: contentItem.shareabilityBasis,

    // PublishDate where the row has one, CreatedWhen otherwise. A draft has no publish date and
    // a card with no date at all reads as broken, so the honest fallback is when it was written.
    publishedDate: new Date(contentItem.publishDate ?? contentItem.createdWhen),

    // The ID alone — the filter half of "Submitted by". onSubmittedByClick needs something the
    // read can match on, and CreatedBy is exactly what the rows carry; the NAME stays unset (see
    // below), and the card renders no segment without one, so the id itself never shows.
    submittedById: contentItem.createdBy,

    // The status the page hands the panel, so a row that is not yet public wears a badge. Set
    // unconditionally: the panel says nothing about an Approved one, which is the ordinary case.
    approvalStatus: contentItem.approvalStatus

    // DELIBERATELY UNSET, all for the same reason — the API cannot answer them yet:
    //
    //   submittedByName  CreatedBy is an ACCOUNT ID, and the only display-name resolver in the
    //                    host is [Authorize] and gated on the reviewer tier (§16.7.4). Rendering
    //                    the id would leak it; inventing a name would be worse.
    //   tags,
    //   bibleReferences  Associations have no HTTP exposer (#318).
    //   reactionSummary,
    //   commentCount     Neither Reaction nor Comment carries a ContentItemId — both are linked
    //                    by an Association, so these are blocked on #318 too.
    //
    // Each one is optional on the projection and the templates LEAVE IT OUT rather than
    // rendering a zero, so a card claims no figure it does not have.
});
