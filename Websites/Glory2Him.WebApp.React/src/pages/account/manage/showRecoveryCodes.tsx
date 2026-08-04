import { StatusMessage } from '../statusMessage';

// Ported from Blazor's Account/Shared/ShowRecoveryCodes.razor.
export interface ShowRecoveryCodesProps {
    recoveryCodes: string[];
    statusMessage?: string | null;
}

export function ShowRecoveryCodes({ recoveryCodes, statusMessage }: ShowRecoveryCodesProps) {
    return (
        <>
            <StatusMessage message={statusMessage} />
            <h3>Recovery codes</h3>
            <div className="alert alert-warning" role="alert">
                <p>
                    <strong>Put these codes in a safe place.</strong>
                </p>
                <p>
                    If you lose your device and don&apos;t have the recovery codes you will lose access to your account.
                </p>
            </div>
            <div className="row">
                <div className="col-md-12">
                    {recoveryCodes.map((recoveryCode) => (
                        <div key={recoveryCode}>
                            <code className="recovery-code">{recoveryCode}</code>
                        </div>
                    ))}
                </div>
            </div>
        </>
    );
}
