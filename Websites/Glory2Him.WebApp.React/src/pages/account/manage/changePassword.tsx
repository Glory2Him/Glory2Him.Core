import { FormEvent, useState } from 'react';
import { accountService } from '../../../services/foundations/accountService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';

// Ported from Blazor's Account/Pages/Manage/ChangePassword.razor.
interface ValidationMessages {
    oldPassword?: string;
    newPassword?: string;
    confirmPassword?: string;
}

export function ChangePassword() {
    const [oldPassword, setOldPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [validationMessages, setValidationMessages] = useState<ValidationMessages>({});
    const [message, setMessage] = useState<string | null>(null);

    const changePassword = accountService.useChangePassword();

    const validate = (): boolean => {
        const messages: ValidationMessages = {};

        if (oldPassword.length === 0) {
            messages.oldPassword = 'The Current password field is required.';
        }

        if (newPassword.length === 0) {
            messages.newPassword = 'The New password field is required.';
        } else if (newPassword.length < 6 || newPassword.length > 100) {
            messages.newPassword = 'The New password must be at least 6 and at max 100 characters long.';
        }

        if (confirmPassword !== newPassword) {
            messages.confirmPassword = 'The new password and confirmation password do not match.';
        }

        setValidationMessages(messages);

        return Object.keys(messages).length === 0;
    };

    const onValidSubmit = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        if (!validate()) {
            return;
        }

        changePassword.mutate({ oldPassword, newPassword }, {
            onSuccess: () => {
                setMessage('Your password has been changed');
                setOldPassword('');
                setNewPassword('');
                setConfirmPassword('');
            },
            onError: (error: unknown) => {
                setMessage(`Error: ${extractApiErrorMessage(
                    error, 'We could not change your password. Please try again.')}`);
            }
        });
    };

    const summaryMessages = Object.values(validationMessages)
        .filter((validationMessage): validationMessage is string => validationMessage != null);

    return (
        <>
            <h3>Change password</h3>
            <StatusMessage message={message} />
            <div className="row">
                <div className="col-xl-6">
                    <form onSubmit={onValidSubmit} noValidate>
                        {summaryMessages.length > 0 && (
                            <ul className="text-danger" role="alert">
                                {summaryMessages.map((summaryMessage) =>
                                    <li key={summaryMessage}>{summaryMessage}</li>)}
                            </ul>
                        )}
                        <div className="form-floating mb-3">
                            <input
                                type="password"
                                value={oldPassword}
                                onChange={(event) => setOldPassword(event.target.value)}
                                id="Input.OldPassword"
                                className="form-control"
                                autoComplete="current-password"
                                aria-required="true"
                                placeholder="Enter the old password" />
                            <label htmlFor="Input.OldPassword" className="form-label">Old password</label>
                            {validationMessages.oldPassword != null && (
                                <div className="text-danger">{validationMessages.oldPassword}</div>
                            )}
                        </div>
                        <div className="form-floating mb-3">
                            <input
                                type="password"
                                value={newPassword}
                                onChange={(event) => setNewPassword(event.target.value)}
                                id="Input.NewPassword"
                                className="form-control"
                                autoComplete="new-password"
                                aria-required="true"
                                placeholder="Enter the new password" />
                            <label htmlFor="Input.NewPassword" className="form-label">New password</label>
                            {validationMessages.newPassword != null && (
                                <div className="text-danger">{validationMessages.newPassword}</div>
                            )}
                        </div>
                        <div className="form-floating mb-3">
                            <input
                                type="password"
                                value={confirmPassword}
                                onChange={(event) => setConfirmPassword(event.target.value)}
                                id="Input.ConfirmPassword"
                                className="form-control"
                                autoComplete="new-password"
                                aria-required="true"
                                placeholder="Enter the new password" />
                            <label htmlFor="Input.ConfirmPassword" className="form-label">Confirm password</label>
                            {validationMessages.confirmPassword != null && (
                                <div className="text-danger">{validationMessages.confirmPassword}</div>
                            )}
                        </div>
                        <button
                            type="submit"
                            className="w-100 btn btn-lg btn-primary"
                            disabled={changePassword.isPending}>
                            Update password
                        </button>
                    </form>
                </div>
            </div>
        </>
    );
}
