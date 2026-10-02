import { MemoryRouter } from 'react-router-dom';
import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DeletePersonalData } from './deletePersonalData';

interface DeletePersonalDataCallbacks {
    onSuccess: () => void;
    onError: (error: unknown) => void;
}

const mocks = vi.hoisted(() => ({ deletePersonalDataMutate: vi.fn(), replace: vi.fn() }));

vi.mock('../../../services/foundations/manageAccountService', () => ({
    manageAccountService: {
        useGetPersonalDataInfo: () => ({ data: { requirePassword: true } }),
        useDeletePersonalData: () => ({ mutate: mocks.deletePersonalDataMutate, isPending: false })
    }
}));

const renderDeletePersonalData = () => render(
    <MemoryRouter initialEntries={['/Account/Manage/DeletePersonalData']}>
        <DeletePersonalData />
    </MemoryRouter>);

const submitDeletion = () => {
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'P@ssw0rd!' } });
    fireEvent.click(screen.getByRole('button', { name: 'Delete data and close my account' }));
};

describe('DeletePersonalData', () => {
    beforeEach(() => {
        mocks.deletePersonalDataMutate.mockReset();
        mocks.replace.mockReset();
        vi.stubGlobal('location', { ...window.location, replace: mocks.replace });
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
    });
});
