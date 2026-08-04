import axios from 'axios';

// Admin API 400s carry { message } with a human-readable reason (the Blazor pages surfaced
// the equivalent UsersViewValidationException.Message). Anything else falls back to the
// page's generic wording.
export function extractApiErrorMessage(error: unknown, fallback: string): string {
    if (axios.isAxiosError(error)) {
        const data = error.response?.data as { message?: unknown } | undefined;

        if (typeof data?.message === 'string' && data.message.length > 0) {
            return data.message;
        }
    }

    return fallback;
}
