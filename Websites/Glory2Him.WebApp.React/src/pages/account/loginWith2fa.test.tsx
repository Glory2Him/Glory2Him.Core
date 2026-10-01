import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LoginWith2fa } from './loginWith2fa';

interface LoginWith2faCallbacks {
    onSuccess: (result: { isLockedOut: boolean }) => void;
    onError: (error: unknown) => void;
}

const mocks = vi.hoisted(() => ({ loginWith2faMutate: vi.fn() }));

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useLoginWith2fa: () => ({ mutate: mocks.loginWith2faMutate, isPending: false })
    }
}));

// The inputs GHSA-xm46-3gcv-3gxp's Suggested fix table lists as accepted, each as a page reads it
// from its address once the query string is decoded.
const acceptedReturnUrls = ['/%2F%2Fevil.example', '/posts?q=grace#comments'];

// The inputs the same table lists as refused, read the same way.
const refusedReturnUrls = [
    '//evil.example',
    '/\\evil.example',
    'https://evil.example',
    'javascript:alert(1)',
    '/\t/evil.example'
];

// Rendered beside the routes, so the test reads where the reader is from the router itself.
const Landed = () => {
    const { pathname, search, hash } = useLocation();

    return <output data-testid="landed">{`${pathname}${search}${hash}`}</output>;
};

const renderLoginWith2fa = (initialEntry: string) => render(
    <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
            <Route path="/Account/LoginWith2fa" element={<LoginWith2fa />} />
            <Route path="*" element={null} />
        </Routes>
        <Landed />
    </MemoryRouter>);

const loginWith2faWithReturnUrl = (returnUrl: string) =>
    `/Account/LoginWith2fa?ReturnUrl=${encodeURIComponent(returnUrl)}&RememberMe=false`;

const landedOn = () => screen.getByTestId('landed').textContent;

const answerCode = (isLockedOut: boolean) =>
    mocks.loginWith2faMutate.mockImplementation((_request: unknown, callbacks: LoginWith2faCallbacks) =>
        callbacks.onSuccess({ isLockedOut }));

const enterAuthenticatorCode = () => {
    fireEvent.change(screen.getByLabelText('Authenticator code'), { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }));
};

describe('LoginWith2fa', () => {
    beforeEach(() => {
        mocks.loginWith2faMutate.mockReset();
    });

    it('should send a reader signed in by a second factor on to their return address', () => {
        // given
        answerCode(false);
        renderLoginWith2fa(loginWith2faWithReturnUrl('/posts?q=grace#comments'));

        // when
        enterAuthenticatorCode();

        // then
        expect(landedOn()).toBe('/posts?q=grace#comments');
    });

    it('should send a reader signed in by a second factor on unchanged to every local return address', () => {
        for (const returnUrl of acceptedReturnUrls) {
            // given
            answerCode(false);
            const { unmount } = renderLoginWith2fa(loginWith2faWithReturnUrl(returnUrl));

            // when
            enterAuthenticatorCode();

            // then
            expect(landedOn()).toBe(returnUrl);
            unmount();
        }
    });

    it('should hand the return address on unchanged to the recovery-code page', () => {
        // given
        renderLoginWith2fa(loginWith2faWithReturnUrl('/posts?q=grace#comments'));

        // when
        fireEvent.click(screen.getByRole('link', { name: 'log in with a recovery code' }));

        // then
        const landed = new URL(landedOn() ?? '', window.location.origin);
        expect(landed.pathname).toBe('/Account/LoginWithRecoveryCode');
        expect(landed.searchParams.get('ReturnUrl')).toBe('/posts?q=grace#comments');
    });

    it('should send a locked-out reader to the lockout page instead of their return address', () => {
        // given
        answerCode(true);
        renderLoginWith2fa(loginWith2faWithReturnUrl('/posts'));

        // when
        enterAuthenticatorCode();

        // then
        expect(landedOn()).toBe('/Account/Lockout');
    });

    it('should keep a reader whose authenticator code is refused on the second-factor page', () => {
        // given
        mocks.loginWith2faMutate.mockImplementation((_request: unknown, callbacks: LoginWith2faCallbacks) =>
            callbacks.onError(new Error('Invalid authenticator code.')));

        renderLoginWith2fa(loginWith2faWithReturnUrl('/posts'));

        // when
        enterAuthenticatorCode();

        // then
        expect(landedOn()).toBe(loginWith2faWithReturnUrl('/posts'));
    });

    it('should send a reader signed in by a second factor to the home page when there is no return address', () => {
        // given
        answerCode(false);
        renderLoginWith2fa('/Account/LoginWith2fa?RememberMe=false');

        // when
        enterAuthenticatorCode();

        // then
        expect(landedOn()).toBe('/');
    });

    it('should send a reader signed in by a second factor to the home page when the return address is not local', () => {
        for (const returnUrl of refusedReturnUrls) {
            // given
            answerCode(false);
            const { unmount } = renderLoginWith2fa(loginWith2faWithReturnUrl(returnUrl));

            // when
            enterAuthenticatorCode();

            // then
            expect(landedOn()).toBe('/');
            unmount();
        }
    });
});
