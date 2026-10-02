import { FormEvent, useState } from 'react';
import { manageAccountService } from '../../../services/foundations/manageAccountService';
import { StatusMessage, extractApiErrorMessage } from '../statusMessage';

// Ported from Blazor's Account/Pages/Manage/DeletePersonalData.razor. On success the
// account is gone and the user is signed out, so the browser loads the home page afresh
// in this page's place — nothing read for the reader stays in the tab (§UI20.8 rule 3).
export function DeletePersonalData() {
    const [password, setPassword] = useState('');
    const [message, setMessage] = useState<string | null>(null);

    const personalDataInfo = manageAccountService.useGetPersonalDataInfo();
    const deletePersonalData = manageAccountService.useDeletePersonalData();

    const requirePassword = personalDataInfo.data?.requirePassword ?? true;

    const onValidSubmit = (event: FormEvent) => {
        event.preventDefault();
        setMessage(null);

        deletePersonalData.mutate(password, {
            onSuccess: () => {
                location.replace('/');
            },
            onError: (error: unknown) => {
                setMessage(extractApiErrorMessage(
                    error, 'Error: Unexpected error occurred deleting user.'));
            }
        });
    };

    return (
        <>
            <StatusMessage message={message} />

            <h3>Delete Personal Data</h3>

            <div className="alert alert-warning" role="alert">
                <p>
                    <strong>Deleting this data will permanently remove your account, and this cannot be recovered.</strong>
                </p>
            </div>

            <div>
                <form onSubmit={onValidSubmit} noValidate>
                    {requirePassword && (
                        <div className="form-floating mb-3">
                            <input
                                type="password"
                                value={password}
                                onChange={(event) => setPassword(event.target.value)}
                                id="Input.Password"
                                className="form-control"
                                autoComplete="current-password"
                                aria-required="true"
                                placeholder="Please enter your password." />
                            <label htmlFor="Input.Password" className="form-label">Password</label>
                        </div>
                    )}
                    <button
                        className="w-100 btn btn-lg btn-danger"
                        type="submit"
                        disabled={deletePersonalData.isPending}>
                        Delete data and close my account
                    </button>
                </form>
            </div>
        </>
    );
}
