import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { accountService } from '../../services/foundations/accountService';
import { StatusMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/ConfirmEmail.razor: the emailed confirmation link
// lands here with userId and code, and the token is confirmed on arrival.
export function ConfirmEmail() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const userId = searchParams.get('userId');
    const code = searchParams.get('code');

    const [statusMessage, setStatusMessage] = useState<string | null>(null);
    const confirmEmail = accountService.useConfirmEmail();
    const confirmed = useRef(false);

    useEffect(() => {
        if (userId == null || code == null) {
            navigate('/');
            return;
        }

        // The confirmation token is single-use — guard against StrictMode's
        // double effect invocation.
        if (confirmed.current) {
            return;
        }

        confirmed.current = true;

        confirmEmail.mutate({ userId, code }, {
            onSuccess: (result) => setStatusMessage(result.message),
            onError: () => setStatusMessage(`Error loading user with ID ${userId}`)
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [userId, code]);

    return (
        <>
            <h1>Confirm email</h1>
            <StatusMessage message={statusMessage} />
        </>
    );
}
