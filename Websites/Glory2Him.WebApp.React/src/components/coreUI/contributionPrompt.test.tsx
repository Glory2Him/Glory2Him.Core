import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { ContributionPrompt } from './contributionPrompt';

describe('ContributionPrompt', () => {
    it('should link an authenticated user straight to the contribution route', () => {
        // when
        render(
            <MemoryRouter>
                <ContributionPrompt isAuthenticated={true} loginHref="/Account/Login" />
            </MemoryRouter>);

        // then
        const submitLink = screen.getByRole('link', { name: /Submit a contribution/ });
        expect(submitLink).toHaveAttribute('href', '/post/contribute');
        expect(screen.queryByText(/Login to share/)).not.toBeInTheDocument();
    });

    it('should show a login prompt instead of the submit link for an anonymous user', () => {
        // when
        render(
            <MemoryRouter>
                <ContributionPrompt
                    isAuthenticated={false}
                    loginHref="/Account/Login?returnUrl=%2F" />
            </MemoryRouter>);

        // then
        expect(screen.queryByRole('link', { name: /Submit a contribution/ })).not.toBeInTheDocument();
        const loginLink = screen.getByRole('link', { name: /Login to share something/ });
        expect(loginLink).toHaveAttribute('href', '/Account/Login?returnUrl=%2F');
    });
});
