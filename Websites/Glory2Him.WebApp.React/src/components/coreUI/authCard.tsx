import { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Card } from './card';

// Centred card for the sign-in / sign-up layouts: a heading, a lead line, the caller's fields and
// a footer link to the opposite action. The parent owns the form and its submit.
export interface AuthCardProps {
    title: string;
    subtitle?: string;
    footerPrompt?: string;
    footerLinkText?: string;
    footerHref?: string;
    showSocialButtons?: boolean;
    children?: ReactNode;
}

export function AuthCard({
    title,
    subtitle,
    footerPrompt = '',
    footerLinkText = '',
    footerHref = '#',
    showSocialButtons = true,
    children,
}: AuthCardProps) {
    return (
        <section className="py-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-sm-10 col-md-8 col-lg-5">
                        <Card
                            cssClass="border shadow-sm"
                            footerContent={
                                <p className="mb-0 text-center small">
                                    {footerPrompt}
                                    <Link to={footerHref} className="btn-link">{footerLinkText}</Link>
                                </p>
                            }>
                            <div className="text-center mb-4">
                                <h2 className="h3 mb-2">{title}</h2>
                                {subtitle != null && subtitle.trim().length > 0 && (
                                    <p className="text-body-secondary mb-0">{subtitle}</p>
                                )}
                            </div>

                            {children}

                            {showSocialButtons && (
                                <>
                                    <div className="position-relative my-4">
                                        <hr />
                                        <p className="small position-absolute top-50 start-50 translate-middle bg-body px-2 mb-0 text-body-secondary">
                                            or
                                        </p>
                                    </div>

                                    <div className="d-grid gap-2">
                                        <a href="#" className="btn btn-outline-secondary">
                                            <i className="bi bi-google me-2"></i>Continue with Google
                                        </a>
                                        <a href="#" className="btn btn-outline-secondary">
                                            <i className="bi bi-facebook me-2"></i>Continue with Facebook
                                        </a>
                                    </div>
                                </>
                            )}
                        </Card>
                    </div>
                </div>
            </div>
        </section>
    );
}
