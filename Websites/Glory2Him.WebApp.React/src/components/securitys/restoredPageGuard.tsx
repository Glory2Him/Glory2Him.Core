import { ReactElement, useEffect, useRef } from 'react';
import { accountService } from '../../services/foundations/accountService';

export const RestoredPageGuard = (): ReactElement => {
    const { data: currentUser, refetch } = accountService.useGetCurrentUser();
    const currentUserReference = useRef(currentUser);
    const cachedReaderReference = useRef<string | undefined>(undefined);

    useEffect(() => {
        currentUserReference.current = currentUser;
    }, [currentUser]);

    useEffect(() => {
        const handlePageHide = () => {
            cachedReaderReference.current = currentUserReference.current?.userId;
        };

        const handlePageShow = async () => {
            document.documentElement.style.visibility = 'hidden';
            const freshRead = await refetch();

            if (freshRead.data?.userId === cachedReaderReference.current) {
                document.documentElement.style.removeProperty('visibility');
            } else {
                window.location.reload();
            }
        };

        window.addEventListener('pagehide', handlePageHide);
        window.addEventListener('pageshow', handlePageShow);

        return () => {
            window.removeEventListener('pagehide', handlePageHide);
            window.removeEventListener('pageshow', handlePageShow);
        };
    }, [refetch]);

    return <></>;
}
