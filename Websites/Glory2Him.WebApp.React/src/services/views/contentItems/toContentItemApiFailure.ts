import axios from 'axios';

import {
    ContentItemValidationIssues
} from '../../../models/components/contentItems/contentItemFormItem';

// What a failed content-item write amounts to on screen: one line for the notification, and —
// when the API named the fields — the readback the form marks itself up from.
export type ContentItemApiFailure = {
    message: string;
    validationIssues?: ContentItemValidationIssues;
};

// A RESTFulSense controller turns the service's Xeption into a ValidationProblemDetails: `title`
// carries the human-readable reason ("Content item is invalid, fix the errors and try again.")
// and `errors` carries the per-parameter messages the validation built. `message` is checked too
// because the app's own admin endpoints answer in that shape.
type ApiProblem = {
    title?: unknown;
    message?: unknown;
    detail?: unknown;
    errors?: Record<string, unknown>;
};

// The wire admits a string as readily as an array — one message on a field arrives either way
// depending on who built the response — so both are accepted and normalized to the array the
// panel renders.
const asMessages = (value: unknown): ReadonlyArray<string> => {
    if (Array.isArray(value)) {
        return value.filter((entry): entry is string => typeof entry === 'string');
    }

    return typeof value === 'string' ? [value] : [];
};

const asText = (value: unknown): string =>
    typeof value === 'string' && value.trim().length > 0 ? value : '';

export const toContentItemApiFailure = (
    error: unknown,
    fallbackMessage: string
): ContentItemApiFailure => {
    if (axios.isAxiosError(error) === false) {
        return { message: fallbackMessage };
    }

    const problem = error.response?.data as ApiProblem | undefined;
    const issues: Record<string, ReadonlyArray<string>> = {};

    Object.entries(problem?.errors ?? {}).forEach(([field, value]) => {
        const messages = asMessages(value);

        if (messages.length > 0) {
            issues[field] = messages;
        }
    });

    const hasIssues = Object.keys(issues).length > 0;

    const message =
        asText(problem?.title)
        || asText(problem?.message)
        || asText(problem?.detail)
        || fallbackMessage;

    return hasIssues
        ? { message, validationIssues: issues }
        : { message };
};
