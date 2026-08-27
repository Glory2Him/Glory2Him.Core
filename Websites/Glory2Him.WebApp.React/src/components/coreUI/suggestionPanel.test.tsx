import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { SuggestionPanel } from './suggestionPanel';

describe('SuggestionPanel', () => {
    it('should render the suggestion input for an authenticated user', () => {
        // when
        render(
            <MemoryRouter>
                <SuggestionPanel
                    heading="Tags"
                    suggestHeading="Suggest a tag"
                    placeholder="Start typing a tag…"
                    isAuthenticated={true}
                    loginHref="/Account/Login" />
            </MemoryRouter>);

        // then
        expect(screen.getByPlaceholderText('Start typing a tag…')).toBeInTheDocument();
        expect(screen.queryByText(/Login to/)).not.toBeInTheDocument();
    });

    it('should render a login prompt instead of the input for an anonymous user', () => {
        // when
        render(
            <MemoryRouter>
                <SuggestionPanel
                    heading="Tags"
                    suggestHeading="Suggest a tag"
                    placeholder="Start typing a tag…"
                    isAuthenticated={false}
                    loginHref="/Account/Login?returnUrl=%2FPost-Single" />
            </MemoryRouter>);

        // then
        expect(screen.queryByPlaceholderText('Start typing a tag…')).not.toBeInTheDocument();
        const loginLink = screen.getByRole('link', { name: /Login to suggest a tag/ });
        expect(loginLink).toBeInTheDocument();
        expect(loginLink).toHaveAttribute('href', '/Account/Login?returnUrl=%2FPost-Single');
    });

    it('should derive the login prompt text from the suggest heading', () => {
        // when
        render(
            <MemoryRouter>
                <SuggestionPanel
                    heading="Bible references"
                    suggestHeading="Suggest a bible reference"
                    isAuthenticated={false}
                    loginHref="/Account/Login" />
            </MemoryRouter>);

        // then
        expect(screen.getByRole('link', { name: /Login to suggest a bible reference/ })).toBeInTheDocument();
    });

    it('should honour an explicit loginPromptText override', () => {
        // when
        render(
            <MemoryRouter>
                <SuggestionPanel
                    heading="Tags"
                    isAuthenticated={false}
                    loginHref="/Account/Login"
                    loginPromptText="Sign in to suggest" />
            </MemoryRouter>);

        // then
        expect(screen.getByRole('link', { name: /Sign in to suggest/ })).toBeInTheDocument();
    });
});
