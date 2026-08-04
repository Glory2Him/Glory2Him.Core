import { FormEvent, useState } from 'react';
import { Link } from 'react-router-dom';
import { manageAccountService } from '../../../services/foundations/manageAccountService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';
import { ShowRecoveryCodes } from './showRecoveryCodes';

// Ported from Blazor's Account/Pages/Manage/GenerateRecoveryCodes.razor.
export function GenerateRecoveryCodes() {
    const [message, setMessage] = useState<string | null>(null);
    const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null);

    const generateRecoveryCodes = manageAccountService.useGenerateRecoveryCodes();

    const onSubmit = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        generateRecoveryCodes.mutate(undefined, {
            onSuccess: (result) => {
                setRecoveryCodes(result.recoveryCodes);
                setMessage(result.message);
            },
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'We could not generate recovery codes. Please try again.')}`);
            }
        });
    };

    if (recoveryCodes != null) {
        return <ShowRecoveryCodes recoveryCodes={recoveryCodes} statusMessage={message} />;
    }

    return (
        <>
            <StatusMessage message={message} />
            <h3>Generate two-factor authentication (2FA) recovery codes</h3>
            <div className="alert alert-warning" role="alert">
                <p>
                    <span className="glyphicon glyphicon-warning-sign"></span>
                    <strong>Put these codes in a safe place.</strong>
                </p>
                <p>
                    If you lose your device and don&apos;t have the recovery codes you will lose access to your account.
                </p>
                <p>
                    Generating new recovery codes does not change the keys used in authenticator apps. If you wish to change the key
                    used in an authenticator app you should <Link to="/Account/Manage/ResetAuthenticator">reset your authenticator keys.</Link>
                </p>
            </div>
            <div>
                <form onSubmit={onSubmit} method="post">
                    <button
                        className="btn btn-danger"
                        type="submit"
                        disabled={generateRecoveryCodes.isPending}>
                        Generate Recovery Codes
                    </button>
                </form>
            </div>
        </>
    );
}
