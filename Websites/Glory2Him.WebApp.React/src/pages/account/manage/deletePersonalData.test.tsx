import { AxiosError, AxiosHeaders } from 'axios';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { fireEvent, render, screen } from '@testing-library/react';
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
    setHref: vi.fn()
}));

vi.mock('../../../services/foundations/manageAccountService', () => ({
    manageAccountService: {
        useGetPersonalDataInfo: () => ({ data: { requirePassword: true } }),
        useDeletePersonalData: () => ({ mutate: mocks.deletePersonalDataMutate, isPending: false })
    }
}));

const deletePersonalDataPath = '/Account/Manage/DeletePersonalData';

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
    </MemoryRouter>);

const landedOn = () => screen.getByTestId('landed').textContent;

// Records every way the page could load another document, so none of them goes unseen.
const stubLocation = () => {
    const location = {
        ...window.location,
        replace: mocks.replace,
        assign: mocks.assign,
        reload: mocks.reload
    };

    Object.defineProperty(location, 'href', {
        get: () => window.location.href,
        set: mocks.setHref
    });

    vi.stubGlobal('location', location);
};

const submitDeletion = () => {
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'P@ssw0rd!' } });
    fireEvent.click(screen.getByRole('button', { name: 'Delete data and close my account' }));
};

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

    it('should load the home page afresh once the account is deleted', () => {
        // given
        mocks.deletePersonalDataMutate.mockImplementation(
            (_password: string, callbacks: DeletePersonalDataCallbacks) => callbacks.onSuccess());

        renderDeletePersonalData();

        // when
        submitDeletion();

        // then
        expect(mocks.replace).toHaveBeenCalledOnce();
        expect(mocks.replace).toHaveBeenCalledWith('/');
        expect(landedOn()).toBe(deletePersonalDataPath);
    });

    it('should stay on the page and show the message when the deletion fails', () => {
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
        submitDeletion();

        // then
        expect(mocks.replace).not.toHaveBeenCalled();
        expect(mocks.assign).not.toHaveBeenCalled();
        expect(mocks.reload).not.toHaveBeenCalled();
        expect(mocks.setHref).not.toHaveBeenCalled();
        expect(landedOn()).toBe(deletePersonalDataPath);
        expect(screen.getByRole('heading', { name: 'Delete Personal Data' })).toBeTruthy();
        expect(screen.getByText('Error: Incorrect password.')).toBeTruthy();
    });
});
