import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { accountService } from '../../services/foundations/accountService';
import { StatusMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/RegisterConfirmation.razor. The demo has no real
// email sender registered, so the server surfaces the confirmation link directly.
export function RegisterConfirmation() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const email = searchParams.get('email');
    const returnUrl = searchParams.get('returnUrl') ?? searchParams.get('ReturnUrl');

    const registerConfirmation =
        accountService.useGetRegisterConfirmation(email, returnUrl);

    useEffect(() => {
        if (email == null) {
            navigate('/');
        }
    }, [email, navigate]);

    const statusMessage = registerConfirmation.isError
        ? 'Error finding user for unspecified email'
        : null;

    const emailConfirmationLink =
        registerConfirmation.data?.emailConfirmationLink ?? null;

    return (
        <>
            <h1>Register confirmation</h1>

            <StatusMessage message={statusMessage} />

            {emailConfirmationLink != null ? (
                <p>
                    This app does not currently have a real email sender registered, see <a href="https://aka.ms/aspaccountconf">these docs</a> for how to configure a real email sender.
                    Normally this would be emailed: <a href={emailConfirmationLink}>Click here to confirm your account</a>
                </p>
            ) : (
                !registerConfirmation.isError && (
                    <p role="alert">Please check your email to confirm your account.</p>
                )
            )}
        </>
    );
}
