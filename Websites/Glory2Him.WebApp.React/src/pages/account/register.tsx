import { FormEvent, useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { registrationService } from '../../services/foundations/registrationService';
import { extractApiErrorMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/Register.razor: real-time username availability +
// suggestions and a friendly "email already registered" hint. On success it redirects to the
// login page (the API does not auto-sign-in).
type AvailabilityStatus = 'None' | 'Checking' | 'Available' | 'Taken' | 'Prohibited';

const debounceDelayMilliseconds = 450;
const fallbackMinimumUsernameLength = 3;

// Mirrors UserNameRule on the server (design §18.3.1). The server is the authority — this only
// saves a round trip and lets the field go red as the "@" is typed. The wording is deliberately
// the reason rather than the rule, because "not allowed" reads as an arbitrary restriction.
const prohibitedUsernameCharacter = '@';

const prohibitedUsernameMessage =
    'A username may not contain "@". Your username is shown to other people wherever the site '
    + 'names who submitted or reviewed something, so an email address used as one becomes public.';

interface ValidationMessages {
    username?: string;
    email?: string;
    name?: string;
    surname?: string;
    preferredName?: string;
    password?: string;
    confirmPassword?: string;
}

export function Register() {
    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [name, setName] = useState('');
    const [surname, setSurname] = useState('');
    const [preferredName, setPreferredName] = useState('');
    const [dateOfBirth, setDateOfBirth] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');

    const [usernameStatus, setUsernameStatus] = useState<AvailabilityStatus>('None');
    const [suggestions, setSuggestions] = useState<string[]>([]);
    const [emailInUse, setEmailInUse] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);
    const [validationMessages, setValidationMessages] = useState<ValidationMessages>({});

    const checkUsername = registrationService.useCheckUsername();
    const checkEmailInUse = registrationService.useCheckEmailInUse();
    const getSuggestions = registrationService.useGetUsernameSuggestions();
    const register = registrationService.useRegister();
    const navigate = useNavigate();

    const usernameDebounce = useRef<number | undefined>(undefined);
    const emailDebounce = useRef<number | undefined>(undefined);
    const usernameCheckSequence = useRef(0);
    const emailCheckSequence = useRef(0);

    // Read the latest name fields inside debounced callbacks without re-arming the timers.
    const nameFields = useRef({ name, surname, preferredName });
    nameFields.current = { name, surname, preferredName };

    useEffect(() => () => {
        window.clearTimeout(usernameDebounce.current);
        window.clearTimeout(emailDebounce.current);
    }, []);

    const usernameInputClass =
        usernameStatus === 'Available' ? 'form-control is-valid'
            : usernameStatus === 'Taken' || usernameStatus === 'Prohibited' ? 'form-control is-invalid'
                : 'form-control';

    const loginWithEmailUrl = `/Account/Login?username=${encodeURIComponent(email)}`;

    const onUsernameInput = (value: string) => {
        setUsername(value);
        setSuggestions([]);
        setUsernameStatus('Checking');

        window.clearTimeout(usernameDebounce.current);
        const sequence = ++usernameCheckSequence.current;

        usernameDebounce.current = window.setTimeout(async () => {
            const candidate = value.trim();

            try {
                // Ahead of the length check, so typing an email address is refused for the right
                // reason rather than sitting at 'None' until it happens to be long enough.
                if (candidate.includes(prohibitedUsernameCharacter)) {
                    if (sequence === usernameCheckSequence.current) {
                        setUsernameStatus('Prohibited');
                    }

                    return;
                }

                if (candidate.length < fallbackMinimumUsernameLength) {
                    if (sequence === usernameCheckSequence.current) {
                        setUsernameStatus('None');
                    }

                    return;
                }

                const availability = await checkUsername.mutateAsync(candidate);

                if (sequence !== usernameCheckSequence.current) {
                    return;
                }

                if (availability.isProhibited) {
                    setUsernameStatus('Prohibited');
                } else if (availability.isTooShort) {
                    setUsernameStatus('None');
                } else if (availability.isAvailable) {
                    setUsernameStatus('Available');
                } else {
                    setUsernameStatus('Taken');

                    const suggested = await getSuggestions.mutateAsync({
                        name: nameFields.current.name,
                        surname: nameFields.current.surname,
                        preferredName: nameFields.current.preferredName,
                    });

                    if (sequence === usernameCheckSequence.current) {
                        setSuggestions(suggested);
                    }
                }
            } catch {
                if (sequence === usernameCheckSequence.current) {
                    setUsernameStatus('None');
                }
            }
        }, debounceDelayMilliseconds);
    };

    const onEmailInput = (value: string) => {
        setEmail(value);

        window.clearTimeout(emailDebounce.current);
        const sequence = ++emailCheckSequence.current;

        emailDebounce.current = window.setTimeout(async () => {
            const candidate = value.trim();

            try {
                const inUse = candidate.includes('@')
                    && await checkEmailInUse.mutateAsync(candidate);

                if (sequence === emailCheckSequence.current) {
                    setEmailInUse(inUse);
                }
            } catch {
                if (sequence === emailCheckSequence.current) {
                    setEmailInUse(false);
                }
            }
        }, debounceDelayMilliseconds);
    };

    const applySuggestion = (suggestion: string) => {
        setUsername(suggestion);
        setUsernameStatus('Available');
        setSuggestions([]);
    };

    const validate = (): boolean => {
        const messages: ValidationMessages = {};

        if (username.trim().length === 0) {
            messages.username = 'The Username field is required.';
        } else if (username.includes(prohibitedUsernameCharacter)) {
            messages.username = prohibitedUsernameMessage;
        } else if (username.trim().length < 3 || username.trim().length > 256) {
            messages.username =
                'The field Username must be a string with a minimum length of 3 and a maximum length of 256.';
        }

        if (email.trim().length === 0) {
            messages.email = 'The Email address field is required.';
        } else if (!/^[^@\s]+@[^@\s]+$/.test(email.trim())) {
            messages.email = 'The Email address field is not a valid e-mail address.';
        }

        if (name.trim().length === 0) {
            messages.name = 'The First name field is required.';
        } else if (name.length > 100) {
            messages.name =
                'The field First name must be a string with a minimum length of 1 and a maximum length of 100.';
        }

        if (surname.trim().length === 0) {
            messages.surname = 'The Surname field is required.';
        } else if (surname.length > 100) {
            messages.surname =
                'The field Surname must be a string with a minimum length of 1 and a maximum length of 100.';
        }

        if (preferredName.length > 100) {
            messages.preferredName =
                'The field Preferred name must be a string with a maximum length of 100.';
        }

        if (password.length === 0) {
            messages.password = 'The Password field is required.';
        } else if (password.length < 4 || password.length > 100) {
            messages.password = 'The Password must be at least 4 and at max 100 characters long.';
        }

        if (confirmPassword !== password) {
            messages.confirmPassword = 'The password and confirmation password do not match.';
        }

        setValidationMessages(messages);

        return Object.keys(messages).length === 0;
    };

    const registerUser = async (event: FormEvent) => {
        event.preventDefault();
        setErrorMessage(null);

        if (!validate()) {
            return;
        }

        setIsSubmitting(true);

        try {
            // Re-check on the server so a name taken between typing and submit is still caught.
            const availability = await checkUsername.mutateAsync(username.trim());

            if (availability.isProhibited) {
                setUsernameStatus('Prohibited');

                setErrorMessage(
                    availability.prohibitedReason ?? prohibitedUsernameMessage);

                return;
            }

            if (!availability.isAvailable) {
                setUsernameStatus('Taken');
                setErrorMessage('That username is already taken. Please choose another.');
                return;
            }

            if (await checkEmailInUse.mutateAsync(email.trim())) {
                setEmailInUse(true);
                setErrorMessage('An account with this email already exists. Please sign in instead.');
                return;
            }

            await register.mutateAsync({
                userName: username.trim(),
                email: email.trim(),
                name,
                surname,
                preferredName: preferredName.trim().length === 0 ? null : preferredName,
                dateOfBirth: dateOfBirth.length === 0 ? null : dateOfBirth,
                password,
            });

            navigate(
                `/Account/Login?registered=true&username=${encodeURIComponent(username.trim())}`);
        } catch (error) {
            setErrorMessage(
                extractApiErrorMessage(error, 'We could not create your account. Please try again.'));
        } finally {
            setIsSubmitting(false);
        }
    };

    const summaryMessages = Object.values(validationMessages)
        .filter((message): message is string => message != null);

    return (
        <section className="py-4 py-lg-5">
            <div className="container">
                <div className="row">
                    <div className="col-md-10 col-lg-8 col-xl-6 mx-auto">
                        <div className="bg-primary bg-opacity-10 rounded p-4 p-sm-5">
                            <h2>Create your free account</h2>

                            {errorMessage != null && (
                                <div className="alert alert-danger mt-3" role="alert">{errorMessage}</div>
                            )}

                            <form onSubmit={registerUser} className="mt-4" noValidate>
                                {summaryMessages.length > 0 && (
                                    <ul className="text-danger" role="alert">
                                        {summaryMessages.map((message) =>
                                            <li key={message}>{message}</li>)}
                                    </ul>
                                )}

                                {/* Username */}
                                <div className="mb-3">
                                    <label className="form-label" htmlFor="Input.Username">Username</label>
                                    <div className="position-relative">
                                        <input
                                            value={username}
                                            onChange={(event) => onUsernameInput(event.target.value)}
                                            id="Input.Username"
                                            className={usernameInputClass}
                                            autoComplete="username"
                                            aria-required="true"
                                            placeholder="Username" />
                                        <span className="position-absolute top-50 end-0 translate-middle-y me-3">
                                            {usernameStatus === 'Checking' && (
                                                <span className="spinner-border spinner-border-sm text-secondary"></span>
                                            )}
                                            {usernameStatus === 'Available' && (
                                                <i className="bi bi-check-circle-fill text-success"></i>
                                            )}
                                            {(usernameStatus === 'Taken' || usernameStatus === 'Prohibited') && (
                                                <i className="bi bi-x-circle-fill text-danger"></i>
                                            )}
                                        </span>
                                    </div>
                                    <small className="form-text">
                                        This is the name other people see, and the name you sign in
                                        with. It is not your email address — you can sign in with
                                        either.
                                    </small>
                                    {usernameStatus === 'Prohibited' && (
                                        <div className="text-danger small mt-1" role="alert">
                                            <i className="bi bi-exclamation-circle me-1"></i>
                                            {prohibitedUsernameMessage}
                                        </div>
                                    )}
                                    {usernameStatus === 'Available' && (
                                        <div className="text-success small mt-1">
                                            <i className="bi bi-check2 me-1"></i>{username} is available
                                        </div>
                                    )}
                                    {usernameStatus === 'Taken' && (
                                        <>
                                            <div className="text-danger small mt-1">
                                                <i className="bi bi-exclamation-circle me-1"></i>{username} is already taken
                                            </div>
                                            {suggestions.length > 0 && (
                                                <div className="mt-2 small">
                                                    <span className="text-body-secondary">Try one of these:</span>
                                                    {suggestions.map((suggestion) => (
                                                        <button
                                                            key={suggestion}
                                                            type="button"
                                                            className="btn btn-sm btn-outline-primary py-0 px-2 ms-1 mb-1"
                                                            onClick={() => applySuggestion(suggestion)}>
                                                            {suggestion}
                                                        </button>
                                                    ))}
                                                </div>
                                            )}
                                        </>
                                    )}
                                    {validationMessages.username != null && (
                                        <div className="text-danger">{validationMessages.username}</div>
                                    )}
                                </div>

                                {/* Email */}
                                <div className="mb-3">
                                    <label className="form-label" htmlFor="Input.Email">Email address</label>
                                    <input
                                        value={email}
                                        onChange={(event) => onEmailInput(event.target.value)}
                                        id="Input.Email"
                                        className="form-control"
                                        autoComplete="email"
                                        aria-required="true"
                                        placeholder="you@example.com" />
                                    {emailInUse && (
                                        <div className="text-danger small mt-1">
                                            <i className="bi bi-info-circle me-1"></i>
                                            An account with this email already exists.{' '}
                                            <Link to={loginWithEmailUrl}>Sign in instead</Link>.
                                        </div>
                                    )}
                                    {validationMessages.email != null && (
                                        <div className="text-danger">{validationMessages.email}</div>
                                    )}
                                </div>

                                {/* Name / Surname */}
                                <div className="row">
                                    <div className="col-md-6 mb-3">
                                        <label className="form-label" htmlFor="Input.Name">First name</label>
                                        <input
                                            value={name}
                                            onChange={(event) => setName(event.target.value)}
                                            id="Input.Name"
                                            className="form-control"
                                            autoComplete="given-name"
                                            aria-required="true"
                                            placeholder="First name" />
                                        {validationMessages.name != null && (
                                            <div className="text-danger">{validationMessages.name}</div>
                                        )}
                                    </div>
                                    <div className="col-md-6 mb-3">
                                        <label className="form-label" htmlFor="Input.Surname">Surname</label>
                                        <input
                                            value={surname}
                                            onChange={(event) => setSurname(event.target.value)}
                                            id="Input.Surname"
                                            className="form-control"
                                            autoComplete="family-name"
                                            aria-required="true"
                                            placeholder="Surname" />
                                        {validationMessages.surname != null && (
                                            <div className="text-danger">{validationMessages.surname}</div>
                                        )}
                                    </div>
                                </div>

                                {/* Preferred name / Date of birth (optional) */}
                                <div className="row">
                                    <div className="col-md-6 mb-3">
                                        <label className="form-label" htmlFor="Input.PreferredName">
                                            Preferred name <span className="text-body-secondary">(optional)</span>
                                        </label>
                                        <input
                                            value={preferredName}
                                            onChange={(event) => setPreferredName(event.target.value)}
                                            id="Input.PreferredName"
                                            className="form-control"
                                            placeholder="Preferred name" />
                                        {validationMessages.preferredName != null && (
                                            <div className="text-danger">{validationMessages.preferredName}</div>
                                        )}
                                    </div>
                                    <div className="col-md-6 mb-3">
                                        <label className="form-label" htmlFor="Input.DateOfBirth">
                                            Date of birth <span className="text-body-secondary">(optional)</span>
                                        </label>
                                        <input
                                            type="date"
                                            value={dateOfBirth}
                                            onChange={(event) => setDateOfBirth(event.target.value)}
                                            id="Input.DateOfBirth"
                                            className="form-control" />
                                    </div>
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
                                        autoComplete="new-password"
                                        aria-required="true"
                                        placeholder="*********" />
                                    {validationMessages.password != null && (
                                        <div className="text-danger">{validationMessages.password}</div>
                                    )}
                                </div>

                                {/* Confirm password */}
                                <div className="mb-3">
                                    <label className="form-label" htmlFor="Input.ConfirmPassword">Confirm Password</label>
                                    <input
                                        type="password"
                                        value={confirmPassword}
                                        onChange={(event) => setConfirmPassword(event.target.value)}
                                        id="Input.ConfirmPassword"
                                        className="form-control"
                                        autoComplete="new-password"
                                        aria-required="true"
                                        placeholder="*********" />
                                    {validationMessages.confirmPassword != null && (
                                        <div className="text-danger">{validationMessages.confirmPassword}</div>
                                    )}
                                </div>

                                {/* Button */}
                                <div className="row align-items-center">
                                    <div className="col-sm-4">
                                        <button type="submit" className="btn btn-success" disabled={isSubmitting}>
                                            Sign me up
                                        </button>
                                    </div>
                                    <div className="col-sm-8 text-sm-end">
                                        <span>
                                            Have an account? <Link to="/Account/Login"><u>Sign in</u></Link>
                                        </span>
                                    </div>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
