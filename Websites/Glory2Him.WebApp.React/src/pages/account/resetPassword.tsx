import { FormEvent, useState } from 'react';
import { Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { accountService } from '../../services/foundations/accountService';
import { StatusMessage, extractApiErrorMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/ResetPassword.razor: the reset code arrives in the
// query string; a missing code redirects to the invalid-reset page, exactly as Blazor did.
interface ValidationMessages {
    email?: string;
    password?: string;
    confirmPassword?: string;
}

export function ResetPassword() {
    const [searchParams] = useSearchParams();
    const code = searchParams.get('code');

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [validationMessages, setValidationMessages] = useState<ValidationMessages>({});
    const [message, setMessage] = useState<string | null>(null);

    const resetPassword = accountService.useResetPassword();
    const navigate = useNavigate();

    if (code == null) {
        return <Navigate to="/Account/InvalidPasswordReset" replace />;
    }

    const validate = (): boolean => {
        const messages: ValidationMessages = {};

        if (email.trim().length === 0) {
            messages.email = 'The Email field is required.';
        } else if (!/^[^@\s]+@[^@\s]+$/.test(email.trim())) {
            messages.email = 'The Email field is not a valid e-mail address.';
        }

        if (password.length === 0) {
            messages.password = 'The Password field is required.';
        } else if (password.length < 6 || password.length > 100) {
            messages.password = 'The Password must be at least 6 and at max 100 characters long.';
        }

        if (confirmPassword !== password) {
            messages.confirmPassword = 'The password and confirmation password do not match.';
        }

        setValidationMessages(messages);

        return Object.keys(messages).length === 0;
    };

    const onValidSubmit = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        if (!validate()) {
            return;
        }

        resetPassword.mutate({ email: email.trim(), code, password }, {
            onSuccess: () => navigate('/Account/ResetPasswordConfirmation'),
            onError: (error: unknown) => {
                setMessage(extractApiErrorMessage(
                    error, 'Error: We could not reset your password. Please try again.'));
            }
        });
    };

    const summaryMessages = Object.values(validationMessages)
        .filter((validationMessage): validationMessage is string => validationMessage != null);

    return (
        <>
            <h1>Reset password</h1>
            <h2>Reset your password.</h2>
            <hr />
            <div className="row">
                <div className="col-md-4">
                    <StatusMessage message={message} />
                    <form onSubmit={onValidSubmit} noValidate>
                        {summaryMessages.length > 0 && (
                            <ul className="text-danger" role="alert">
                                {summaryMessages.map((summaryMessage) =>
                                    <li key={summaryMessage}>{summaryMessage}</li>)}
                            </ul>
                        )}

                        <input type="hidden" name="Input.Code" value={code} readOnly />
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
                            {validationMessages.email != null && (
                                <div className="text-danger">{validationMessages.email}</div>
                            )}
                        </div>
                        <div className="form-floating mb-3">
                            <input
                                type="password"
                                value={password}
                                onChange={(event) => setPassword(event.target.value)}
                                id="Input.Password"
                                className="form-control"
                                autoComplete="new-password"
                                aria-required="true"
                                placeholder="Please enter your password." />
                            <label htmlFor="Input.Password" className="form-label">Password</label>
                            {validationMessages.password != null && (
                                <div className="text-danger">{validationMessages.password}</div>
                            )}
                        </div>
                        <div className="form-floating mb-3">
                            <input
                                type="password"
                                value={confirmPassword}
                                onChange={(event) => setConfirmPassword(event.target.value)}
                                id="Input.ConfirmPassword"
                                className="form-control"
                                autoComplete="new-password"
                                aria-required="true"
                                placeholder="Please confirm your password." />
                            <label htmlFor="Input.ConfirmPassword" className="form-label">Confirm password</label>
                            {validationMessages.confirmPassword != null && (
                                <div className="text-danger">{validationMessages.confirmPassword}</div>
                            )}
                        </div>
                        <button
                            type="submit"
                            className="w-100 btn btn-lg btn-primary"
                            disabled={resetPassword.isPending}>
                            Reset
                        </button>
                    </form>
                </div>
            </div>
        </>
    );
}
