import { ReactNode } from 'react';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { act, renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { useSignInReturn } from './useSignInReturn';

// The hook is rendered beside useLocation in the same router, so the test reads where the
// signed-in reader landed from the router itself rather than from a mocked navigate. The reader
// starts on the sign-in page, so landing on the home page always means they were sent there.
const renderSignInReturn = () => {
    const wrapper = ({ children }: { children: ReactNode }) => (
        <MemoryRouter initialEntries={['/Account/Login']}>{children}</MemoryRouter>
    );

    return renderHook(() => ({ signInReturn: useSignInReturn(), location: useLocation() }), { wrapper });
};

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
});
