import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PasskeySignInButton } from './passkeySignInButton';

interface PasskeySignInCallbacks {
    onSuccess: () => void;
    onError: (error: unknown) => void;
}

const mocks = vi.hoisted(() => ({ passkeySignInMutate: vi.fn() }));

vi.mock('../../services/foundations/passkeyService', () => ({
    passkeyService: {
        usePasskeySignIn: () => ({ mutate: mocks.passkeySignInMutate, isPending: false })
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

const renderPasskeySignInButton = (returnUrl?: string) => render(
    <MemoryRouter initialEntries={['/Account/Login']}>
        <Routes>
            <Route
                path="/Account/Login"
                element={<PasskeySignInButton email="reader" returnUrl={returnUrl} onError={() => { }} />} />
            <Route path="*" element={null} />
        </Routes>
        <Landed />
    </MemoryRouter>);

const landedOn = () => screen.getByTestId('landed').textContent;

const acceptPasskey = () =>
    mocks.passkeySignInMutate.mockImplementation((_email: unknown, callbacks: PasskeySignInCallbacks) =>
        callbacks.onSuccess());

const signInWithPasskey = () =>
    fireEvent.click(screen.getByRole('button', { name: 'Log in with a passkey' }));

describe('PasskeySignInButton', () => {
    beforeEach(() => {
        mocks.passkeySignInMutate.mockReset();
    });

    it('should send a reader signed in by passkey on to their return address', () => {
        // given
        acceptPasskey();
        renderPasskeySignInButton('/posts?q=grace#comments');

        // when
        signInWithPasskey();

        // then
        expect(landedOn()).toBe('/posts?q=grace#comments');
    });

    it('should send a reader signed in by passkey on unchanged to every local return address', () => {
        for (const returnUrl of acceptedReturnUrls) {
            // given
            acceptPasskey();
            const { unmount } = renderPasskeySignInButton(returnUrl);

            // when
            signInWithPasskey();

            // then
            expect(landedOn()).toBe(returnUrl);
            unmount();
        }
    });
});
