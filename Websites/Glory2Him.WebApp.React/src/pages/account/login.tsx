import { FormEvent, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { accountService } from '../../services/foundations/accountService';
import { StatusMessage, extractApiErrorMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/Login.razor. Passkey sign-in, external login providers
// and "resend email confirmation" have no API endpoints yet, so those blocks are omitted.
export function Login() {
    const [searchParams] = useSearchParams();
    const returnUrl = searchParams.get('returnUrl') ?? searchParams.get('ReturnUrl');
    const registered = searchParams.get('registered') === 'true';

    // Prefill the identifier when arriving from sign-up or a "sign in with email" link.
    const [email, setEmail] = useState(searchParams.get('username') ?? '');
    const [password, setPassword] = useState('');
    const [rememberMe, setRememberMe] = useState(false);
    const [validationMessages, setValidationMessages] =
        useState<{ email?: string; password?: string }>({});
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const login = accountService.useLogin();
    const navigate = useNavigate();

    const registerUrl = returnUrl != null
        ? `/Account/Register?ReturnUrl=${encodeURIComponent(returnUrl)}`
        : '/Account/Register';

    const validate = (): boolean => {
        const messages: { email?: string; password?: string } = {};

        if (email.trim().length === 0) {
            messages.email = 'The Username or email field is required.';
        }

        if (password.length === 0) {
            messages.password = 'The Password field is required.';
        }

        setValidationMessages(messages);

        return Object.keys(messages).length === 0;
    };

    const loginUser = (event: FormEvent) => {
        event.preventDefault();
        setErrorMessage(null);

        if (!validate()) {
            return;
        }

        login.mutate({ userName: email.trim(), password, rememberMe }, {
            onSuccess: () => {
                navigate(returnUrl != null && returnUrl.startsWith('/') ? returnUrl : '/');
            },
            onError: (error: unknown) => {
                setErrorMessage(extractApiErrorMessage(error, 'Error: Invalid login attempt.'));
            }
        });
    };

    const summaryMessages =
        [validationMessages.email, validationMessages.password]
            .filter((message): message is string => message != null);

    return (
        <section className="py-4 py-lg-5">
            <div className="container">
                <div className="row">
                    <div className="col-md-10 col-lg-8 col-xl-6 mx-auto">
                        <div className="p-4 p-sm-5 bg-primary bg-opacity-10 rounded">
                            <h2>Log in to your account</h2>

                            {registered && (
                                <div className="alert alert-success mt-3" role="alert">
                                    Your account has been created. Please sign in.
                                </div>
                            )}

                            <StatusMessage message={errorMessage} />

                            <form onSubmit={loginUser} className="mt-4" noValidate>
                                {summaryMessages.length > 0 && (
                                    <ul className="text-danger" role="alert">
                                        {summaryMessages.map((message) =>
                                            <li key={message}>{message}</li>)}
                                    </ul>
                                )}

                                {/* Username or email */}
                                <div className="mb-3">
                                    <label className="form-label" htmlFor="Input.Email">Username or email</label>
                                    <input
                                        value={email}
                                        onChange={(event) => setEmail(event.target.value)}
                                        id="Input.Email"
                                        className="form-control"
                                        autoComplete="username webauthn"
                                        aria-required="true"
                                        placeholder="Username or email" />
                                    {validationMessages.email != null && (
                                        <div className="text-danger">{validationMessages.email}</div>
                                    )}
                                </div>

                                {/* Password */}
                                <div className="mb-3">
                                    <label className="form-label" htmlFor="Input.Password">Password</label>
                                    <input
                                        type="password"
                                        value={password}
                                        onChange={(event) => setPassword(event.target.value)}
                                        id="Input.Password"
                                        className="form-control"
                                        autoComplete="current-password"
                                        aria-required="true"
                                        placeholder="*********" />
                                    {validationMessages.password != null && (
                                        <div className="text-danger">{validationMessages.password}</div>
                                    )}
                                </div>

                                {/* Checkbox */}
                                <div className="mb-3 form-check">
                                    <input
                                        type="checkbox"
                                        checked={rememberMe}
                                        onChange={(event) => setRememberMe(event.target.checked)}
                                        id="Input.RememberMe"
                                        className="form-check-input" />
                                    <label className="form-check-label" htmlFor="Input.RememberMe">Keep me signed in</label>
                                </div>

                                {/* Button */}
                                <div className="row align-items-center">
                                    <div className="col-sm-4">
                                        <button type="submit" className="btn btn-success" disabled={login.isPending}>
                                            Sign me in
                                        </button>
                                    </div>
                                    <div className="col-sm-8 text-sm-end">
                                        <span>
                                            Don&apos;t have an account?{' '}
                                            <Link to={registerUrl}><u>Sign up</u></Link>
                                        </span>
                                    </div>
                                </div>
                            </form>

                            <hr />

                            <div className="text-center">
                                <p className="mb-2"><Link to="/Account/ForgotPassword">Forgot your password?</Link></p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
