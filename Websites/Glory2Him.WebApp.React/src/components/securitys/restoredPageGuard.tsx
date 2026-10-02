import { ReactElement, useEffect, useRef } from 'react';
import { accountService } from '../../services/foundations/accountService';
import { CurrentUser } from '../../models/accounts/currentUser';

const signedInReaderOf = (currentUser: CurrentUser | undefined): string | undefined =>
    currentUser?.isAuthenticated ? currentUser.userId : undefined;

export const RestoredPageGuard = (): ReactElement => {
    const { data: currentUser, refetch } = accountService.useGetCurrentUser();
    const currentUserReference = useRef(currentUser);
    const cachedReaderReference = useRef<string | undefined>(undefined);

    useEffect(() => {
        currentUserReference.current = currentUser;
    }, [currentUser]);

    useEffect(() => {
        const handlePageHide = () => {
            cachedReaderReference.current = signedInReaderOf(currentUserReference.current);
        };

        const handlePageShow = async () => {
            document.documentElement.style.visibility = 'hidden';
            const cachedReader = cachedReaderReference.current;
            const freshRead = await refetch();

            if (cachedReader !== undefined && signedInReaderOf(freshRead.data) === cachedReader) {
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
