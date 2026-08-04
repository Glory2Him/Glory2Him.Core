import { Link } from 'react-router-dom';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';

// Blogzine offline.html: the no-connection state.
export const OfflineSample = () => {
    useDocumentTitle('Offline — Sample — Glory 2 Him');

    return (
        <SampleShell title="Offline" sourceFile="offline.html">
            <section className="py-5">
                <div className="container">
                    <div className="row justify-content-center text-center">
                        <div className="col-lg-7">
                            <i className="bi bi-wifi-off display-1 text-body-secondary"></i>
                            <h1 className="h2 mt-4 mb-3">You are offline</h1>
                            <p className="lead text-body-secondary mb-4">
                                We could not reach the network. Check your connection and try again — your
                                place in the journal is safe.
                            </p>

                            <Link to="/" className="btn btn-primary">
                                <i className="bi bi-arrow-clockwise me-1"></i>Try again
                            </Link>
                        </div>
                    </div>
                </div>
            </section>
        </SampleShell>
    );
};
