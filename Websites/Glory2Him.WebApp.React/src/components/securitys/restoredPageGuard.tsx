import { ReactElement, useEffect } from 'react';
import { accountService } from '../../services/foundations/accountService';

export const RestoredPageGuard = (): ReactElement => {
    const { refetch } = accountService.useGetCurrentUser();

    useEffect(() => {
        const handlePageShow = async () => {
            document.documentElement.style.visibility = 'hidden';
            await refetch();
            document.documentElement.style.removeProperty('visibility');
        };

        window.addEventListener('pageshow', handlePageShow);

        return () => window.removeEventListener('pageshow', handlePageShow);
    }, [refetch]);

    return <></>;
}
