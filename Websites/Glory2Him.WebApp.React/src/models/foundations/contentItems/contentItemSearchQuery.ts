import { ContentItem } from './contentItem';
import { ContentType } from '../contentItemSettings/contentType';

// What a page of the content-item list asks for. The sibling of ContentItemSettingQuery, and
// paged the same way for the same reason.
//
// api/ContentItems is an ordinary MVC route carrying [EnableQuery], not an OData route, so there
// is no @odata.count and $count adds no total. A page therefore asks for ONE ROW BEYOND the page
// and drops it: the extra row is the only thing that separates a full last page from a page with
// more behind it.
export type ContentItemSearchQuery = {
    // Free text, matched server-side against the title, the content and the author.
    searchTerm: string;

    // Null is "any category".
    contentType: ContentType | null;

    // The author of the WORDS, matched as a substring — a surname or a first name has to be
    // enough to find someone.
    author: string;

    // Zero-based, because it is the react-query page param and arithmetic on it reads better
    // from zero. ContentItemSettingQuery counts from one because an admin table shows the number.
    pageIndex: number;

    // The host caps [EnableQuery] reads at OData:PageSize (50), and the +1 probe row rides inside
    // that cap, so anything approaching it would silently lose the probe and never page again.
    pageSize: number;
};

export type ContentItemPage = {
    items: ContentItem[];
    pageIndex: number;
    pageSize: number;
    hasNextPage: boolean;
};
