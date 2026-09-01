import {
    ContentItemSettingQuery,
    ContentItemSettingScope
} from '../../../models/foundations/contentItemSettings/contentItemSettingQuery';

import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';

// The admin list's filters, round-tripped through the URL. The list holds them nowhere else,
// which is what lets a row's Manage hand the detail page the exact view it was opened from —
// and the way back (and the save) land on that view rather than on an unfiltered first page.
//
// The two rules the feed's criteria keep (contentItemSearchCriteriaUrl) hold here too. The
// ContentType travels by MEMBER NAME, so ?type=Testimony survives being read by a person and
// the numbering stays the wire contract it is. A value at its default is left out altogether,
// so an unfiltered list has a clean URL and a missing parameter reads back as the default it
// was. pageSize is the page's own constant rather than a filter, so it never travels.
const searchTermParameterName = 'q';
const contentTypeParameterName = 'type';
const scopeParameterName = 'scope';
const pageParameterName = 'page';

const defaultScope: ContentItemSettingScope = 'All';
const firstPage = 1;

const toContentType = (value: string | null): ContentType | undefined => {
    if (value == null || value.length === 0) {
        return undefined;
    }

    const member = ContentType[value as keyof typeof ContentType];

    return typeof member === 'number' ? member : undefined;
};

const toScope = (value: string | null): ContentItemSettingScope =>
    value === 'Default' || value === 'Override' ? value : defaultScope;

// Anything that is not a page number is the first page: a hand-edited ?page=0 should show the
// list rather than ask the server for a negative $skip.
const toPage = (value: string | null): number => {
    const page = Number.parseInt(value ?? '', 10);

    return Number.isNaN(page) || page < firstPage ? firstPage : page;
};

export const toContentItemSettingQuery = (
    searchParams: URLSearchParams,
    pageSize: number): ContentItemSettingQuery => ({
        searchTerm: searchParams.get(searchTermParameterName) ?? '',
        contentType: toContentType(searchParams.get(contentTypeParameterName)),
        scope: toScope(searchParams.get(scopeParameterName)),
        page: toPage(searchParams.get(pageParameterName)),
        pageSize
    });

export const toContentItemSettingSearchParams = (
    query: ContentItemSettingQuery): URLSearchParams => {
    const parameters = new URLSearchParams();
    const searchTerm = (query.searchTerm ?? '').trim();

    if (searchTerm.length > 0) {
        parameters.set(searchTermParameterName, searchTerm);
    }

    if (query.contentType != null) {
        parameters.set(contentTypeParameterName, ContentType[query.contentType]);
    }

    if (query.scope != null && query.scope !== defaultScope) {
        parameters.set(scopeParameterName, query.scope);
    }

    if (query.page > firstPage) {
        parameters.set(pageParameterName, String(query.page));
    }

    return parameters;
};
