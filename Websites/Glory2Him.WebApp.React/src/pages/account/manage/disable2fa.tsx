import { FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { manageAccountService } from '../../../services/foundations/manageAccountService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';

// Ported from Blazor's Account/Pages/Manage/Disable2fa.razor.
export function Disable2fa() {
    const navigate = useNavigate();
    const [message, setMessage] = useState<string | null>(null);

    const disable2fa = manageAccountService.useDisable2fa();

    const onSubmit = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        disable2fa.mutate(undefined, {
            onSuccess: () => {
                navigate('/Account/Manage/TwoFactorAuthentication', {
                    state: {
                        statusMessage:
                            '2fa has been disabled. You can reenable 2fa when you ' +
                            'setup an authenticator app'
                    }
                });
            },
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'Unexpected error occurred disabling 2FA.')}`);
            }
        });
    };

    return (
        <>
            <StatusMessage message={message} />
            <h3>Disable two-factor authentication (2FA)</h3>

            <div className="alert alert-warning" role="alert">
                <p>
                    <strong>This action only disables 2FA.</strong>
                </p>
                <p>
                    Disabling 2FA does not change the keys used in authenticator apps. If you wish to change the key
                    used in an authenticator app you should <Link to="/Account/Manage/ResetAuthenticator">reset your authenticator keys.</Link>
                </p>
            </div>

            <div>
                <form onSubmit={onSubmit} method="post">
                    <button
                        className="btn btn-danger"
                        type="submit"
                        disabled={disable2fa.isPending}>
                        Disable 2FA
                    </button>
                </form>
            </div>
        </>
    );
}
