import { ContentItemItemDefaultPanel } from './contentItemItemDefaultPanel';

import {
    ContentItemItemTemplateProps
} from '../../models/components/contentItems/contentItemItemTemplate';

import './contentItems.css';

// The Quotes override. A quote is short enough to show WHOLE, and showing it whole is what makes
// engaging with it from the list fair — so the content block is the quote itself, standing as
// the card's own heading, the author inline after the em-dash.
//
// DERIVES FROM ContentItemItemDefaultPanel, in the React register of inheritance: it renders the
// default template and replaces only its content slot. The meta row, the pills and the
// engagement row are therefore the default's, identically — an override changes how the content
// READS, never what the card offers.
//
// Two faces, decided by the imagery the consumer supplied: a dark hero with the quote over the
// image where there is one, a quiet light block where there is not — exactly the two the design
// screenshots carry.
export function ContentItemItemQuotesPanel(props: ContentItemItemTemplateProps) {
    const { contentItem, onTitleClick } = props;

    const hasImage = (contentItem.imageUrl ?? '').length > 0;

    // A quote carries no title, so the CONTENT is the way into the detail surface — the same
    // destination onTitleClick names on every other card.
    const quoteBlock = hasImage ? (
        <div
            className="g2h-content-item-quote-hero card-overlay-bottom rounded-3 overflow-hidden position-relative d-flex align-items-end"
            style={{
                backgroundImage: `url(${contentItem.imageUrl})`,
                backgroundPosition: 'center center',
                backgroundSize: 'cover'
            }}>

            <h3 className="g2h-content-item-quote-text h4 text-white p-3 p-sm-4 mb-0 w-100">
                <button
                    type="button"
                    className="btn btn-link text-reset fw-bold p-0 mb-0 text-start"
                    onClick={() => onTitleClick?.(contentItem)}>
                    {contentItem.content}
                    {(contentItem.author ?? '').length > 0 && <> — {contentItem.author}</>}
                </button>
            </h3>
        </div>
    ) : (
        <div className="bg-light rounded-3 p-4 text-center">
            <h3 className="h4 mb-0">
                <button
                    type="button"
                    className="btn btn-link text-reset fw-bold p-0 mb-0"
                    onClick={() => onTitleClick?.(contentItem)}>
                    {contentItem.content}
                    {(contentItem.author ?? '').length > 0 && <> — {contentItem.author}</>}
                </button>
            </h3>
        </div>
    );

    return <ContentItemItemDefaultPanel {...props} contentSlot={quoteBlock} />;
}
