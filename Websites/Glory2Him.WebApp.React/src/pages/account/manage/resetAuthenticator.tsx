import { FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { manageAccountService } from '../../../services/foundations/manageAccountService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';

// Ported from Blazor's Account/Pages/Manage/ResetAuthenticator.razor.
export function ResetAuthenticator() {
    const navigate = useNavigate();
    const [message, setMessage] = useState<string | null>(null);

    const resetAuthenticator = manageAccountService.useResetAuthenticator();

    const onSubmit = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        resetAuthenticator.mutate(undefined, {
            onSuccess: () => {
                navigate('/Account/Manage/EnableAuthenticator', {
                    state: {
                        statusMessage:
                            'Your authenticator app key has been reset, you will need ' +
                            'to configure your authenticator app using the new key.'
                    }
                });
            },
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'We could not reset your authenticator key. Please try again.')}`);
            }
        });
    };

    return (
        <>
            <StatusMessage message={message} />
            <h3>Reset authenticator key</h3>
            <div className="alert alert-warning" role="alert">
                <p>
                    <span className="glyphicon glyphicon-warning-sign"></span>
                    <strong>If you reset your authenticator key your authenticator app will not work until you reconfigure it.</strong>
                </p>
                <p>
                    This process disables 2FA until you verify your authenticator app.
                    If you do not complete your authenticator app configuration you may lose access to your account.
                </p>
            </div>
            <div>
                <form onSubmit={onSubmit} method="post">
                    <button
                        className="btn btn-danger"
                        type="submit"
                        disabled={resetAuthenticator.isPending}>
                        Reset authenticator key
                    </button>
                </form>
            </div>
        </>
    );
}
