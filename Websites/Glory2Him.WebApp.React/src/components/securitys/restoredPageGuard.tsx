import { ReactElement, useEffect } from 'react';
import { accountService } from '../../services/foundations/accountService';

export const RestoredPageGuard = (): ReactElement => {
    const { refetch } = accountService.useGetCurrentUser();

    useEffect(() => {
        const handlePageShow = async () => {
            await refetch();
        };

        window.addEventListener('pageshow', handlePageShow);

        return () => window.removeEventListener('pageshow', handlePageShow);
    }, [refetch]);

    return <></>;
}
