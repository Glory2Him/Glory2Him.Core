import { useNavigate } from 'react-router-dom';

export const useSignInReturn = (): ((returnUrl: string | null | undefined) => void) => {
    const navigate = useNavigate();

    return (returnUrl) => {
        navigate(returnUrl?.startsWith('/') ? returnUrl : '/');
    };
};
