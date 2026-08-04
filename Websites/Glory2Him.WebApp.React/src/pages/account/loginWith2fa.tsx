import { FormEvent, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { accountService } from '../../services/foundations/accountService';
import { StatusMessage, extractApiErrorMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/LoginWith2fa.razor. The login page navigates here
// with ReturnUrl and RememberMe in the query string, exactly like Blazor did.
export function LoginWith2fa() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const returnUrl = searchParams.get('ReturnUrl') ?? searchParams.get('returnUrl');
    const rememberMe = searchParams.get('RememberMe') === 'true';

    const [twoFactorCode, setTwoFactorCode] = useState('');
    const [rememberMachine, setRememberMachine] = useState(false);
    const [validationMessage, setValidationMessage] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);

    const loginWith2fa = accountService.useLoginWith2fa();

    const recoveryCodeUrl = returnUrl != null
        ? `/Account/LoginWithRecoveryCode?ReturnUrl=${encodeURIComponent(returnUrl)}`
        : '/Account/LoginWithRecoveryCode';

    const validate = (): boolean => {
        if (twoFactorCode.length === 0) {
            setValidationMessage('The Authenticator code field is required.');
            return false;
        }

        if (twoFactorCode.length < 6 || twoFactorCode.length > 7) {
            setValidationMessage(
                'The Authenticator code must be at least 6 and at max 7 characters long.');

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

        loginWith2fa.mutate({ twoFactorCode, rememberMachine, rememberMe }, {
            onSuccess: (result) => {
                if (result.isLockedOut) {
                    navigate('/Account/Lockout');
                    return;
                }

                navigate(returnUrl != null && returnUrl.startsWith('/') ? returnUrl : '/');
            },
            onError: (error: unknown) => {
                setMessage(extractApiErrorMessage(
                    error, 'Error: Invalid authenticator code.'));
            }
        });
    };

    return (
        <>
            <h1>Two-factor authentication</h1>
                <hr />
                <StatusMessage message={message} />
                <p>Your login is protected with an authenticator app. Enter your authenticator code below.</p>
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
                                    value={twoFactorCode}
                                    onChange={(event) => setTwoFactorCode(event.target.value)}
                                    id="Input.TwoFactorCode"
                                    className="form-control"
                                    autoComplete="off" />
                                <label htmlFor="Input.TwoFactorCode" className="form-label">Authenticator code</label>
                                {validationMessage != null && (
                                    <div className="text-danger">{validationMessage}</div>
                                )}
                            </div>
                            <div className="checkbox mb-3">
                                <label htmlFor="remember-machine" className="form-label">
                                    <input
                                        type="checkbox"
                                        checked={rememberMachine}
                                        onChange={(event) =>
                                            setRememberMachine(event.target.checked)}
                                        id="remember-machine" />
                                    {' '}Remember this machine
                                </label>
                            </div>
                            <div>
                                <button
                                    type="submit"
                                    className="w-100 btn btn-lg btn-primary"
                                    disabled={loginWith2fa.isPending}>
                                    Log in
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
                <p>
                    Don&apos;t have access to your authenticator device? You can{' '}
                <Link to={recoveryCodeUrl}>log in with a recovery code</Link>.
            </p>
        </>
    );
}
