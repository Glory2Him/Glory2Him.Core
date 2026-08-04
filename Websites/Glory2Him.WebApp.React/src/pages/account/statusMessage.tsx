import axios from 'axios';

// Ported from Blazor's Account/Shared/StatusMessage.razor: a success alert unless the
// message starts with "Error", in which case it renders as danger.
export interface StatusMessageProps {
    message?: string | null;
}

export function StatusMessage({ message }: StatusMessageProps) {
    if (message == null || message.length === 0) {
        return null;
    }

    const statusMessageClass = message.startsWith('Error') ? 'danger' : 'success';

    return (
        <div className={`alert alert-${statusMessageClass}`} role="alert">
            {message}
        </div>
    );
}

// API 400s carry { message, errors? } — flatten them to the single line the Blazor pages
// showed in their status message / validation summary.
// eslint-disable-next-line react-refresh/only-export-components
export function extractApiErrorMessage(error: unknown, fallback: string): string {
    if (axios.isAxiosError(error)) {
        const data = error.response?.data as
            | { message?: string; errors?: Record<string, string[]> }
            | undefined;

        const detailMessages = data?.errors != null
            ? Object.values(data.errors).flat()
            : [];

        if (detailMessages.length > 0) {
            return detailMessages.join(' ');
        }

        if (data?.message != null && data.message.length > 0) {
            return data.message;
        }
    }

    return fallback;
}
