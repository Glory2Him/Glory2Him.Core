import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';
import { toastError } from './toastBroker.error';

export const queryClientGlobalOptions = new QueryClient({
    defaultOptions: {
        queries: {
            retry: false
        }
    },
    queryCache: new QueryCache({
        onError: (error: Error) => {
            toastError("An unknown error has occurred, please refresh the page and try again.");
            throw error;
        }
    }),
    mutationCache: new MutationCache({
        onError: (error: Error) => {
            if (error) {
                toastError("An unknown error has occurred, please try again.");
            }
            throw error;
        }
    })
});
