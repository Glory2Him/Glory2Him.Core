import { FormEvent, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { accountService } from '../../services/foundations/accountService';
import { StatusMessage, extractApiErrorMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/LoginWithRecoveryCode.razor.
export function LoginWithRecoveryCode() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const returnUrl = searchParams.get('ReturnUrl') ?? searchParams.get('returnUrl');

    const [recoveryCode, setRecoveryCode] = useState('');
    const [validationMessage, setValidationMessage] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);

    const loginWithRecoveryCode = accountService.useLoginWithRecoveryCode();

    const validate = (): boolean => {
        if (recoveryCode.length === 0) {
            setValidationMessage('The Recovery Code field is required.');
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

        loginWithRecoveryCode.mutate(recoveryCode, {
            onSuccess: (result) => {
                if (result.isLockedOut) {
                    navigate('/Account/Lockout');
                    return;
                }

                navigate(returnUrl != null && returnUrl.startsWith('/') ? returnUrl : '/');
            },
            onError: (error: unknown) => {
                setMessage(extractApiErrorMessage(
                    error, 'Error: Invalid recovery code entered.'));
            }
        });
    };

    return (
        <>
            <h1>Recovery code verification</h1>
                <hr />
                <StatusMessage message={message} />
                <p>
                    You have requested to log in with a recovery code. This login will not be remembered until you provide
                    an authenticator app code at log in or disable 2FA and log in again.
                </p>
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
                                    value={recoveryCode}
                                    onChange={(event) => setRecoveryCode(event.target.value)}
                                    id="Input.RecoveryCode"
                                    className="form-control"
                                    autoComplete="off"
                                    placeholder="RecoveryCode" />
                                <label htmlFor="Input.RecoveryCode" className="form-label">Recovery Code</label>
                                {validationMessage != null && (
                                    <div className="text-danger">{validationMessage}</div>
                                )}
                            </div>
                            <button
                                type="submit"
                                className="w-100 btn btn-lg btn-primary"
                                disabled={loginWithRecoveryCode.isPending}>
                                Log in
                            </button>
                        </form>
                </div>
            </div>
        </>
    );
}
