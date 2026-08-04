import { Link } from 'react-router-dom';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';

// Blogzine 404.html: oversized code, a short apology and the way back.
export const Error404Sample = () => {
    useDocumentTitle('Error 404 — Sample — Glory 2 Him');

    return (
        <SampleShell title="Error 404" sourceFile="404.html">
            <section className="py-5">
                <div className="container">
                    <div className="row justify-content-center text-center">
                        <div className="col-lg-7">
                            <h1 className="display-1 fw-bold text-primary mb-0">404</h1>
                            <h2 className="h3 mb-3">We could not find that page</h2>
                            <p className="lead text-body-secondary mb-4">
                                The link may be broken, or the page may have moved. Let's get you back to
                                something worth reading.
                            </p>

                            <div className="d-flex flex-wrap justify-content-center gap-2">
                                <Link to="/" className="btn btn-primary">
                                    <i className="bi bi-house me-1"></i>Back to home
                                </Link>
                                <Link to="/Categories" className="btn btn-outline-secondary">
                                    Browse the journal
                                </Link>
                            </div>
                        </div>
                    </div>
                </div>
            </section>
        </SampleShell>
    );
};
