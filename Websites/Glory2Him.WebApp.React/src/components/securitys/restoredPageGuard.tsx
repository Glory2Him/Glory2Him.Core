import { ReactElement, useEffect, useRef } from 'react';
import { onlineManager } from '@tanstack/react-query';
import { accountService } from '../../services/foundations/accountService';
import { CurrentUser } from '../../models/accounts/currentUser';

const signedInReaderOf = (currentUser: CurrentUser | undefined): string | undefined =>
    currentUser?.isAuthenticated ? currentUser.userId : undefined;

// Hiding the root alone lets through any element a stylesheet marks `visibility: visible`, as the
// theme's own rules do, some with `!important`. Nothing inside the root escapes its opacity, and
// nothing inside an inert body can be reached by a click, the keyboard or assistive technology.
const hidePage = (): void => {
    const rootStyle = document.documentElement.style;
    rootStyle.visibility = 'hidden';
    rootStyle.setProperty('opacity', '0', 'important');
    document.body.setAttribute('inert', '');
};

const showPage = (): void => {
    const rootStyle = document.documentElement.style;
    rootStyle.removeProperty('visibility');
    rootStyle.removeProperty('opacity');
    document.body.removeAttribute('inert');
};

type PlaceOnThePage = {
    scrollPosition: number;
    horizontalScrollPosition: number;
    focusedElement: Element | null;
};

const notePlaceOnThePage = (): PlaceOnThePage => ({
    scrollPosition: window.scrollY,
    horizontalScrollPosition: window.scrollX,
    focusedElement: document.activeElement
});

// A browser does not scroll a page taken out of the render, nor focus a field out of reach of
// input, so the place is put back only once the page is shown again.
const resumePlaceOnThePage = (place: PlaceOnThePage): void => {
    window.scrollTo({ left: place.horizontalScrollPosition, top: place.scrollPosition });

    if (place.focusedElement instanceof HTMLElement && place.focusedElement !== document.body) {
        place.focusedElement.focus({ preventScroll: true });
    }
};

export const RestoredPageGuard = (): ReactElement => {
    const { data: currentUser, refetch } = accountService.useGetCurrentUser();
    const currentUserReference = useRef(currentUser);
    const cachedReaderReference = useRef<string | undefined>(undefined);
    const cachedPlaceReference = useRef<PlaceOnThePage>({
        scrollPosition: 0,
        horizontalScrollPosition: 0,
        focusedElement: null
    });

    useEffect(() => {
        currentUserReference.current = currentUser;
    }, [currentUser]);

    useEffect(() => {
        const handlePageHide = () => {
            cachedReaderReference.current = signedInReaderOf(currentUserReference.current);
            cachedPlaceReference.current = notePlaceOnThePage();

            // The page goes into the cache hidden as a restored page is, which moves nothing the
            // router saves for a page loaded afresh.
            hidePage();
        };

        const handlePageShow = async (event: PageTransitionEvent) => {
            if (!event.persisted) {
                return;
            }

            hidePage();

            if (!onlineManager.isOnline()) {
                window.location.reload();

                return;
            }

            const cachedReader = cachedReaderReference.current;
            const freshRead = await refetch();

            if (!freshRead.isError
                && cachedReader !== undefined
                && signedInReaderOf(freshRead.data) === cachedReader) {
                showPage();
                resumePlaceOnThePage(cachedPlaceReference.current);
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
