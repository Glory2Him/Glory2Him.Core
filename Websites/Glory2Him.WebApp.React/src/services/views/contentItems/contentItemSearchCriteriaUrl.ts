import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';

import {
    ShareabilityBasis,
    shareabilityBasisMemberNames
} from '../../../models/components/contentItems/contentItemFormItem';

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
const tagsParameterName = 'tags';
const tagMatchModeParameterName = 'tagMode';
const bibleReferencesParameterName = 'refs';
const bibleReferenceMatchModeParameterName = 'refMode';
const shareabilityParameterName = 'shareability';

// The basis travels by MEMBER NAME too, for the same reason the type does.
const toShareabilityBasis = (value: string | null): ShareabilityBasis | null => {
    if (value == null || value.length === 0) {
        return null;
    }

    const match = (Object.entries(shareabilityBasisMemberNames) as [string, string][])
        .find(([, memberName]) => memberName === value);

    return match == null ? null : Number(match[0]) as ShareabilityBasis;
};

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

        tags: (searchParams.get(tagsParameterName) ?? '')
            .split(',')
            .map((tag) => tag.trim())
            .filter((tag) => tag.length > 0),

        tagMatchMode:
            searchParams.get(tagMatchModeParameterName) === 'all' ? 'all' : 'any',

        bibleReferences: (searchParams.get(bibleReferencesParameterName) ?? '')
            .split(',')
            .map((reference) => reference.trim())
            .filter((reference) => reference.length > 0),

        bibleReferenceMatchMode:
            searchParams.get(bibleReferenceMatchModeParameterName) === 'all'
                ? 'all'
                : 'any',

        shareabilityBasis:
            toShareabilityBasis(searchParams.get(shareabilityParameterName))
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

    if (criteria.tags.length > 0) {
        parameters.set(tagsParameterName, criteria.tags.join(','));

        // 'any' is the default, so only 'all' earns a parameter — shorter links, and a
        // missing mode reads back as the default it was.
        if (criteria.tagMatchMode === 'all') {
            parameters.set(tagMatchModeParameterName, 'all');
        }
    }

    if (criteria.bibleReferences.length > 0) {
        parameters.set(bibleReferencesParameterName, criteria.bibleReferences.join(','));

        if (criteria.bibleReferenceMatchMode === 'all') {
            parameters.set(bibleReferenceMatchModeParameterName, 'all');
        }
    }

    if (criteria.shareabilityBasis != null) {
        parameters.set(
            shareabilityParameterName,
            shareabilityBasisMemberNames[criteria.shareabilityBasis]);
    }

    return parameters;
};
