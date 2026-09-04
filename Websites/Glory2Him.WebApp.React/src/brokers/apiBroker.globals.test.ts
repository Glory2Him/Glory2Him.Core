import { QueryClient } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { queryClientGlobalOptions } from './apiBroker.globals';

// THE GENERIC FAILURE TOAST, and who is allowed to answer for themselves instead. A read whose
// refusal is an ANSWER — the approval endpoints 404 both for an entity with no round and for a
// caller outside the moderation tier, by design — must be able to opt out, or one page opening
// several of them shouts several times about a state it renders correctly.
vi.mock('./toastBroker.error', () => ({
    toastError: vi.fn()
}));

const { toastError } = await import('./toastBroker.error');
const toastErrorMock = vi.mocked(toastError);

const failWith = async (client: QueryClient, meta?: Record<string, unknown>) => {
    await client
        .fetchQuery({
            queryKey: ['failing', JSON.stringify(meta ?? {})],
            queryFn: async () => {
                throw new Error('refused');
            },
            retry: false,
            meta
        })
        .catch(() => undefined);
};

describe('the global failure toast', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('should tell the reader when a read fails for no stated reason', async () => {
        // when
        await failWith(queryClientGlobalOptions);

        // then
        expect(toastErrorMock).toHaveBeenCalledTimes(1);
    });

    /// The opt-out queries needed and did not have: mutations could already declare it, reads
    /// could not, so every refusal reached the reader as an unexplained failure.
    it('should stay quiet for a read that answers for itself', async () => {
        // when
        await failWith(queryClientGlobalOptions, { suppressGlobalErrorToast: true });

        // then
        expect(toastErrorMock).not.toHaveBeenCalled();
    });
});
