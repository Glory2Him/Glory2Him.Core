import { Mutation, MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';
import { toastError } from './toastBroker.error';

// A mutation that shows the API's OWN message — a validation readback on the form, a toast naming
// what is actually wrong — opts out of the generic toast below by declaring
// meta: { suppressGlobalErrorToast: true }. Without it the reader gets two notifications for one
// failure, the second less useful than the first.
const suppressesGlobalErrorToast = (
    mutation: Mutation<unknown, unknown, unknown, unknown> | undefined): boolean =>
    mutation?.meta?.suppressGlobalErrorToast === true;

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
        onError: (error: Error, _variables, _context, mutation) => {
            if (error && suppressesGlobalErrorToast(mutation) === false) {
                toastError("An unknown error has occurred, please try again.");
            }

            throw error;
        }
    })
});
