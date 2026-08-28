import { ContentType } from "./contentType";
import { ContentItemSetting } from "./contentItemSetting";

// A row is either the DEFAULT for its content type (ContentItemId is null) or an OVERRIDE for
// one content item. Design §6.10 resolves an effective setting by merging the two, and the
// unique indexes allow one of each, so the distinction is the list's primary axis.
export type ContentItemSettingScope = 'All' | 'Default' | 'Override';

export type ContentItemSettingQuery = {
    searchTerm?: string;
    contentType?: ContentType;
    scope?: ContentItemSettingScope;
    page: number;
    pageSize: number;
};

// The collection read answers with a plain JSON array — it goes through an ordinary attribute
// route rather than an OData route, so there is no @odata.count and $count adds no total. A
// page therefore knows whether another one follows, but not how many there are.
export type ContentItemSettingPage = {
    items: ContentItemSetting[];
    page: number;
    pageSize: number;
    hasNextPage: boolean;
};
