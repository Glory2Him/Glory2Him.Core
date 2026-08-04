import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { accountService } from '../../services/foundations/accountService';
import { StatusMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/ConfirmEmailChange.razor: the emailed change-email
// confirmation link lands here with userId, email and code.
export function ConfirmEmailChange() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const userId = searchParams.get('userId');
    const email = searchParams.get('email');
    const code = searchParams.get('code');

    const [message, setMessage] = useState<string | null>(null);
    const confirmEmailChange = accountService.useConfirmEmailChange();
    const confirmed = useRef(false);

    useEffect(() => {
        if (userId == null || email == null || code == null) {
            navigate('/Account/Login', {
                state: { statusMessage: 'Error: Invalid email change confirmation link.' }
            });

            return;
        }

        // The confirmation token is single-use — guard against StrictMode's
        // double effect invocation.
        if (confirmed.current) {
            return;
        }

        confirmed.current = true;

        confirmEmailChange.mutate({ userId, email, code }, {
            onSuccess: (result) => setMessage(result.message),
            onError: () => setMessage('Error changing email.')
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [userId, email, code]);

    return (
        <>
            <h1>Confirm email change</h1>

            <StatusMessage message={message} />
        </>
    );
}
