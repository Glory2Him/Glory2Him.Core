import { useState } from 'react';
import { passkeyService } from '../../../services/foundations/passkeyService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';

// Ported from Blazor's Account/Pages/Manage/ExternalLogins.razor. With no
// external providers configured (this demo's state) the page renders nothing
// but the status message, exactly as the Blazor page did. The "Add another
// service" form posts to the Blazor-era challenge endpoint
// /Account/Manage/LinkExternalLogin because an OAuth challenge must be a
// top-level navigation, not an XHR; see the note on that form below.
export function ExternalLogins() {
    const [message, setMessage] = useState<string | null>(null);

    const getExternalLogins = passkeyService.useGetExternalLogins();
    const removeExternalLogin = passkeyService.useRemoveExternalLogin();

    const currentLogins = getExternalLogins.data?.currentLogins ?? [];
    const otherLogins = getExternalLogins.data?.otherLogins ?? [];
    const showRemoveButton = getExternalLogins.data?.showRemoveButton ?? false;

    const onRemoveLogin = (loginProvider: string, providerKey: string) => {
        setMessage(null);

        removeExternalLogin.mutate({ loginProvider, providerKey }, {
            onSuccess: () => {
                setMessage('The external login was removed.');
            },
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'The external login was not removed.')}`);
            }
        });
    };

    return (
        <>
            <StatusMessage message={message} />
            {currentLogins.length > 0 && (
                <>
                    <h3>Registered Logins</h3>
                    <table className="table">
                        <tbody>
                            {currentLogins.map((login) => (
                                <tr key={login.loginProvider}>
                                    <td>{login.providerDisplayName}</td>
                                    <td>
                                        {showRemoveButton ? (
                                            <div>
                                                <button
                                                    type="button"
                                                    className="btn btn-primary"
                                                    title={`Remove this ${login.providerDisplayName} login from your account`}
                                                    disabled={removeExternalLogin.isPending}
                                                    onClick={() => onRemoveLogin(
                                                        login.loginProvider, login.providerKey)}>
                                                    Remove
                                                </button>
                                            </div>
                                        ) : (
                                            <>&nbsp;</>
                                        )}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </>
            )}
            {otherLogins.length > 0 && (
                <>
                    <h4>Add another service to log in.</h4>
                    <hr />
                    {/* Constraint: linking a provider is an OAuth challenge (a
                        redirect), so this posts the Blazor form endpoint. That
                        endpoint validates an antiforgery token the SPA does not
                        have, so this path only becomes actionable once a real
                        provider is configured and the challenge endpoint is
                        exposed without the Razor-form antiforgery coupling. */}
                    <form
                        className="form-horizontal"
                        action="/Account/Manage/LinkExternalLogin"
                        method="post">
                        <div>
                            <p>
                                {otherLogins.map((provider) => (
                                    <button
                                        key={provider.name}
                                        type="submit"
                                        className="btn btn-primary"
                                        name="Provider"
                                        value={provider.name}
                                        title={`Log in using your ${provider.displayName} account`}>
                                        {provider.displayName}
                                    </button>
                                ))}
                            </p>
                        </div>
                    </form>
                </>
            )}
        </>
    );
}
