import { FormEvent, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { passkeyService } from '../../../services/foundations/passkeyService';
import { extractApiErrorMessage } from '../statusMessage';

// Ported from Blazor's Account/Pages/Manage/RenamePasskey.razor. The Blazor
// page redirected back to Passkeys with a status message; here the message
// travels through router location state.
export function RenamePasskey() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [name, setName] = useState('');
    const [validationMessage, setValidationMessage] = useState<string | null>(null);

    const getPasskeys = passkeyService.useGetPasskeys();
    const renamePasskey = passkeyService.useRenamePasskey();

    const passkey = getPasskeys.data?.find(
        (currentPasskey) => currentPasskey.credentialId === id);

    const navigateToPasskeys = (statusMessage: string) => {
        navigate('/Account/Manage/Passkeys', { state: { statusMessage } });
    };

    const validate = (): boolean => {
        if (name.trim().length === 0) {
            setValidationMessage('The Name field is required.');
            return false;
        }

        if (name.length > 200) {
            setValidationMessage('Passkey names must be no longer than 200 characters.');
            return false;
        }

        setValidationMessage(null);

        return true;
    };

    const onValidSubmit = (event: FormEvent) => {
        event.preventDefault();

        if (id == null || !validate()) {
            return;
        }

        renamePasskey.mutate({ credentialId: id, name }, {
            onSuccess: () => {
                navigateToPasskeys('Passkey updated successfully.');
            },
            onError: (error: unknown) => {
                navigateToPasskeys(`Error: ${extractApiErrorMessage(
                    error, 'The passkey could not be updated.')}`);
            }
        });
    };

    return (
        <form onSubmit={onValidSubmit} noValidate>
            {passkey?.name != null ? (
                <h4>Enter a new name for your &quot;{passkey.name}&quot; passkey</h4>
            ) : (
                <h4>Enter a name for your passkey</h4>
            )}
            <hr />
            {validationMessage != null && (
                <ul className="text-danger" role="alert">
                    <li>{validationMessage}</li>
                </ul>
            )}
            <div className="form-floating mb-3">
                <input
                    value={name}
                    onChange={(event) => setName(event.target.value)}
                    id="Input.Name"
                    className="form-control"
                    aria-required="true"
                    placeholder="My passkey" />
                <label htmlFor="Input.Name" className="form-label">Passkey name</label>
                {validationMessage != null && (
                    <div className="text-danger">{validationMessage}</div>
                )}
            </div>
            <div>
                <button
                    type="submit"
                    className="w-100 btn btn-lg btn-primary"
                    disabled={renamePasskey.isPending}>
                    Continue
                </button>
            </div>
        </form>
    );
}
