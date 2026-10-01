import { ReactNode } from 'react';
import { Location, MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { act, renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { useSignInReturn } from './useSignInReturn';

// The hook is rendered beside useLocation in the same router, so the test reads where the
// signed-in reader landed from the router itself rather than from a mocked navigate. The reader
// starts on the sign-in page's route, as in the app, so an address resolves from where it would
// there and landing on the home page always means they were sent there. The catch-all route keeps
// the hook rendered wherever they land.
const renderSignInReturn = () => {
    const wrapper = ({ children }: { children: ReactNode }) => (
        <MemoryRouter initialEntries={['/Account/Login']}>
            <Routes>
                <Route path="/Account/Login" element={children} />
                <Route path="*" element={children} />
            </Routes>
        </MemoryRouter>
    );

    return renderHook(() => ({ signInReturn: useSignInReturn(), location: useLocation() }), { wrapper });
};

const landedOn = ({ pathname, search, hash }: Location) => `${pathname}${search}${hash}`;

// The inputs GHSA-xm46-3gcv-3gxp's Suggested fix table lists as accepted, each as a page reads it
// from its address once the query string is decoded.
const acceptedReturnUrls = ['/%2F%2Fevil.example', '/posts?q=grace#comments'];

// The inputs the same table lists as refused, read the same way.
const refusedReturnUrls = [
    '//evil.example',
    '/\\evil.example',
    'https://evil.example',
    'javascript:alert(1)',
    '/	/evil.example'
];

describe('useSignInReturn', () => {
    it('should send the reader on to a local return address with its path, query and fragment', () => {
        // given
        const { result } = renderSignInReturn();

        // when
        act(() => result.current.signInReturn('/posts?q=grace#comments'));

        // then
        expect(result.current.location.pathname).toBe('/posts');
        expect(result.current.location.search).toBe('?q=grace');
        expect(result.current.location.hash).toBe('#comments');
    });

    it('should send the reader on unchanged to every local return address', () => {
        for (const returnUrl of acceptedReturnUrls) {
            // given
            const { result, unmount } = renderSignInReturn();

            // when
            act(() => result.current.signInReturn(returnUrl));

            // then
            expect(landedOn(result.current.location)).toBe(returnUrl);
            unmount();
        }
    });

    it('should send the reader to the home page when there is no return address', () => {
        for (const returnUrl of [null, undefined]) {
            // given
            const { result, unmount } = renderSignInReturn();

            // when
            act(() => result.current.signInReturn(returnUrl));

            // then
            expect(landedOn(result.current.location)).toBe('/');
            unmount();
        }
    });

    it('should send the reader to the home page when the return address is empty', () => {
        // given
        const { result } = renderSignInReturn();

        // when
        act(() => result.current.signInReturn(''));

        // then
        expect(landedOn(result.current.location)).toBe('/');
    });

    it('should send the reader to the home page when the return address is a full address', () => {
        // given
        const { result } = renderSignInReturn();

        // when
        act(() => result.current.signInReturn(`${window.location.origin}/posts`));

        // then
        expect(landedOn(result.current.location)).toBe('/');
    });

    it('should send the reader to the home page when the return address is not local', () => {
        for (const returnUrl of refusedReturnUrls) {
            // given
            const { result, unmount } = renderSignInReturn();

            // when
            act(() => result.current.signInReturn(returnUrl));

            // then
            expect(landedOn(result.current.location)).toBe('/');
            unmount();
        }
    });

    it('should send the reader to the home page when the return address cannot be resolved', () => {
        // given
        const { result } = renderSignInReturn();

        // when
        const sendOn = () => act(() => result.current.signInReturn('//['));

        // then
        expect(sendOn).not.toThrow();
        expect(landedOn(result.current.location)).toBe('/');
    });
});
