import { Location, NavigateFunction } from 'react-router-dom';
import { bibleReferenceHref } from '../bibleReferences/toUsfmReference';

import {
    ContentItemSearchItem
} from '../../../models/components/contentItems/contentItemSearchItem';

// The navigation half of the search panel family's contract, built once for the pages that feed
// it. The panel raises the event; the PAGE decides where it leads — and every redirect built
// here carries the origin in router state, so the destination can offer a true way back instead
// of guessing at history.
//
// The detail destination is a parameter because it is exactly what differs between the surfaces:
// the public feed and "my posts" read an item at /posts/{id} today, and the moderation queue
// will point at the moderation detail once #350 builds one — the cards never change.
export interface ContentItemFeedNavigation {
    onTitleClick: (item: ContentItemSearchItem) => void;
    onReadMoreClick: (item: ContentItemSearchItem) => void;
    onCommentsClick: (item: ContentItemSearchItem) => void;
    onBibleReferenceClick: (item: ContentItemSearchItem, bibleReference: string) => void;
}

export const buildContentItemFeedNavigation = (
    navigate: NavigateFunction,
    location: Location,
    detailHrefOf: (item: ContentItemSearchItem) => string =
        (item) => `/posts/${item.id}`): ContentItemFeedNavigation => {

    const from = `${location.pathname}${location.search}`;

    const toDetail = (item: ContentItemSearchItem) =>
        navigate(detailHrefOf(item), { state: { from } });

    return {
        onTitleClick: toDetail,
        onReadMoreClick: toDetail,

        // The same page, aimed at its comment section — a fragment rather than a different
        // route, so the destination stays one page however the reader arrived.
        onCommentsClick: (item) =>
            navigate(`${detailHrefOf(item)}#comments`, { state: { from } }),

        // A reference leads to the passage itself, exactly as the pill family has always
        // addressed it.
        onBibleReferenceClick: (_item, bibleReference) =>
            navigate(bibleReferenceHref(bibleReference), { state: { from } })
    };
};
