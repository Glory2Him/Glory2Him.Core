import { Link } from 'react-router-dom';
import { useDocumentTitle } from './useDocumentTitle';

// Holding page for the contribution flow. ContributionPrompt (home and post detail) already
// links here for signed-in readers — this placeholder just confirms the destination exists
// until the actual submission form is built.
export function Contribute() {
    useDocumentTitle('Submit a contribution — Glory 2 Him');

    return (
        <section className="position-relative overflow-hidden py-5">
            <div className="container">
                <div className="row justify-content-center text-center py-5">
                    <div className="col-lg-8">
                        <i className="bi bi-pencil-square text-primary display-1" aria-hidden="true"></i>
                        <h2 className="mb-3 mt-3">Contributions are coming soon</h2>
                        <p className="mb-4">
                            We are still building the form for sharing your story, testimony, or verse.
                            Check back soon — we would love to read it.
                        </p>
                        <Link to="/" className="btn btn-primary mb-0">Back to home</Link>
                    </div>
                </div>
            </div>
        </section>
    );
}
