import { Link } from 'react-router-dom';

// Ported from Blazor's Account/Pages/ResetPasswordConfirmation.razor.
export function ResetPasswordConfirmation() {
    return (
        <>
            <h1>Reset password confirmation</h1>
            <p role="alert">
                Your password has been reset. Please <Link to="/Account/Login">click here to log in</Link>.
            </p>
        </>
    );
}
