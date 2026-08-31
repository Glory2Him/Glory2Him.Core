import { ContentItemDefaultPanel } from './contentItemDefaultPanel';

import {
    ContentItemTemplateProps
} from '../../models/components/contentItems/contentItemTemplate';

import './contentItems.css';

// The Quotes override. A quote is short enough to show WHOLE, and showing it whole is what makes
// engaging with it from the list fair — so the content block is the quote itself, standing large
// as the card's own heading, the author inline after the em-dash.
//
// DERIVES FROM ContentItemDefaultPanel, in the React register of inheritance: it renders the
// default template and replaces only its content slot. The meta row, the pills and the
// engagement row are therefore the default's, identically — an override changes how the content
// READS, never what the card offers.
//
// Two faces, decided by the imagery the consumer supplied: the quote VERTICALLY CENTRED over a
// dark hero where there is an image, and a quiet light block where there is not. Today nothing
// supplies a quote image — the projection deliberately gives Quote no placeholder — so the light
// block is what ships, and the hero face is already here for the day real images land (§4.9).
export function ContentItemQuotesPanel(props: ContentItemTemplateProps) {
    const { contentItem, onTitleClick } = props;

    const hasImage = (contentItem.imageUrl ?? '').length > 0;

    // A quote carries no title, so the CONTENT is the way into the detail surface — the same
    // destination onTitleClick names on every other card.
    const quoteText = (
        <button
            type="button"
            className="btn btn-link text-reset fw-bold p-0 mb-0 text-start"
            onClick={() => onTitleClick?.(contentItem)}>
            {contentItem.content}
            {(contentItem.author ?? '').length > 0 && <> — {contentItem.author}</>}
        </button>
    );

    const quoteBlock = hasImage ? (
        <div
            className="g2h-content-item-quote-hero card-overlay-bottom rounded-3 overflow-hidden position-relative d-flex align-items-center"
            style={{
                backgroundImage: `url(${contentItem.imageUrl})`,
                backgroundPosition: 'center center',
                backgroundSize: 'cover'
            }}>

            <h3 className="g2h-content-item-quote-text h3 text-white p-4 mb-0 w-100">
                {quoteText}
            </h3>
        </div>
    ) : (
        <div className="bg-light rounded-3 p-4">
            <h3 className="h3 mb-0">{quoteText}</h3>
        </div>
    );

    return <ContentItemDefaultPanel {...props} contentSlot={quoteBlock} />;
}
