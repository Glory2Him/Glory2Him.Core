import { FormEvent, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { manageAccountService } from '../../../services/foundations/manageAccountService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';
import { ShowRecoveryCodes } from './showRecoveryCodes';

// Ported from Blazor's Account/Pages/Manage/EnableAuthenticator.razor. The QR code SVG is
// generated server-side by the same QRCoder call the Blazor page used and inlined here, so
// the rendered QR image is identical.
export function EnableAuthenticator() {
    const location = useLocation();
    const navigate = useNavigate();

    const [code, setCode] = useState('');
    const [validationMessage, setValidationMessage] = useState<string | null>(null);
    const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null);

    const [message, setMessage] = useState<string | null>(
        (location.state as { statusMessage?: string } | null)?.statusMessage ?? null);

    const authenticatorSetup = manageAccountService.useGetAuthenticatorSetup();
    const qrCode = manageAccountService.useGetAuthenticatorQrCode();
    const verifyAuthenticator = manageAccountService.useVerifyAuthenticator();

    const sharedKey = authenticatorSetup.data?.sharedKey ?? null;
    const qrCodeSvg = qrCode.data ?? null;

    const validate = (): boolean => {
        if (code.length === 0) {
            setValidationMessage('The Verification Code field is required.');
            return false;
        }

        if (code.length < 6 || code.length > 7) {
            setValidationMessage(
                'The Verification Code must be at least 6 and at max 7 characters long.');

            return false;
        }

        setValidationMessage(null);

        return true;
    };

    const onValidSubmit = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        if (!validate()) {
            return;
        }

        verifyAuthenticator.mutate(code, {
            onSuccess: (result) => {
                if (result.recoveryCodes != null) {
                    setMessage(result.message);
                    setRecoveryCodes(result.recoveryCodes);
                } else {
                    navigate('/Account/Manage/TwoFactorAuthentication', {
                        state: { statusMessage: result.message }
                    });
                }
            },
            onError: (error: unknown) => {
                setMessage(extractApiErrorMessage(
                    error, 'Error: Verification code is invalid.'));
            }
        });
    };

    if (recoveryCodes != null) {
        return <ShowRecoveryCodes recoveryCodes={recoveryCodes} statusMessage={message} />;
    }

    return (
        <>
            <StatusMessage message={message} />
            <h3>Configure authenticator app</h3>
            <div>
                <p>To use an authenticator app go through the following steps:</p>
                <ol className="list">
                    <li>
                        <p>
                            Download a two-factor authenticator app like Microsoft Authenticator for{' '}
                            <a href="https://go.microsoft.com/fwlink/?Linkid=825072">Android</a> and{' '}
                            <a href="https://go.microsoft.com/fwlink/?Linkid=825073">iOS</a> or
                            Google Authenticator for{' '}
                            <a href="https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2&hl=en">Android</a> and{' '}
                            <a href="https://itunes.apple.com/us/app/google-authenticator/id388497605?mt=8">iOS</a>.
                        </p>
                    </li>
                    <li>
                        <p>Scan the QR Code or enter this key <kbd>{sharedKey}</kbd> into your two factor authenticator app. Spaces and casing do not matter.</p>
                        {qrCodeSvg != null && (
                            <div
                                className="my-3"
                                dangerouslySetInnerHTML={{ __html: qrCodeSvg }} />
                        )}
                    </li>
                    <li>
                        <p>
                            Once you have scanned the QR code or input the key above, your two factor authentication app will provide you
                            with a unique code. Enter the code in the confirmation box below.
                        </p>
                        <div className="row">
                            <div className="col-xl-6">
                                <form onSubmit={onValidSubmit} noValidate>
                                    <div className="form-floating mb-3">
                                        <input
                                            value={code}
                                            onChange={(event) => setCode(event.target.value)}
                                            id="Input.Code"
                                            className="form-control"
                                            autoComplete="off"
                                            placeholder="Enter the code" />
                                        <label htmlFor="Input.Code" className="control-label form-label">Verification Code</label>
                                        {validationMessage != null && (
                                            <div className="text-danger">{validationMessage}</div>
                                        )}
                                    </div>
                                    <button
                                        type="submit"
                                        className="w-100 btn btn-lg btn-primary"
                                        disabled={verifyAuthenticator.isPending}>
                                        Verify
                                    </button>
                                    {validationMessage != null && (
                                        <ul className="text-danger" role="alert">
                                            <li>{validationMessage}</li>
                                        </ul>
                                    )}
                                </form>
                            </div>
                        </div>
                    </li>
                </ol>
            </div>
        </>
    );
}
