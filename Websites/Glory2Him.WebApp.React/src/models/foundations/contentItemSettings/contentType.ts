// Mirrors Glory2Him.Core.Models.Enums.ContentType, including its numbering: the members are
// append-only and never renumbered on the server (a content item's type composes
// content-type-scoped role names), so these values are a contract rather than a convenience.
//
// Two representations are in play and both are needed. The wire carries the NUMBER — the host
// registers no JsonStringEnumConverter — while OData's $filter parses the member NAME
// ("contentType eq 'Quote'"). TypeScript's reverse mapping (ContentType[value]) supplies the
// second from the first, so only the numbering is written out here.
export enum ContentType {
    Quote = 0,
    Story = 1,
    Testimony = 2,
    Devotional = 3,
    BibleStudy = 4,
    BlogPost = 5,
    Verses = 6,
    Series = 100,
    Topic = 200,
}

// Declaration order, for the admin filter. Object.keys over a numeric enum yields the reverse
// mapping alongside the members, so the list is stated rather than derived.
export const contentTypeMembers: ReadonlyArray<ContentType> = [
    ContentType.Quote,
    ContentType.Story,
    ContentType.Testimony,
    ContentType.Devotional,
    ContentType.BibleStudy,
    ContentType.BlogPost,
    ContentType.Verses,
    ContentType.Series,
    ContentType.Topic,
];

// What an administrator reads in a filter dropdown. A SETTING's own ContentTypeName is
// editable per row and is what visitors see; this is the fixed member name, spaced out.
export const contentTypeLabels: Readonly<Record<ContentType, string>> = {
    [ContentType.Quote]: 'Quote',
    [ContentType.Story]: 'Story',
    [ContentType.Testimony]: 'Testimony',
    [ContentType.Devotional]: 'Devotional',
    [ContentType.BibleStudy]: 'Bible Study',
    [ContentType.BlogPost]: 'Blog Post',
    [ContentType.Verses]: 'Verses',
    [ContentType.Series]: 'Series',
    [ContentType.Topic]: 'Topic',
};
