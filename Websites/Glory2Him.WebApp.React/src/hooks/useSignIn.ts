import { useLocation, useNavigate } from 'react-router-dom';

// The one action that sends a reader to sign in (§UI20.6.6 rule 2). The sign-in page returns
// them to returnUrl, so the path, query and fragment they left travel as one encoded value.
// Who is sent is the caller's decision, never this hook's.
export const useSignIn = (): (() => void) => {
    const navigate = useNavigate();
    const location = useLocation();

    return () => {
        const returnUrl = `${location.pathname}${location.search}${location.hash}`;
        navigate(`/Account/Login?returnUrl=${encodeURIComponent(returnUrl)}`);
    };
};
