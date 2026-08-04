import { useNavigate } from 'react-router-dom';
import { PasskeyCeremonyError } from '../../hooks/usePasskeys';
import { passkeyService } from '../../services/foundations/passkeyService';
import { extractApiErrorMessage } from './statusMessage';

// Ported from Blazor's PasskeySubmit (Operation="Request") usage on the Login
// page: runs request-options → WebAuthn get ceremony → passkey login, then
// navigates like the password login does. The host page (login.tsx) supplies
// the typed identifier and receives error messages for its StatusMessage.
export interface PasskeySignInButtonProps {
    email: string;
    returnUrl?: string | null;
    onError: (message: string) => void;
}

export function PasskeySignInButton({ email, returnUrl, onError }: PasskeySignInButtonProps) {
    const passkeySignIn = passkeyService.usePasskeySignIn();
    const navigate = useNavigate();

    const onSignInWithPasskey = () => {
        passkeySignIn.mutate(email, {
            onSuccess: () => {
                navigate(returnUrl != null && returnUrl.startsWith('/') ? returnUrl : '/');
            },
            onError: (error: unknown) => {
                if (error instanceof PasskeyCeremonyError) {
                    // An empty message means the user canceled the ceremony.
                    if (error.message.length > 0) {
                        onError(`Error: ${error.message}`);
                    }

                    return;
                }

                onError(extractApiErrorMessage(error, 'Error: Invalid login attempt.'));
            }
        });
    };

    return (
        <div className="d-flex flex-column text-center">
            <button
                type="button"
                className="btn btn-outline-primary mx-auto"
                disabled={passkeySignIn.isPending}
                onClick={onSignInWithPasskey}>
                <i className="bi bi-key me-2"></i>Log in with a passkey
            </button>
        </div>
    );
}
