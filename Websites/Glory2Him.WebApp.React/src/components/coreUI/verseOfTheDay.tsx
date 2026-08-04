import { Link } from 'react-router-dom';

// Tinted strip carrying the day's verse, sitting directly under the header.
export interface VerseOfTheDayProps {
    verse: string;
    label?: string;
    href?: string;
}

export function VerseOfTheDay({ verse, label = 'Verse of the day:', href = '#' }: VerseOfTheDayProps) {
    return (
        <section className="py-2">
            <div className="container">
                <div className="row g-0">
                    <div className="col-12 bg-primary bg-opacity-10 p-2 rounded">
                        <div className="d-sm-flex align-items-center text-center text-sm-start">
                            <div className="me-3">
                                <span className="badge bg-primary p-2 px-3">{label}</span>
                            </div>
                            <div>
                                <Link to={href} className="text-reset btn-link">{verse}</Link>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
