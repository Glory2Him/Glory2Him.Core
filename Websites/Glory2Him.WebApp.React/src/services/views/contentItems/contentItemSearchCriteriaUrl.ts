import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';

import {
    ContentItemSearchCriteria
} from '../../../models/components/contentItems/contentItemSearchItem';

// The criteria's round trip through the URL, shared by every page that feeds the search panel
// family — the header's search, a shared link and the back button all land with the results
// already showing, exactly as /Search does with ?q=.
//
// The URL carries the ContentType MEMBER NAME rather than the number: a link reading
// ?type=Devotional survives somebody reading it, and the numbering is a wire contract, not
// something to put in front of people. The submitted-by criterion carries both halves — the id
// the read filters on and the name the chip shows — because a chip rendering a bare account id
// would be doing the one thing the id must never do.
const queryParameterName = 'q';
const contentTypeParameterName = 'type';
const authorParameterName = 'author';
const submittedByIdParameterName = 'by';
const submittedByNameParameterName = 'byName';
const tagParameterName = 'tag';

const toContentType = (value: string | null): ContentType | null => {
    if (value == null || value.length === 0) {
        return null;
    }

    const member = ContentType[value as keyof typeof ContentType];

    return typeof member === 'number' ? member : null;
};

export const toContentItemSearchCriteria = (
    searchParams: URLSearchParams): ContentItemSearchCriteria => {
    const submittedById = searchParams.get(submittedByIdParameterName) ?? '';

    return {
        query: searchParams.get(queryParameterName) ?? '',
        contentType: toContentType(searchParams.get(contentTypeParameterName)),
        author: searchParams.get(authorParameterName) ?? '',

        submittedBy: submittedById.length === 0
            ? null
            : {
                id: submittedById,
                name: searchParams.get(submittedByNameParameterName) ?? ''
            },

        tag: searchParams.get(tagParameterName)
    };
};

export const toContentItemSearchParams = (
    criteria: ContentItemSearchCriteria): URLSearchParams => {
    const parameters = new URLSearchParams();

    if (criteria.query.trim().length > 0) {
        parameters.set(queryParameterName, criteria.query.trim());
    }

    if (criteria.contentType != null) {
        parameters.set(contentTypeParameterName, ContentType[criteria.contentType]);
    }

    if (criteria.author.trim().length > 0) {
        parameters.set(authorParameterName, criteria.author.trim());
    }

    if (criteria.submittedBy != null) {
        parameters.set(submittedByIdParameterName, criteria.submittedBy.id);

        if (criteria.submittedBy.name.length > 0) {
            parameters.set(submittedByNameParameterName, criteria.submittedBy.name);
        }
    }

    if ((criteria.tag ?? '').length > 0) {
        parameters.set(tagParameterName, criteria.tag ?? '');
    }

    return parameters;
};
