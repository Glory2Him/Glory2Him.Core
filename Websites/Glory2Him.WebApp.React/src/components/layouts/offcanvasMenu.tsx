import { ReactElement } from "react";
import { Link } from "react-router-dom";
import BrandComponent from "../coreUI/brand";

// Slide-out offcanvas menu (Blogzine chrome), ported from the Blazor OffcanvasMenuComponent.
// The header's toggler opens it via Bootstrap's offcanvas data API (bootstrap.bundle.min.js,
// loaded globally, delegates at the document level so it reaches React-rendered DOM).
// Presentational only (ts-ui-001).
export default function OffcanvasMenuComponent(): ReactElement {
    return (
        <div className="offcanvas offcanvas-end" tabIndex={-1} id="offcanvasMenu">
            <div className="offcanvas-header justify-content-end">
                <button type="button" className="btn-close text-reset" data-bs-dismiss="offcanvas" aria-label="Close"></button>
            </div>
            <div className="offcanvas-body d-flex flex-column pt-0">
                <div>
                    <div className="my-3">
                        <BrandComponent variant="banner" bannerHeightPx={64} />
                    </div>
                    <p>
                        Glory 2 Him — sharing the good news of Jesus Christ. Read, reflect, and be
                        encouraged as we point all glory back to Him.
                    </p>
                    {/* Nav START */}
                    <ul className="nav d-block flex-column my-4">
                        <li className="nav-item h5">
                            <Link className="nav-link" to="/">Home</Link>
                        </li>
                        <li className="nav-item h5">
                            <Link className="nav-link" to="/About-Us">About</Link>
                        </li>
                        <li className="nav-item h5">
                            <Link className="nav-link" to="/Categories">Our Journal</Link>
                        </li>
                        <li className="nav-item h5">
                            <Link className="nav-link" to="/Contact-Us">Contact Us</Link>
                        </li>
                    </ul>
                    {/* Nav END */}
                    <div className="bg-primary bg-opacity-10 p-4 mb-4 text-center w-100 rounded">
                        <span>Glory 2 Him</span>
                        <h3>Go and share the Gospel</h3>
                        <p>"Go into all the world and preach the gospel to all creation." — Mark 16:15</p>
                        <Link to="/About-Us" className="btn btn-warning">Learn more</Link>
                    </div>
                </div>
                <div className="mt-auto pb-3">
                    <p className="text-body mb-2 fw-bold">Glory 2 Him</p>
                    <address className="mb-0">Sharing the good news, everywhere.</address>
                    <Link to="/Contact-Us" className="text-body d-block">hello@glory2him.org</Link>
                </div>
            </div>
        </div>
    );
}
