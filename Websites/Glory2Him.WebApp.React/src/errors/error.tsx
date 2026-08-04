import { useEffect } from 'react';
import { useRouteError } from 'react-router-dom';

// The router's error page, matching the Blazor /Error page. The Blazor page showed the
// server-side request id; a client-side router has none, so the route error's own detail
// stands in its place under the same conditional markup.
export default function ErrorPage() {
    const error = useRouteError() as { statusText?: string, message?: string } | undefined;

    const errorDetail = error?.statusText || error?.message || '';
    const showErrorDetail = errorDetail.length > 0;

    useEffect(() => {
        document.title = 'Error — Glory 2 Him';
    }, []);

    return (
        <section className="position-relative overflow-hidden py-5">
            <div className="container">
                <div className="row justify-content-center text-center py-5">
                    <div className="col-lg-8">
                        <h1 className="display-4 fw-bold text-danger">Something went wrong</h1>
                        <p className="mb-4">An unexpected error occurred while processing your request.</p>

                        {showErrorDetail && (
                            <p className="text-muted">
                                <strong>Error:</strong> <code>{errorDetail}</code>
                            </p>
                        )}

                        <a href="/" className="btn btn-primary mb-0">Back to home</a>
                    </div>
                </div>
            </div>
        </section>
    );
}
