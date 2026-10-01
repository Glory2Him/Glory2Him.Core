import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Login } from './login';

interface LoginCallbacks {
    onSuccess: (loginResult: { requiresTwoFactor: boolean }) => void;
    onError: (error: unknown) => void;
}

const mocks = vi.hoisted(() => ({ loginMutate: vi.fn() }));

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useLogin: () => ({ mutate: mocks.loginMutate, isPending: false })
    }
}));

vi.mock('../../services/foundations/passkeyService', () => ({
    passkeyService: {
        usePasskeySignIn: () => ({ mutate: vi.fn(), isPending: false }),
        useGetExternalProviders: () => ({ data: [], isLoading: false })
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

const renderLogin = (initialEntry: string) => render(
    <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
            <Route path="/Account/Login" element={<Login />} />
            <Route path="*" element={null} />
        </Routes>
        <Landed />
    </MemoryRouter>);

const loginWithReturnUrl = (returnUrl: string) =>
    `/Account/Login?returnUrl=${encodeURIComponent(returnUrl)}`;

const landedOn = () => screen.getByTestId('landed').textContent;

const acceptSignIn = (requiresTwoFactor: boolean) =>
    mocks.loginMutate.mockImplementation((_request: unknown, callbacks: LoginCallbacks) =>
        callbacks.onSuccess({ requiresTwoFactor }));

const signInWithPassword = () => {
    fireEvent.change(screen.getByLabelText('Username or email'), { target: { value: 'reader' } });
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'a password' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign me in' }));
};

describe('Login', () => {
    beforeEach(() => {
        mocks.loginMutate.mockReset();
    });

    it('should send a reader signed in by password on to their return address', () => {
        // given
        acceptSignIn(false);
        renderLogin(loginWithReturnUrl('/posts?q=grace#comments'));

        // when
        signInWithPassword();

        // then
        expect(landedOn()).toBe('/posts?q=grace#comments');
    });

    it('should send a reader signed in by password on unchanged to every local return address', () => {
        for (const returnUrl of acceptedReturnUrls) {
            // given
            acceptSignIn(false);
            const { unmount } = renderLogin(loginWithReturnUrl(returnUrl));

            // when
            signInWithPassword();

            // then
            expect(landedOn()).toBe(returnUrl);
            unmount();
        }
    });

    it('should hand the return address on unchanged when a second factor is asked', () => {
        // given
        acceptSignIn(true);
        renderLogin(loginWithReturnUrl('/posts?q=grace#comments'));

        // when
        signInWithPassword();

        // then
        const landed = new URL(landedOn() ?? '', window.location.origin);
        expect(landed.pathname).toBe('/Account/LoginWith2fa');
        expect(landed.searchParams.get('ReturnUrl')).toBe('/posts?q=grace#comments');
    });

    it('should keep a reader whose password sign-in is refused on the sign-in page', () => {
        // given
        mocks.loginMutate.mockImplementation((_request: unknown, callbacks: LoginCallbacks) =>
            callbacks.onError(new Error('Invalid login attempt.')));

        renderLogin(loginWithReturnUrl('/posts'));

        // when
        signInWithPassword();

        // then
        expect(landedOn()).toBe(loginWithReturnUrl('/posts'));
    });

    it('should send a reader signed in by password to the home page when there is no return address', () => {
        // given
        acceptSignIn(false);
        renderLogin('/Account/Login');

        // when
        signInWithPassword();

        // then
        expect(landedOn()).toBe('/');
    });
});
