import { FormEvent, useEffect } from 'react';
import { PageHeader } from '../components/coreUI/pageHeader';

export function Contact() {
    useEffect(() => {
        document.title = 'Contact — Glory 2 Him';
    }, []);

    // The Blazor page's form was decorative — no submit handler behind it. Swallow the
    // submit so the browser does not reload the page.
    const onSubmit = (event: FormEvent<HTMLFormElement>) =>
        event.preventDefault();

    return (
        <>
            <PageHeader title="Contact us" />

            <section className="pt-4 pb-5">
                <div className="container">
                    <div className="row g-4">
                        <div className="col-lg-7">
                            <h3 className="mb-3">Send us a message</h3>
                            <form className="row g-3" onSubmit={onSubmit}>
                                <div className="col-md-6">
                                    <label className="form-label" htmlFor="contact-name">Name</label>
                                    <input type="text" className="form-control" id="contact-name" placeholder="Your name" />
                                </div>
                                <div className="col-md-6">
                                    <label className="form-label" htmlFor="contact-email">Email</label>
                                    <input type="email" className="form-control" id="contact-email" placeholder="you@example.com" />
                                </div>
                                <div className="col-12">
                                    <label className="form-label" htmlFor="contact-subject">Subject</label>
                                    <input type="text" className="form-control" id="contact-subject" placeholder="How can we help?" />
                                </div>
                                <div className="col-12">
                                    <label className="form-label" htmlFor="contact-message">Message</label>
                                    <textarea className="form-control" id="contact-message" rows={5} placeholder="Your message"></textarea>
                                </div>
                                <div className="col-12">
                                    <button type="submit" className="btn btn-primary mb-0">Send message</button>
                                </div>
                            </form>
                        </div>
                        <div className="col-lg-5">
                            <div className="card card-body h-100">
                                <h4 className="mb-3">Glory 2 Him</h4>
                                <p>We would love to hear from you. Reach out and we will respond as soon as we can.</p>
                                <ul className="list-unstyled mb-0">
                                    <li className="mb-2"><i className="bi bi-envelope text-primary me-2"></i>hello@glory2him.org</li>
                                    <li className="mb-2"><i className="bi bi-globe text-primary me-2"></i>Sharing the good news, everywhere.</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </div>
            </section>
        </>
    );
}
