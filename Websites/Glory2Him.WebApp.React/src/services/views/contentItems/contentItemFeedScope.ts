import {
    ContentItemSearchCriteria
} from '../../../models/components/contentItems/contentItemSearchItem';

// WHICH READ A JOURNAL SURFACE ASKS, given what the reader has narrowed.
//
// The feed (§DOM11.3) is the design's front-page listing: the canonical visible set in
// effective publication order, with Topic and Series excluded (§DOM3.8 rule 2). It takes no
// $filter, so it cannot serve a narrowed search — and giving it one would be the wrong fix
// anyway, because applying the feed's exclusion to a narrowing read would make every topic
// unsearchable. So the moment the reader narrows anything the page moves to the public read,
// which is caller-independent in exactly the same way and does carry a $filter.
//
// WHAT COUNTS AS NARROWING is the six criteria that put a $filter on the wire, and only those:
// free text, content type, author, submitted-by id, shareability basis, and the statuses the
// READER chose.
//
// The surface's DEFAULT statuses are the page's choice rather than the reader's, so they are
// deliberately not consulted here — they narrow nothing the feed does not already exclude, a
// Rejected row being outside §14.1 anyway.
//
// Tags and Bible references are deliberately not criteria either: nothing narrows on them
// until #318, so treating them as narrowing would cost the feed's order and re-admit topics in
// exchange for no narrowing at all.
export const resolveContentItemFeedScope = (
    criteria: ContentItemSearchCriteria): 'feed' | 'public' =>
    hasNarrowingContentItemSearchCriteria(criteria) ? 'public' : 'feed';

const hasNarrowingContentItemSearchCriteria = (
    criteria: ContentItemSearchCriteria): boolean =>
    criteria.query.trim().length > 0
    || criteria.contentType != null
    || criteria.author.trim().length > 0
    || (criteria.submittedBy?.id ?? '').trim().length > 0
    || criteria.shareabilityBasis != null
    || criteria.approvalStatuses.length > 0;
