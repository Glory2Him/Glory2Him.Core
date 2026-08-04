import { FormEvent, useState } from 'react';
import { accountService } from '../../services/foundations/accountService';
import { StatusMessage, extractApiErrorMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/ResendEmailConfirmation.razor. Like the Blazor page,
// success never reveals whether the account exists.
export function ResendEmailConfirmation() {
    const [email, setEmail] = useState('');
    const [validationMessage, setValidationMessage] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);

    const resendEmailConfirmation = accountService.useResendEmailConfirmation();

    const validate = (): boolean => {
        if (email.trim().length === 0) {
            setValidationMessage('The Email field is required.');
            return false;
        }

        if (!/^[^@\s]+@[^@\s]+$/.test(email.trim())) {
            setValidationMessage('The Email field is not a valid e-mail address.');
            return false;
        }

        setValidationMessage(null);

        return true;
    };

    const onValidSubmit = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        if (!validate()) {
            return;
        }

        resendEmailConfirmation.mutate(email.trim(), {
            onSuccess: () =>
                setMessage('Verification email sent. Please check your email.'),
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'We could not process your request. Please try again.')}`);
            }
        });
    };

    return (
        <>
            <h1>Resend email confirmation</h1>
            <h2>Enter your email.</h2>
            <hr />
            <StatusMessage message={message} />
            <div className="row">
                <div className="col-md-4">
                    <form onSubmit={onValidSubmit} noValidate>
                        {validationMessage != null && (
                            <ul className="text-danger" role="alert">
                                <li>{validationMessage}</li>
                            </ul>
                        )}
                        <div className="form-floating mb-3">
                            <input
                                value={email}
                                onChange={(event) => setEmail(event.target.value)}
                                id="Input.Email"
                                className="form-control"
                                aria-required="true"
                                placeholder="name@example.com" />
                            <label htmlFor="Input.Email" className="form-label">Email</label>
                            {validationMessage != null && (
                                <div className="text-danger">{validationMessage}</div>
                            )}
                        </div>
                        <button
                            type="submit"
                            className="w-100 btn btn-lg btn-primary"
                            disabled={resendEmailConfirmation.isPending}>
                            Resend
                        </button>
                    </form>
                </div>
            </div>
        </>
    );
}
