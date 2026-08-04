import { useEffect } from 'react';
import { PageHeader } from '../components/coreUI/pageHeader';

export function About() {
    useEffect(() => {
        document.title = 'About — Glory 2 Him';
    }, []);

    return (
        <>
            <PageHeader title="About Glory 2 Him" />

            <section className="pt-4 pb-5">
                <div className="container">
                    <div className="row">
                        <div className="col-lg-10 mx-auto">
                            <p className="lead">
                                Glory 2 Him exists to share the good news of Jesus Christ — to encourage,
                                to build up, and to point all glory back to Him.
                            </p>
                            <p>
                                This is a demo site built on the Blogzine theme. As we begin to model real
                                content, these sample pages will serve as templates for the stories,
                                reflections, and resources that follow.
                            </p>
                            <blockquote className="blockquote my-4">
                                <p className="mb-2">"Jesus answered, 'I am the way and the truth and the life. No one comes to the Father except through me.'"</p>
                                <footer className="blockquote-footer">John 14:6</footer>
                            </blockquote>
                        </div>
                    </div>

                    <div className="row g-4 mt-2">
                        <div className="col-md-4">
                            <div className="card card-body h-100 text-center">
                                <i className="bi bi-book display-6 text-primary mb-2"></i>
                                <h5>Rooted in Scripture</h5>
                                <p className="mb-0">Everything we share is anchored in God's Word.</p>
                            </div>
                        </div>
                        <div className="col-md-4">
                            <div className="card card-body h-100 text-center">
                                <i className="bi bi-people display-6 text-primary mb-2"></i>
                                <h5>For the community</h5>
                                <p className="mb-0">Stories and encouragement to build one another up.</p>
                            </div>
                        </div>
                        <div className="col-md-4">
                            <div className="card card-body h-100 text-center">
                                <i className="bi bi-globe display-6 text-primary mb-2"></i>
                                <h5>Go and share</h5>
                                <p className="mb-0">"Go into all the world and preach the gospel." — Mark 16:15</p>
                            </div>
                        </div>
                    </div>
                </div>
            </section>
        </>
    );
}
