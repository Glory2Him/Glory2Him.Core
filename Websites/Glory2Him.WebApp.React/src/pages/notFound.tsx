import { useEffect } from 'react';
import { Link } from 'react-router-dom';

export function NotFound() {
    useEffect(() => {
        document.title = 'Page not found — Glory 2 Him';
    }, []);

    return (
        <section className="position-relative overflow-hidden py-5">
            <div className="container">
                <div className="row justify-content-center text-center py-5">
                    <div className="col-lg-8">
                        <h1 className="display-1 fw-bold text-primary">404</h1>
                        <h2 className="mb-3">Oops! This page could not be found.</h2>
                        <p className="mb-4">
                            The page you are looking for might have been moved, renamed, or may never
                            have existed. Let us guide you back home.
                        </p>
                        <Link to="/" className="btn btn-primary mb-0">Back to home</Link>
                    </div>
                </div>
            </div>
        </section>
    );
}
