// Round share buttons in each network's own colour, using the theme's bg-facebook / bg-twitter /
// bg-linkedin helpers.
export interface ShareLinksProps {
    heading?: string;
}

export function ShareLinks({ heading = 'Share this article' }: ShareLinksProps) {
    return (
        <>
            <h4 className="mb-3">{heading}</h4>

            <ul className="nav text-white-force">
                <li className="nav-item">
                    <a className="nav-link icon-md rounded-circle me-2 mb-2 p-0 fs-5 bg-facebook" href="#"
                        aria-label="Share on Facebook">
                        <i className="fab fa-facebook-square align-middle"></i>
                    </a>
                </li>
                <li className="nav-item">
                    <a className="nav-link icon-md rounded-circle me-2 mb-2 p-0 fs-5 bg-twitter" href="#"
                        aria-label="Share on Twitter">
                        <i className="fab fa-twitter-square align-middle"></i>
                    </a>
                </li>
                <li className="nav-item">
                    <a className="nav-link icon-md rounded-circle me-2 mb-2 p-0 fs-5 bg-linkedin" href="#"
                        aria-label="Share on LinkedIn">
                        <i className="fab fa-linkedin align-middle"></i>
                    </a>
                </li>
                <li className="nav-item">
                    <a className="nav-link icon-md rounded-circle me-2 mb-2 p-0 fs-5 bg-primary" href="#"
                        aria-label="Share by email">
                        <i className="far fa-envelope align-middle"></i>
                    </a>
                </li>
            </ul>
        </>
    );
}
