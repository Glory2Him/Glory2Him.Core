import { AxiosError, AxiosHeaders } from 'axios';
import { Link, MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DeletePersonalData } from './deletePersonalData';

interface DeletePersonalDataCallbacks {
    onSuccess: () => void;
    onError: (error: unknown) => void;
}

const mocks = vi.hoisted(() => ({
    deletePersonalDataMutate: vi.fn(),
    replace: vi.fn(),
    assign: vi.fn(),
    reload: vi.fn(),
    setHref: vi.fn(),
    pageShown: false
}));

// Behaves as TanStack Query does: the callbacks handed to mutate run only while the page is
// still rendered, and the promise mutateAsync returns settles whether or not it is.
vi.mock('../../../services/foundations/manageAccountService', async () => {
    const { useEffect } = await import('react');

    return {
        manageAccountService: {
            useGetPersonalDataInfo: () => ({ data: { requirePassword: true } }),
            useDeletePersonalData: () => {
                useEffect(() => {
                    mocks.pageShown = true;

                    return () => { mocks.pageShown = false; };
                }, []);

                return {
                    mutate: (password: string, callbacks: DeletePersonalDataCallbacks) =>
                        mocks.deletePersonalDataMutate(password, {
                            onSuccess: () => { if (mocks.pageShown) callbacks.onSuccess(); },
                            onError: (error: unknown) => { if (mocks.pageShown) callbacks.onError(error); }
                        }),
                    mutateAsync: (password: string) => new Promise<void>((resolve, reject) =>
                        mocks.deletePersonalDataMutate(password, {
                            onSuccess: () => resolve(),
                            onError: reject
                        })),
                    isPending: false
                };
            }
        }
    };
});

const deletePersonalDataPath = '/Account/Manage/DeletePersonalData';
const elsewherePath = '/Account/Manage';

// Rendered beside the routes, so the test reads where the reader is from the router itself.
const Landed = () => {
    const { pathname } = useLocation();

    return <output data-testid="landed">{pathname}</output>;
};

const renderDeletePersonalData = () => render(
    <MemoryRouter initialEntries={[deletePersonalDataPath]}>
        <Routes>
            <Route path={deletePersonalDataPath} element={<DeletePersonalData />} />
            <Route path="*" element={null} />
        </Routes>
        <Landed />
        <Link to={elsewherePath}>Elsewhere</Link>
    </MemoryRouter>);

const leaveThePage = () => fireEvent.click(screen.getByRole('link', { name: 'Elsewhere' }));

const landedOn = () => screen.getByTestId('landed').textContent;

// Records the four members of the global location a page normally loads another document
// through — replace, assign, reload and setting href. It does not see document.location,
// a reassigned window.location or a router reload such as navigate(0).
const stubLocation = () => {
    const realHref = window.location.href;

    const location = {
        replace: mocks.replace,
        assign: mocks.assign,
        reload: mocks.reload
    };

    Object.defineProperty(location, 'href', {
        get: () => realHref,
        set: mocks.setHref
    });

    vi.stubGlobal('location', location);
};

// Awaited inside act, so the page has handled however the deletion settled before the test asserts.
const submitDeletion = async () => {
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'P@ssw0rd!' } });
    await act(async () => {
        fireEvent.click(screen.getByRole('button', { name: 'Delete data and close my account' }));
    });
};

const refusedWith = (message: string) => new AxiosError(
    'Request failed', 'ERR_BAD_REQUEST', undefined, undefined, {
        status: 400,
        statusText: 'Bad Request',
        headers: {},
        config: { headers: new AxiosHeaders() },
        data: { message }
    });

describe('DeletePersonalData', () => {
    beforeEach(() => {
        mocks.deletePersonalDataMutate.mockReset();
        mocks.replace.mockReset();
        mocks.assign.mockReset();
        mocks.reload.mockReset();
        mocks.setHref.mockReset();
        stubLocation();
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('should load the home page afresh once the account is deleted', async () => {
        // given
        mocks.deletePersonalDataMutate.mockImplementation(
            (_password: string, callbacks: DeletePersonalDataCallbacks) => callbacks.onSuccess());

        renderDeletePersonalData();

        // when
        await submitDeletion();

        // then
        expect(mocks.replace).toHaveBeenCalledOnce();
        expect(mocks.replace).toHaveBeenCalledWith('/');
        expect(landedOn()).toBe(deletePersonalDataPath);
    });

    it('should stay on the page and show the message when the deletion fails', async () => {
        // given
        const failure = new AxiosError('Request failed', 'ERR_BAD_REQUEST', undefined, undefined, {
            status: 400,
            statusText: 'Bad Request',
            headers: {},
            config: { headers: new AxiosHeaders() },
            data: { message: 'Error: Incorrect password.' }
        });

        mocks.deletePersonalDataMutate.mockImplementation(
            (_password: string, callbacks: DeletePersonalDataCallbacks) => callbacks.onError(failure));

        renderDeletePersonalData();

        // when
        await submitDeletion();

        // then
        expect(mocks.replace).not.toHaveBeenCalled();
        expect(mocks.assign).not.toHaveBeenCalled();
        expect(mocks.reload).not.toHaveBeenCalled();
        expect(mocks.setHref).not.toHaveBeenCalled();
        expect(landedOn()).toBe(deletePersonalDataPath);
        expect(screen.getByRole('heading', { name: 'Delete Personal Data' })).toBeTruthy();
        expect(screen.getByText('Error: Incorrect password.')).toBeTruthy();
    });

    it("should show the server's refusal as a failure when the deletion is refused", async () => {
        // given
        const refusal = 'This is the last administrator holding the "Administrators" role, '
            + 'so it cannot be deleted. Give another account the "Administrators" role first.';

        mocks.deletePersonalDataMutate.mockImplementation(
            (_password: string, callbacks: DeletePersonalDataCallbacks) =>
                callbacks.onError(refusedWith(refusal)));

        renderDeletePersonalData();

        // when
        await submitDeletion();

        // then
        const alert = screen.getByText(`Error: ${refusal}`);

        expect(alert.className).toBe('alert alert-danger');
    });

    it('should show a refusal already marked as a failure unchanged when the deletion is refused', async () => {
        // given
        mocks.deletePersonalDataMutate.mockImplementation(
            (_password: string, callbacks: DeletePersonalDataCallbacks) =>
                callbacks.onError(refusedWith('Error: Incorrect password.')));

        renderDeletePersonalData();

        // when
        await submitDeletion();

        // then
        const alert = screen.getByText('Error: Incorrect password.');

        expect(alert.className).toBe('alert alert-danger');
        expect(screen.queryByText(/Error: Error:/)).toBeNull();
    });

    it('should show its own message as a failure when the deletion fails without one', async () => {
        // given
        const failure = new AxiosError('Request failed', 'ERR_BAD_REQUEST', undefined, undefined, {
            status: 401,
            statusText: 'Unauthorized',
            headers: {},
            config: { headers: new AxiosHeaders() },
            data: ''
        });

        mocks.deletePersonalDataMutate.mockImplementation(
            (_password: string, callbacks: DeletePersonalDataCallbacks) => callbacks.onError(failure));

        renderDeletePersonalData();

        // when
        await submitDeletion();

        // then
        const alert = screen.getByText('Error: Unexpected error occurred deleting user.');

        expect(alert.className).toBe('alert alert-danger');
        expect(screen.queryByText(/Error: Error:/)).toBeNull();
    });

    it('should load the home page afresh when the deletion succeeds after the reader has left the page', async () => {
        // given
        let settle: DeletePersonalDataCallbacks | undefined;

        mocks.deletePersonalDataMutate.mockImplementation(
            (_password: string, callbacks: DeletePersonalDataCallbacks) => { settle = callbacks; });

        renderDeletePersonalData();
        await submitDeletion();
        leaveThePage();

        // when
        await act(async () => settle?.onSuccess());

        // then
        expect(landedOn()).toBe(elsewherePath);
        expect(mocks.replace).toHaveBeenCalledOnce();
        expect(mocks.replace).toHaveBeenCalledWith('/');
    });

    it('should load nothing when the deletion fails after the reader has left the page', async () => {
        // given
        let settle: DeletePersonalDataCallbacks | undefined;

        mocks.deletePersonalDataMutate.mockImplementation(
            (_password: string, callbacks: DeletePersonalDataCallbacks) => { settle = callbacks; });

        renderDeletePersonalData();
        await submitDeletion();
        leaveThePage();

        // when
        await act(async () => settle?.onError(refusedWith('Error: Incorrect password.')));

        // then
        expect(mocks.replace).not.toHaveBeenCalled();
        expect(mocks.assign).not.toHaveBeenCalled();
        expect(mocks.reload).not.toHaveBeenCalled();
        expect(mocks.setHref).not.toHaveBeenCalled();
        expect(landedOn()).toBe(elsewherePath);
    });
});
