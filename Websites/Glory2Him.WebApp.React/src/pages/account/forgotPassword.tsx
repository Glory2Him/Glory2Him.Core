import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { accountService } from '../../services/foundations/accountService';
import { StatusMessage, extractApiErrorMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/ForgotPassword.razor. Like the Blazor page, success
// never reveals whether the account exists — it always lands on the confirmation page.
export function ForgotPassword() {
    const [email, setEmail] = useState('');
    const [validationMessage, setValidationMessage] = useState<string | null>(null);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const forgotPassword = accountService.useForgotPassword();
    const navigate = useNavigate();

    const onValidSubmit = (event: FormEvent) => {
        event.preventDefault();
        setErrorMessage(null);

        if (email.trim().length === 0) {
            setValidationMessage('The Email field is required.');
            return;
        }

        if (!/^[^@\s]+@[^@\s]+$/.test(email.trim())) {
            setValidationMessage('The Email field is not a valid e-mail address.');
            return;
        }

        setValidationMessage(null);

        forgotPassword.mutate(email.trim(), {
            onSuccess: () => navigate('/Account/ForgotPasswordConfirmation'),
            onError: (error: unknown) => {
                setErrorMessage(extractApiErrorMessage(
                    error, 'Error: We could not process your request. Please try again.'));
            }
        });
    };

    return (
        <>
            <h1>Forgot your password?</h1>
            <h2>Enter your email.</h2>
            <hr />
            <div className="row">
                <div className="col-md-4">
                    <StatusMessage message={errorMessage} />
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
                                autoComplete="username"
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
                            disabled={forgotPassword.isPending}>
                            Reset password
                        </button>
                    </form>
                </div>
            </div>
        </>
    );
}
