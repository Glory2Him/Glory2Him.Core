import { useRouteError } from "react-router-dom";

export default function ErrorPage() {
    const error = useRouteError() as { statusText?: string, message?: string };

    return (
        <div className="container mt-5">
            <div className="row justify-content-center">
                <div className="col-md-8 text-center py-5">
                    <h1>Something went wrong</h1>
                    <p className="lead">Sorry, an unexpected error has occurred.</p>
                    <p className="text-muted">
                        <i>{error?.statusText || error?.message}</i>
                    </p>
                </div>
            </div>
        </div>
    );
}
