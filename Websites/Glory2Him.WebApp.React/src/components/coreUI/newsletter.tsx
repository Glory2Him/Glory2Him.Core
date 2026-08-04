// Reusable newsletter sign-up call to action.
export interface NewsletterProps {
    heading?: string;
    subheading?: string;
    buttonText?: string;
}

export function Newsletter({
    heading = 'Join our community',
    subheading = 'Get encouraging stories and Scripture delivered to your inbox.',
    buttonText = 'Subscribe',
}: NewsletterProps) {
    return (
        <div className="bg-primary bg-opacity-10 p-4 p-sm-5 rounded-3 text-center">
            <h2 className="mb-2">{heading}</h2>
            <p className="mb-4">{subheading}</p>
            <form className="row row-cols-sm-auto g-2 justify-content-center align-items-center">
                <div className="col-12">
                    <label className="visually-hidden" htmlFor="newsletter-email">Email address</label>
                    <input type="email" className="form-control" id="newsletter-email" placeholder="Enter your email address" />
                </div>
                <div className="col-12">
                    <button type="submit" className="btn btn-primary m-0">{buttonText}</button>
                </div>
            </form>
        </div>
    );
}
