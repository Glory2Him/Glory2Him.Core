import { ReactNode } from 'react';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { act, renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { useSignIn } from './useSignIn';

// The hook is rendered beside useLocation in the same router, so the test reads where the
// reader was sent from the router itself rather than from a mocked navigate.
const renderSignIn = (initialEntry: string) => {
    const wrapper = ({ children }: { children: ReactNode }) => (
        <MemoryRouter initialEntries={[initialEntry]}>{children}</MemoryRouter>
    );

    return renderHook(() => ({ signIn: useSignIn(), location: useLocation() }), { wrapper });
};

describe('useSignIn', () => {
    it('should send the reader to sign in with the path, the query and the fragment they left', () => {
        // given
        const { result } = renderSignIn('/posts?q=grace#comments');

        // when
        act(() => result.current.signIn());

        // then
        expect(result.current.location.pathname).toBe('/Account/Login');
        expect(result.current.location.hash).toBe('');
        expect(result.current.location.search).toBe(`?returnUrl=${encodeURIComponent('/posts?q=grace#comments')}`);
        expect(new URLSearchParams(result.current.location.search).get('returnUrl')).toBe('/posts?q=grace#comments');
    });

    it('should send a reader on the home page back to the home page', () => {
        // given
        const { result } = renderSignIn('/');

        // when
        act(() => result.current.signIn());

        // then
        expect(result.current.location.pathname).toBe('/Account/Login');
        expect(new URLSearchParams(result.current.location.search).get('returnUrl')).toBe('/');
    });
});
