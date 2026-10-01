import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LoginWithRecoveryCode } from './loginWithRecoveryCode';

interface LoginWithRecoveryCodeCallbacks {
    onSuccess: (result: { isLockedOut: boolean }) => void;
    onError: (error: unknown) => void;
}

const mocks = vi.hoisted(() => ({ loginWithRecoveryCodeMutate: vi.fn() }));

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useLoginWithRecoveryCode: () => ({ mutate: mocks.loginWithRecoveryCodeMutate, isPending: false })
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

const renderLoginWithRecoveryCode = (initialEntry: string) => render(
    <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
            <Route path="/Account/LoginWithRecoveryCode" element={<LoginWithRecoveryCode />} />
            <Route path="*" element={null} />
        </Routes>
        <Landed />
    </MemoryRouter>);

const loginWithRecoveryCodeWithReturnUrl = (returnUrl: string) =>
    `/Account/LoginWithRecoveryCode?ReturnUrl=${encodeURIComponent(returnUrl)}`;

const landedOn = () => screen.getByTestId('landed').textContent;

const answerCode = (isLockedOut: boolean) =>
    mocks.loginWithRecoveryCodeMutate.mockImplementation(
        (_recoveryCode: unknown, callbacks: LoginWithRecoveryCodeCallbacks) => callbacks.onSuccess({ isLockedOut }));

const enterRecoveryCode = () => {
    fireEvent.change(screen.getByLabelText('Recovery Code'), { target: { value: 'a1b2c-d3e4f' } });
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }));
};

describe('LoginWithRecoveryCode', () => {
    beforeEach(() => {
        mocks.loginWithRecoveryCodeMutate.mockReset();
    });

    it('should send a reader signed in by a recovery code on to their return address', () => {
        // given
        answerCode(false);
        renderLoginWithRecoveryCode(loginWithRecoveryCodeWithReturnUrl('/posts?q=grace#comments'));

        // when
        enterRecoveryCode();

        // then
        expect(landedOn()).toBe('/posts?q=grace#comments');
    });

    it('should send a reader signed in by a recovery code on unchanged to every local return address', () => {
        for (const returnUrl of acceptedReturnUrls) {
            // given
            answerCode(false);
            const { unmount } = renderLoginWithRecoveryCode(loginWithRecoveryCodeWithReturnUrl(returnUrl));

            // when
            enterRecoveryCode();

            // then
            expect(landedOn()).toBe(returnUrl);
            unmount();
        }
    });

    it('should send a locked-out reader to the lockout page instead of their return address', () => {
        // given
        answerCode(true);
        renderLoginWithRecoveryCode(loginWithRecoveryCodeWithReturnUrl('/posts'));

        // when
        enterRecoveryCode();

        // then
        expect(landedOn()).toBe('/Account/Lockout');
    });

    it('should keep a reader whose recovery code is refused on the recovery-code page', () => {
        // given
        mocks.loginWithRecoveryCodeMutate.mockImplementation(
            (_recoveryCode: unknown, callbacks: LoginWithRecoveryCodeCallbacks) =>
                callbacks.onError(new Error('Invalid recovery code entered.')));

        renderLoginWithRecoveryCode(loginWithRecoveryCodeWithReturnUrl('/posts'));

        // when
        enterRecoveryCode();

        // then
        expect(landedOn()).toBe(loginWithRecoveryCodeWithReturnUrl('/posts'));
    });

    it('should send a reader signed in by a recovery code to the home page when there is no return address', () => {
        // given
        answerCode(false);
        renderLoginWithRecoveryCode('/Account/LoginWithRecoveryCode');

        // when
        enterRecoveryCode();

        // then
        expect(landedOn()).toBe('/');
    });

    it('should send a reader signed in by a recovery code to the home page when the return address is not local', () => {
        for (const returnUrl of refusedReturnUrls) {
            // given
            answerCode(false);
            const { unmount } = renderLoginWithRecoveryCode(loginWithRecoveryCodeWithReturnUrl(returnUrl));

            // when
            enterRecoveryCode();

            // then
            expect(landedOn()).toBe('/');
            unmount();
        }
    });
});
