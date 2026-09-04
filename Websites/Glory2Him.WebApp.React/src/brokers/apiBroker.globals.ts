import {
    Mutation,
    MutationCache,
    Query,
    QueryCache,
    QueryClient
} from '@tanstack/react-query';
import { toastError } from './toastBroker.error';

// A mutation that shows the API's OWN message — a validation readback on the form, a toast naming
// what is actually wrong — opts out of the generic toast below by declaring
// meta: { suppressGlobalErrorToast: true }. Without it the reader gets two notifications for one
// failure, the second less useful than the first.
const suppressesGlobalErrorToast = (
    mutation: Mutation<unknown, unknown, unknown, unknown> | undefined): boolean =>
    mutation?.meta?.suppressGlobalErrorToast === true;

// A READ opts out the same way, and needs to: some refusals are ANSWERS. The approval endpoints
// return 404 both for an entity with no approval round and for a caller outside the moderation
// tier — deliberately, so the endpoint cannot be used to probe what exists (§14.5 rule 1) — and
// the panel renders that as "no verdict" perfectly well. Without this a single page opening
// three such reads shouted three times about a state it was handling correctly.
const querySuppressesGlobalErrorToast = (
    query: Query<unknown, unknown, unknown, readonly unknown[]> | undefined): boolean =>
    query?.meta?.suppressGlobalErrorToast === true;

export const queryClientGlobalOptions = new QueryClient({
    defaultOptions: {
        queries: {
            retry: false
        }
    },
    queryCache: new QueryCache({
        onError: (error: Error, query) => {
            if (querySuppressesGlobalErrorToast(query) === false) {
                toastError(
                    "An unknown error has occurred, please refresh the page and try again.");
            }

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
