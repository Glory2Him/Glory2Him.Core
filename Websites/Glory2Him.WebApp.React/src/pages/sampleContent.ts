import { CommentEntry } from '../models/coreUI/commentEntry';
import { ReactionOption } from '../models/coreUI/reactionOption';

// The content shown on the Home and Post Single pages, transcribed from the Blazor
// SampleContent so the two stacks can be compared against each other side by side.
// The copy still comes from this module — real posts have not been wired in yet.

export interface SamplePost {
    title: string;
    slug: string;
    excerpt: string;
    category: string;
    categoryBadgeCss: string;
    authorName: string;
    authorRole: string;
    authorImageUrl: string;
    imageUrl: string;
    publishedDate: Date;
    readMinutes: number;
    reactions: number;
    comments: number;
    views: number;
    tags: ReadonlyArray<string>;
    bibleReferences: ReadonlyArray<string>;
    isFeatured: boolean;
}

export const verseOfTheDay =
    '"For by grace you have been saved through faith..." — Ephesians 2:8 NIV';

// The lead story, shown as the featured card on Home and in full on Post Single.
export const featured: SamplePost = {
    title: 'NASA Proves The Bible Is True',
    slug: 'nasa-proves-the-bible-is-true',
    excerpt: "The story of the missing day in space — Joshua's long day and "
        + "Hezekiah's shadow that went backward.",
    category: 'Testimony',
    categoryBadgeCss: 'text-bg-warning',
    authorName: 'Louis',
    authorRole: 'An editor at Glory 2 Him',
    authorImageUrl: '/assets/images/avatar/01.jpg',
    imageUrl: '/assets/images/blog/16by9/big/01.jpg',
    publishedDate: new Date(2026, 6, 15),
    readMinutes: 5,
    reactions: 266,
    comments: 18,
    views: 2344,
    tags: ['creation', 'science', 'faith', 'miracles'],
    bibleReferences: ['Joshua 10:8, 12–13', '2 Kings 20:9–11'],
    isFeatured: true,
};

// The three tiles filling the right half of the hero grid, in the mockup's order.
export const heroTiles: ReadonlyArray<SamplePost> = [
    {
        title: "Justification means there isn't a charge against you — D.L. Moody",
        slug: 'justification-no-charge-against-you',
        excerpt: '',
        category: 'Quotes',
        categoryBadgeCss: 'text-bg-success',
        authorName: 'Bryan',
        authorRole: 'Contributor',
        authorImageUrl: '/assets/images/avatar/02.jpg',
        imageUrl: '/assets/images/blog/4by3/01.jpg',
        publishedDate: new Date(2026, 6, 18),
        readMinutes: 2,
        reactions: 142,
        comments: 9,
        views: 980,
        tags: ['justified', 'redemption', 'grace'],
        bibleReferences: ['Romans 3:23–24'],
        isFeatured: false,
    },
    {
        title: 'Walking daily in grace',
        slug: 'walking-daily-in-grace',
        excerpt: '',
        category: 'Devotional',
        categoryBadgeCss: 'text-bg-danger',
        authorName: 'Joan',
        authorRole: 'Contributor',
        authorImageUrl: '/assets/images/avatar/03.jpg',
        imageUrl: '/assets/images/blog/4by3/03.jpg',
        publishedDate: new Date(2026, 6, 3),
        readMinutes: 3,
        reactions: 87,
        comments: 5,
        views: 1120,
        tags: ['grace', 'discipleship'],
        bibleReferences: ['Ephesians 2:8–9'],
        isFeatured: false,
    },
    {
        title: 'The armor of God, piece by piece',
        slug: 'the-armor-of-god-piece-by-piece',
        excerpt: '',
        category: 'Bible Study',
        categoryBadgeCss: 'text-bg-info',
        authorName: 'Amanda',
        authorRole: 'Contributor',
        authorImageUrl: '/assets/images/avatar/04.jpg',
        imageUrl: '/assets/images/blog/4by3/04.jpg',
        publishedDate: new Date(2026, 5, 28),
        readMinutes: 6,
        reactions: 54,
        comments: 12,
        views: 1340,
        tags: ['prayer', 'spiritual-warfare'],
        bibleReferences: ['Ephesians 6:10–18'],
        isFeatured: false,
    },
];

// The "Latest posts" grid — four cards, each carrying its own excerpt, tags and references.
export const latest: ReadonlyArray<SamplePost> = [
    {
        title: 'Justification means there isn’t a charge against you',
        slug: 'justification-no-charge-against-you',
        excerpt: 'Your sins are completely wiped out; God says He puts them out of '
            + 'His memory. — Dwight L. Moody',
        category: 'Quotes',
        categoryBadgeCss: 'text-bg-success',
        authorName: 'Bryan',
        authorRole: 'Contributor',
        authorImageUrl: '/assets/images/avatar/02.jpg',
        imageUrl: '/assets/images/blog/4by3/01.jpg',
        publishedDate: new Date(2026, 6, 18),
        readMinutes: 2,
        reactions: 142,
        comments: 9,
        views: 980,
        tags: ['justified', 'redemption', 'grace'],
        bibleReferences: ['Romans 3:23–24'],
        isFeatured: false,
    },
    {
        title: 'NASA Proves The Bible Is True',
        slug: 'nasa-proves-the-bible-is-true',
        excerpt: 'The story of the missing day in space — and the forty minutes found '
            + 'in Hezekiah’s backward shadow.',
        category: 'Testimony',
        categoryBadgeCss: 'text-bg-warning',
        authorName: 'Louis',
        authorRole: 'An editor at Glory 2 Him',
        authorImageUrl: '/assets/images/avatar/01.jpg',
        imageUrl: '/assets/images/blog/4by3/02.jpg',
        publishedDate: new Date(2026, 6, 15),
        readMinutes: 5,
        reactions: 266,
        comments: 18,
        views: 2344,
        tags: ['creation', 'science', 'faith'],
        bibleReferences: ['Joshua 10:12–13', '2 Kings 20:9–11'],
        isFeatured: false,
    },
    {
        title: 'Walking daily in grace',
        slug: 'walking-daily-in-grace',
        excerpt: 'Grace is not a one-time event but the daily air the believer breathes.',
        category: 'Devotional',
        categoryBadgeCss: 'text-bg-danger',
        authorName: 'Joan',
        authorRole: 'Contributor',
        authorImageUrl: '/assets/images/avatar/03.jpg',
        imageUrl: '/assets/images/blog/4by3/03.jpg',
        publishedDate: new Date(2026, 6, 3),
        readMinutes: 3,
        reactions: 87,
        comments: 5,
        views: 1120,
        tags: ['grace', 'discipleship'],
        bibleReferences: ['Ephesians 2:8–9'],
        isFeatured: false,
    },
    {
        title: 'The armor of God, piece by piece',
        slug: 'the-armor-of-god-piece-by-piece',
        excerpt: 'A six-part walk through Paul’s picture of the believer’s equipment '
            + 'for the fight.',
        category: 'Bible Study',
        categoryBadgeCss: 'text-bg-info',
        authorName: 'Amanda',
        authorRole: 'Contributor',
        authorImageUrl: '/assets/images/avatar/04.jpg',
        imageUrl: '/assets/images/blog/4by3/04.jpg',
        publishedDate: new Date(2026, 5, 28),
        readMinutes: 6,
        reactions: 54,
        comments: 12,
        views: 1340,
        tags: ['prayer', 'spiritual-warfare'],
        bibleReferences: ['Ephesians 6:10–18'],
        isFeatured: false,
    },
];

// Post Single states its own figures for the lead story, and they differ from the numbers
// on the same story's Home card (257 vs 266 reactions, 4 vs 18 comments). Each page is
// kept faithful to its own mockup rather than one being quietly "corrected".
export const detailReactions = 257;
export const detailComments = 4;
export const detailViews = 2344;
export const detailAuthorName = 'Louis Ferguson';

export const reactions: ReadonlyArray<ReactionOption> = [
    { label: 'Amen', iconCssClass: 'fas fa-thumbs-up', color: '#4e5ff9', count: 112 },
    { label: 'Love', iconCssClass: 'fas fa-heart', color: '#d6293e', count: 98 },
    { label: 'Joy', iconCssClass: 'fas fa-smile', color: '#f7c32e', count: 41 },
    { label: 'Moved', iconCssClass: 'fas fa-sad-tear', color: '#17a2b8', count: 6 },
];

export const comments: ReadonlyArray<CommentEntry> = [
    {
        authorName: 'Allen Smith',
        authorImageUrl: '/assets/images/avatar/01.jpg',
        postedAt: new Date(2026, 6, 16, 6, 1),
        body: 'This blessed me so much. The little words in Scripture really do '
            + 'matter — "about a whole day"!',
        reactions: 14,
    },
    {
        authorName: 'Louis Ferguson',
        authorImageUrl: '/assets/images/avatar/02.jpg',
        postedAt: new Date(2026, 6, 16, 9, 24),
        body: 'Thank you Allen — that phrase is exactly what sent me looking into '
            + 'this in the first place.',
        reactions: 6,
        isReply: true,
    },
    {
        authorName: 'Marie Cooper',
        authorImageUrl: '/assets/images/avatar/03.jpg',
        postedAt: new Date(2026, 6, 17, 14, 12),
        body: "I had never connected Hezekiah's sundial to Joshua's long day. "
            + 'Reading both together is remarkable.',
        reactions: 21,
    },
    {
        authorName: 'Peter Nguyen',
        authorImageUrl: '/assets/images/avatar/04.jpg',
        postedAt: new Date(2026, 6, 18, 8, 40),
        body: 'Worth reading slowly. Sharing this with our small group this week.',
        reactions: 9,
    },
];

export const categories: ReadonlyArray<[label: string, buttonCssClass: string]> = [
    ['Testimony', 'btn-warning'],
    ['Quotes', 'btn-success'],
    ['Stories', 'btn-info'],
    ['Devotional', 'btn-danger'],
];

export const popularTags: ReadonlyArray<[label: string, buttonCssClass: string]> = [
    ['grace', 'btn-primary-soft'],
    ['faith', 'btn-warning-soft'],
    ['redemption', 'btn-success-soft'],
    ['prayer', 'btn-danger-soft'],
    ['justified', 'btn-info-soft'],
    ['creation', 'btn-primary-soft'],
];

export const popularReferences: ReadonlyArray<string> = [
    'Romans 3:23–24',
    'Joshua 10:12–13',
    '2 Kings 20:9–11',
    'Ephesians 2:8–9',
];
