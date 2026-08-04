import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { PasskeyCeremonyError } from '../../../hooks/usePasskeys';
import { passkeyService } from '../../../services/foundations/passkeyService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';

const maxPasskeyCount = 100;

// Ported from Blazor's Account/Pages/Manage/Passkeys.razor. The WebAuthn
// create ceremony that PasskeySubmit.razor.js ran now lives in usePasskeys /
// passkeyService; the status-message-through-redirect pattern becomes router
// location state.
export function Passkeys() {
    const location = useLocation();
    const navigate = useNavigate();

    const [message, setMessage] = useState<string | null>(
        (location.state as { statusMessage?: string } | null)?.statusMessage ?? null);

    const getPasskeys = passkeyService.useGetPasskeys();
    const addPasskey = passkeyService.useAddPasskey();
    const deletePasskey = passkeyService.useDeletePasskey();

    const currentPasskeys = getPasskeys.data ?? [];

    const onAddPasskey = () => {
        setMessage(null);

        addPasskey.mutate(undefined, {
            onSuccess: (credentialId: string) => {
                // Immediately prompt the user to enter a name for the credential
                navigate(`/Account/Manage/RenamePasskey/${encodeURIComponent(credentialId)}`);
            },
            onError: (error: unknown) => {
                if (error instanceof PasskeyCeremonyError) {
                    // An empty message means the user canceled the ceremony.
                    if (error.message.length > 0) {
                        setMessage(`Error: ${error.message}`);
                    }

                    return;
                }

                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'The passkey could not be added to your account.')}`);
            }
        });
    };

    const onRenamePasskey = (credentialId: string) => {
        navigate(`/Account/Manage/RenamePasskey/${encodeURIComponent(credentialId)}`);
    };

    const onDeletePasskey = (credentialId: string) => {
        setMessage(null);

        deletePasskey.mutate(credentialId, {
            onSuccess: () => {
                setMessage('Passkey deleted successfully.');
            },
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'The passkey could not be deleted.')}`);
            }
        });
    };

    return (
        <>
            <h3>Manage your passkeys</h3>

            <StatusMessage message={message} />

            {currentPasskeys.length > 0 ? (
                <table className="table">
                    <tbody>
                        {currentPasskeys.map((passkey) => (
                            <tr key={passkey.credentialId}>
                                <td>{passkey.name ?? 'Unnamed passkey'}</td>
                                <td>
                                    <div>
                                        <button
                                            type="button"
                                            className="btn btn-primary"
                                            title="Rename this passkey"
                                            onClick={() => onRenamePasskey(passkey.credentialId)}>
                                            Rename
                                        </button>{' '}
                                        <button
                                            type="button"
                                            className="btn btn-danger"
                                            title="Remove this passkey from your account"
                                            disabled={deletePasskey.isPending}
                                            onClick={() => onDeletePasskey(passkey.credentialId)}>
                                            Delete
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            ) : (
                <p>No passkeys are registered.</p>
            )}

            {currentPasskeys.length >= maxPasskeyCount ? (
                <p className="text-danger">
                    You have reached the maximum number of allowed passkeys.
                    Please delete one before adding a new one.
                </p>
            ) : (
                <button
                    type="button"
                    className="btn btn-primary"
                    disabled={addPasskey.isPending}
                    onClick={onAddPasskey}>
                    Add a new passkey
                </button>
            )}
        </>
    );
}
