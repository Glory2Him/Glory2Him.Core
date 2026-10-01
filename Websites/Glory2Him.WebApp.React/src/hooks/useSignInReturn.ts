import { useNavigate } from 'react-router-dom';

// §SEC18.7.1 rule 7: a return address is local when it is written as a path and, resolved against
// this site's own address, names this site's origin. One that cannot be resolved names no origin.
const isLocal = (returnUrl: string | null | undefined): returnUrl is string => {
    if (!returnUrl?.startsWith('/')) {
        return false;
    }

    try {
        return new URL(returnUrl, window.location.origin).origin === window.location.origin;
    } catch {
        return false;
    }
};

// The one shared return after sign-in (§UI20.8.1). The reader is sent on to a local return address
// whole, with its path, query and fragment, and to the home page otherwise, which is not an error.
// Whether they have signed in is the caller's decision, never this hook's.
export const useSignInReturn = (): ((returnUrl: string | null | undefined) => void) => {
    const navigate = useNavigate();

    return (returnUrl) => {
        navigate(isLocal(returnUrl) ? returnUrl : '/');
    };
};
