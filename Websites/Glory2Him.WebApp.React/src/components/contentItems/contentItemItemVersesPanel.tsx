import { ContentItemItemDefaultPanel } from './contentItemItemDefaultPanel';

import {
    ContentItemItemTemplateProps
} from '../../models/components/contentItems/contentItemItemTemplate';

import './contentItems.css';

// The Verse Image override — the ContentItemItem{ContentType}Panel the redesign sketched, landed
// now that ContentType.Verses exists. A verse card IS its verse: the content arrives whole, with
// its own quotation marks, reference and translation ("…" — John 3:16 ESV), so the block renders
// it exactly as written and appends NOTHING — unlike the quotes template, which adds the author
// after an em-dash, because a verse's content already ends in one.
//
// DERIVES FROM ContentItemItemDefaultPanel, exactly as the quotes override does: the default is
// rendered with only the content slot replaced, so the meta row (where "Author The Bible" shows),
// the pills and the engagement row stay the default's own.
//
// Two faces, decided by the imagery the consumer supplied: the verse VERTICALLY CENTRED over a
// dark hero where there is an image — the face the design's mock draws — and the quiet light
// block where there is not, which is what ships until real header images land (§4.9).
export function ContentItemItemVersesPanel(props: ContentItemItemTemplateProps) {
    const { contentItem, onTitleClick } = props;

    const hasImage = (contentItem.imageUrl ?? '').length > 0;

    // A verse image carries no title, so the VERSE is the way into the detail surface — the same
    // destination onTitleClick names on every other card.
    const verseText = (
        <button
            type="button"
            className="btn btn-link text-reset fw-bold p-0 mb-0 text-start"
            onClick={() => onTitleClick?.(contentItem)}>
            {contentItem.content}
        </button>
    );

    const verseBlock = hasImage ? (
        <div
            className="g2h-content-item-quote-hero card-overlay-bottom rounded-3 overflow-hidden position-relative d-flex align-items-center"
            style={{
                backgroundImage: `url(${contentItem.imageUrl})`,
                backgroundPosition: 'center center',
                backgroundSize: 'cover'
            }}>

            <h3 className="g2h-content-item-quote-text h3 text-white p-4 mb-0 w-100">
                {verseText}
            </h3>
        </div>
    ) : (
        <div className="bg-light rounded-3 p-4">
            <h3 className="h3 mb-0">{verseText}</h3>
        </div>
    );

    return <ContentItemItemDefaultPanel {...props} contentSlot={verseBlock} />;
}
