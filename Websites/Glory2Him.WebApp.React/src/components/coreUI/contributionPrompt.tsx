import { Link } from 'react-router-dom';
import './coreUI.css';

// Invites the reader to send in something of their own. Sits in a bordered, rounded panel so it
// reads as a call to action rather than another block of sidebar copy. The pencil is floated
// rather than set in a flex row, so it behaves like the article's dropcap: the body copy wraps
// around it and then back underneath, while the heading keeps the panel's full width.
export interface ContributionPromptProps {
    heading?: string;
    body?: string;
    linkText?: string;
    href?: string;
    iconCssClass?: string;

    // A little under the 44px author avatar, so it reads as an icon rather than a portrait.
    iconSizePx?: number;
    cssClass?: string;

    // Contributing requires an account. The prompt itself stays a pure renderer — the page decides
    // isAuthenticated (via useAuth) and loginHref (via useLocation), the same split SecuredRoute
    // uses for its own login prompt.
    isAuthenticated: boolean;
    loginHref: string;
    loginPromptText?: string;
}

export function ContributionPrompt({
    heading = 'Have something to share?',
    body = 'A story, a testimony, or a verse that carried you through — if it might encourage '
        + 'someone else, we would love to read it.',
    linkText = 'Submit a contribution',
    href = '/posts/contribute',
    iconCssClass = 'bi-pencil-square',
    iconSizePx = 36,
    cssClass = 'mb-4',
    isAuthenticated,
    loginHref,
    loginPromptText = 'Login to share something',
}: ContributionPromptProps) {
    return (
        <div className={`border rounded-3 p-3 p-lg-4 g2h-contribute ${cssClass}`}>
            <h4 className="mb-2">{heading}</h4>

            {/* The pencil sits inside the paragraph, exactly as the article's dropcap does, so it
                floats against the body copy and leaves the heading on its own full-width line
                above. */}
            <p className="mb-3">
                <i
                    className={`bi ${iconCssClass} text-primary lh-1 g2h-contribute-icon`}
                    style={{ fontSize: `${iconSizePx}px` }}
                    aria-hidden="true"></i>{body}
            </p>

            {isAuthenticated ? (
                <Link to={href} className="btn btn-sm btn-primary-soft mb-0">
                    {linkText}<i className="bi bi-arrow-right ms-1"></i>
                </Link>
            ) : (
                <Link to={loginHref} className="btn btn-sm btn-primary-soft mb-0">
                    <i className="bi bi-box-arrow-in-right me-1"></i>{loginPromptText}
                </Link>
            )}
        </div>
    );
}
