import { FormEvent, useEffect, useState } from 'react';
import { manageAccountService } from '../../../services/foundations/manageAccountService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';

// Ported from Blazor's Account/Pages/Manage/Email.razor.
export function Email() {
    const [newEmail, setNewEmail] = useState('');
    const [newEmailTouched, setNewEmailTouched] = useState(false);
    const [validationMessage, setValidationMessage] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);

    const emailInfo = manageAccountService.useGetEmailInfo();
    const changeEmail = manageAccountService.useChangeEmail();
    const sendVerificationEmail = manageAccountService.useSendVerificationEmail();

    const email = emailInfo.data?.email ?? null;
    const isEmailConfirmed = emailInfo.data?.isEmailConfirmed ?? false;

    // The Blazor page prefills the "New email" input with the current address.
    useEffect(() => {
        if (!newEmailTouched && email != null) {
            setNewEmail(email);
        }
    }, [email, newEmailTouched]);

    const validate = (): boolean => {
        if (newEmail.trim().length === 0) {
            setValidationMessage('The New email field is required.');
            return false;
        }

        if (!/^[^@\s]+@[^@\s]+$/.test(newEmail.trim())) {
            setValidationMessage('The New email field is not a valid e-mail address.');
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

        if (newEmail === email) {
            setMessage('Your email is unchanged.');
            return;
        }

        changeEmail.mutate(newEmail, {
            onSuccess: (result) => setMessage(result.message),
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'We could not change your email. Please try again.')}`);
            }
        });
    };

    const onSendEmailVerification = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        if (email == null) {
            return;
        }

        sendVerificationEmail.mutate(undefined, {
            onSuccess: (result) => setMessage(result.message),
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'We could not send the verification email. Please try again.')}`);
            }
        });
    };

    return (
        <>
            <h3>Manage email</h3>

            <StatusMessage message={message} />
            <div className="row">
                <div className="col-xl-6">
                    <form
                        onSubmit={onSendEmailVerification}
                        id="send-verification-form"
                        method="post" />
                    <form onSubmit={onValidSubmit} noValidate>
                        {validationMessage != null && (
                            <ul className="text-danger" role="alert">
                                <li>{validationMessage}</li>
                            </ul>
                        )}
                        {isEmailConfirmed ? (
                            <div className="form-floating mb-3 input-group">
                                <input
                                    type="text"
                                    value={email ?? ''}
                                    id="email"
                                    className="form-control"
                                    placeholder="Enter your email"
                                    disabled
                                    readOnly />
                                <div className="input-group-append">
                                    <span className="h-100 input-group-text text-success font-weight-bold">✓</span>
                                </div>
                                <label htmlFor="email" className="form-label">Email</label>
                            </div>
                        ) : (
                            <div className="form-floating mb-3">
                                <input
                                    type="text"
                                    value={email ?? ''}
                                    id="email"
                                    className="form-control"
                                    placeholder="Enter your email"
                                    disabled
                                    readOnly />
                                <label htmlFor="email" className="form-label">Email</label>
                                <button
                                    type="submit"
                                    className="btn btn-link"
                                    form="send-verification-form"
                                    disabled={sendVerificationEmail.isPending}>
                                    Send verification email
                                </button>
                            </div>
                        )}
                        <div className="form-floating mb-3">
                            <input
                                value={newEmail}
                                onChange={(event) => {
                                    setNewEmailTouched(true);
                                    setNewEmail(event.target.value);
                                }}
                                id="Input.NewEmail"
                                className="form-control"
                                autoComplete="email"
                                aria-required="true"
                                placeholder="Enter a new email" />
                            <label htmlFor="Input.NewEmail" className="form-label">New email</label>
                            {validationMessage != null && (
                                <div className="text-danger">{validationMessage}</div>
                            )}
                        </div>
                        <button
                            type="submit"
                            className="w-100 btn btn-lg btn-primary"
                            disabled={changeEmail.isPending}>
                            Change email
                        </button>
                    </form>
                </div>
            </div>
        </>
    );
}
