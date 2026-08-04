import { useSearchParams } from 'react-router-dom';
import { StatusMessage } from './statusMessage';

// Ported from Blazor's Account/Pages/InvalidUser.razor. Blazor surfaced the status message
// via a redirect cookie; here an optional ?message= query parameter carries it instead.
export function InvalidUser() {
    const [searchParams] = useSearchParams();

    return (
        <>
            <h3>Invalid user</h3>

            <StatusMessage message={searchParams.get('message')} />
        </>
    );
}
