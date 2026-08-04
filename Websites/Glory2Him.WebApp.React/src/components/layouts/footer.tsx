import { ReactElement } from "react";
import { Link } from "react-router-dom";
import BrandComponent from "../coreUI/brand";

// Blogzine footer, ported from the Blazor FooterComponent: about + newsletter, widget columns,
// hot topics and copyright bar. Presentational only (ts-ui-001).
export default function FooterComponent(): ReactElement {
    const currentYear = new Date().getUTCFullYear();

    return (
        <footer className="bg-dark pt-5">
            <div className="container">
                {/* About and Newsletter START */}
                <div className="row pt-3 pb-4">
                    <div className="col-md-3">
                        <BrandComponent variant="banner" bannerHeightPx={72} />
                    </div>
                    <div className="col-md-5">
                        <p className="text-body-secondary">
                            Glory 2 Him is a place to read, reflect, and be encouraged in the good news of
                            Jesus Christ — sharing stories that point all glory back to Him.
                        </p>
                    </div>
                    <div className="col-md-4">
                        {/* Form */}
                        <form className="row row-cols-lg-auto g-2 align-items-center justify-content-end">
                            <div className="col-12">
                                <input type="email" className="form-control" placeholder="Enter your email address" />
                            </div>
                            <div className="col-12">
                                <button type="submit" className="btn btn-primary m-0">Subscribe</button>
                            </div>
                            <div className="form-text mt-2">
                                By subscribing you agree to our
                                {" "}<a href="#" className="text-decoration-underline text-reset">Privacy Policy</a>
                            </div>
                        </form>
                    </div>
                </div>
                {/* About and Newsletter END */}

                {/* Divider */}
                <hr />

                {/* Widgets START */}
                <div className="row pt-5">
                    {/* Footer Widget */}
                    <div className="col-md-6 col-lg-3 mb-4">
                        <h5 className="mb-4 text-white">Explore</h5>
                        <ul className="nav flex-column text-primary-hover">
                            <li className="nav-item"><Link className="nav-link pt-0" to="/">Home</Link></li>
                            <li className="nav-item"><Link className="nav-link" to="/About-Us">About</Link></li>
                            <li className="nav-item"><Link className="nav-link" to="/Categories">Journal</Link></li>
                            <li className="nav-item"><Link className="nav-link" to="/Author">Authors</Link></li>
                            <li className="nav-item"><Link className="nav-link" to="/Contact-Us">Contact us</Link></li>
                        </ul>
                    </div>

                    {/* Footer Widget */}
                    <div className="col-md-6 col-lg-3 mb-4">
                        <h5 className="mb-4 text-white">Account</h5>
                        <ul className="nav flex-column text-primary-hover">
                            <li className="nav-item"><Link className="nav-link pt-0" to="/Account/Login">Sign in</Link></li>
                            <li className="nav-item"><Link className="nav-link" to="/Account/Register">Sign up</Link></li>
                            <li className="nav-item"><Link className="nav-link" to="/Account/Manage">My account</Link></li>
                        </ul>
                    </div>

                    {/* Footer Widget */}
                    <div className="col-sm-6 col-lg-3 mb-4">
                        <h5 className="mb-4 text-white">Get Regular Updates</h5>
                        <ul className="nav flex-column text-primary-hover">
                            <li className="nav-item"><a className="nav-link pt-0" href="#"><i className="fab fa-whatsapp fa-fw me-2"></i>WhatsApp</a></li>
                            <li className="nav-item"><a className="nav-link" href="#"><i className="fab fa-youtube fa-fw me-2"></i>YouTube</a></li>
                            <li className="nav-item"><a className="nav-link" href="#"><i className="far fa-bell fa-fw me-2"></i>Notifications</a></li>
                            <li className="nav-item"><a className="nav-link" href="#"><i className="far fa-envelope fa-fw me-2"></i>Newsletters</a></li>
                        </ul>
                    </div>

                    {/* Footer Widget */}
                    <div className="col-sm-6 col-lg-3 mb-4">
                        <h5 className="mb-4 text-white">A word of hope</h5>
                        <p className="text-body-secondary">
                            "Jesus answered, 'I am the way and the truth and the life. No one comes to the
                            Father except through me.'" — John 14:6
                        </p>
                    </div>
                </div>
                {/* Widgets END */}

                {/* Hot topics START */}
                <div className="row">
                    <h5 className="mb-2 text-white">Hot topics</h5>
                    <ul className="list-inline text-primary-hover lh-lg">
                        <li className="list-inline-item"><Link to="/Categories">Faith</Link></li>
                        <li className="list-inline-item"><Link to="/Categories">Hope</Link></li>
                        <li className="list-inline-item"><Link to="/Categories">Prayer</Link></li>
                        <li className="list-inline-item"><Link to="/Categories">Scripture</Link></li>
                        <li className="list-inline-item"><Link to="/Categories">Testimony</Link></li>
                        <li className="list-inline-item"><Link to="/Categories">Worship</Link></li>
                        <li className="list-inline-item"><Link to="/Categories">Community</Link></li>
                        <li className="list-inline-item"><Link to="/Categories">Missions</Link></li>
                    </ul>
                </div>
                {/* Hot topics END */}
            </div>

            {/* Footer copyright START */}
            <div className="bg-dark-overlay-3 mt-5">
                <div className="container">
                    <div className="row align-items-center justify-content-md-between py-4">
                        <div className="col-md-6">
                            <div className="text-center text-md-start text-primary-hover text-body-secondary">
                                ©{currentYear} Glory 2 Him. Free to use to help share the Gospel.
                            </div>
                        </div>
                        <div className="col-md-6 d-sm-flex align-items-center justify-content-center justify-content-md-end">
                            <ul className="nav text-primary-hover text-center text-sm-end justify-content-center mt-3 mt-md-0">
                                <li className="nav-item"><a className="nav-link" href="#">Terms</a></li>
                                <li className="nav-item"><a className="nav-link" href="#">Privacy</a></li>
                                <li className="nav-item"><a className="nav-link pe-0" href="#">Cookies</a></li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
            {/* Footer copyright END */}
        </footer>
    );
}
