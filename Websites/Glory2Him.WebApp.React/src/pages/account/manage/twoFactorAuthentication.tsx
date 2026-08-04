import { FormEvent, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { manageAccountService } from '../../../services/foundations/manageAccountService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';

// Ported from Blazor's Account/Pages/Manage/TwoFactorAuthentication.razor. Sibling manage
// pages pass their completion status message through router state (the SPA equivalent of
// Blazor's status-message cookie redirect).
export function TwoFactorAuthentication() {
    const location = useLocation();

    const [message, setMessage] = useState<string | null>(
        (location.state as { statusMessage?: string } | null)?.statusMessage ?? null);

    const twoFactorInfo = manageAccountService.useGetTwoFactorInfo();
    const forgetBrowser = manageAccountService.useForgetBrowser();

    if (twoFactorInfo.data == null) {
        return null;
    }

    const {
        canTrack,
        hasAuthenticator,
        is2faEnabled,
        isMachineRemembered,
        recoveryCodesLeft
    } = twoFactorInfo.data;

    const onSubmitForgetBrowser = (event: FormEvent) => {
        event.preventDefault();

        forgetBrowser.mutate(undefined, {
            onSuccess: () => {
                setMessage(
                    'The current browser has been forgotten. When you login again ' +
                    'from this browser you will be prompted for your 2fa code.');
            },
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'We could not forget this browser. Please try again.')}`);
            }
        });
    };

    return (
        <>
            <StatusMessage message={message} />
            <h3>Two-factor authentication (2FA)</h3>
            {canTrack ? (
                <>
                    {is2faEnabled && (
                        <>
                            {recoveryCodesLeft === 0 && (
                                <div className="alert alert-danger">
                                    <strong>You have no recovery codes left.</strong>
                                    <p>You must <Link to="/Account/Manage/GenerateRecoveryCodes">generate a new set of recovery codes</Link> before you can log in with a recovery code.</p>
                                </div>
                            )}
                            {recoveryCodesLeft === 1 && (
                                <div className="alert alert-danger">
                                    <strong>You have 1 recovery code left.</strong>
                                    <p>You can <Link to="/Account/Manage/GenerateRecoveryCodes">generate a new set of recovery codes</Link>.</p>
                                </div>
                            )}
                            {recoveryCodesLeft > 1 && recoveryCodesLeft <= 3 && (
                                <div className="alert alert-warning">
                                    <strong>You have {recoveryCodesLeft} recovery codes left.</strong>
                                    <p>You should <Link to="/Account/Manage/GenerateRecoveryCodes">generate a new set of recovery codes</Link>.</p>
                                </div>
                            )}

                            {isMachineRemembered && (
                                <form
                                    style={{ display: 'inline-block' }}
                                    onSubmit={onSubmitForgetBrowser}
                                    method="post">
                                    <button
                                        type="submit"
                                        className="btn btn-primary"
                                        disabled={forgetBrowser.isPending}>
                                        Forget this browser
                                    </button>
                                </form>
                            )}

                            <Link to="/Account/Manage/Disable2fa" className="btn btn-primary">Disable 2FA</Link>
                            <Link to="/Account/Manage/GenerateRecoveryCodes" className="btn btn-primary">Reset recovery codes</Link>
                        </>
                    )}

                    <h4>Authenticator app</h4>
                    {!hasAuthenticator ? (
                        <Link to="/Account/Manage/EnableAuthenticator" className="btn btn-primary">Add authenticator app</Link>
                    ) : (
                        <>
                            <Link to="/Account/Manage/EnableAuthenticator" className="btn btn-primary">Set up authenticator app</Link>
                            <Link to="/Account/Manage/ResetAuthenticator" className="btn btn-primary">Reset authenticator app</Link>
                        </>
                    )}
                </>
            ) : (
                <div className="alert alert-danger">
                    <strong>Privacy and cookie policy have not been accepted.</strong>
                    <p>You must accept the policy before you can enable two factor authentication.</p>
                </div>
            )}
        </>
    );
}
