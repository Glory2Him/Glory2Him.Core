import { useNavigate } from 'react-router-dom';

const isLocal = (returnUrl: string | null | undefined): returnUrl is string => {
    if (!returnUrl?.startsWith('/')) {
        return false;
    }

    return new URL(returnUrl, window.location.origin).origin === window.location.origin;
};

export const useSignInReturn = (): ((returnUrl: string | null | undefined) => void) => {
    const navigate = useNavigate();

    return (returnUrl) => {
        navigate(isLocal(returnUrl) ? returnUrl : '/');
    };
};
