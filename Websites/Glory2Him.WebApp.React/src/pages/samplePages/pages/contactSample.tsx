import { Button } from '../../../components/coreUI/button';
import { Card } from '../../../components/coreUI/card';
import { FormText } from '../../../components/coreUI/formText';
import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';

// Blogzine contact-us.html: contact detail cards beside the enquiry form.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Contact', isActive: true },
];

export const ContactSample = () => {
    useDocumentTitle('Contact — Sample — Glory 2 Him');

    return (
        <SampleShell title="Contact" sourceFile="contact-us.html">
            <HeroBanner title="Contact us" crumbs={crumbs} />

            <section className="py-5">
                <div className="container">
                    <div className="row g-4 mb-5">
                        <div className="col-md-4">
                            <Card cssClass="border text-center h-100">
                                <i className="bi bi-envelope fs-1 text-primary"></i>
                                <h6 className="mt-3 mb-1">Email us</h6>
                                <p className="mb-0 small text-body-secondary">hello@glory2him.org</p>
                            </Card>
                        </div>
                        <div className="col-md-4">
                            <Card cssClass="border text-center h-100">
                                <i className="bi bi-chat-heart fs-1 text-primary"></i>
                                <h6 className="mt-3 mb-1">Prayer requests</h6>
                                <p className="mb-0 small text-body-secondary">We would love to pray with you.</p>
                            </Card>
                        </div>
                        <div className="col-md-4">
                            <Card cssClass="border text-center h-100">
                                <i className="bi bi-people fs-1 text-primary"></i>
                                <h6 className="mt-3 mb-1">Write with us</h6>
                                <p className="mb-0 small text-body-secondary">Share the story God gave you.</p>
                            </Card>
                        </div>
                    </div>

                    <div className="row justify-content-center">
                        <div className="col-lg-8">
                            <Card cssClass="border" headerContent="Send us a message">
                                <div className="row">
                                    <div className="col-md-6">
                                        <FormText label="Your name" placeholder="Jane Doe" />
                                    </div>
                                    <div className="col-md-6">
                                        <FormText label="Email address" placeholder="jane@example.com" />
                                    </div>
                                </div>

                                <FormText label="Subject" placeholder="How can we help?" />

                                <div className="mb-3">
                                    <label className="form-label">Message</label>
                                    <textarea
                                        className="form-control"
                                        rows={5}
                                        placeholder="Tell us a little more…"></textarea>
                                </div>

                                <Button color="primary">Send message</Button>
                            </Card>
                        </div>
                    </div>
                </div>
            </section>
        </SampleShell>
    );
};
