import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { AxiosError, AxiosHeaders, AxiosResponse } from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Contribute } from './contribute';
import { AuthProvider } from '../components/securitys/authProvider';
import { ContentItemSetting } from '../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { ShareabilityBasis } from '../models/components/contentItems/contentItemFormItem';
import { createAuthState, signInAs } from '../tests/testAuth';

// What the page OWNS is everything the panel does not: the POST, the redirect, the notification
// and the validation readback. Each is mocked at its own boundary and asserted directly — none
// of them shows on screen except the readback.
const authState = createAuthState();
const navigate = vi.fn();
const toastError = vi.fn();
const mutateAsync = vi.fn();
let isPending = false;

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');

    return { ...actual, useNavigate: () => navigate };
});

vi.mock('../brokers/toastBroker.error', () => ({
    toastError: (message: string) => toastError(message)
}));

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemService: {
        useAddContentItem: () => ({ mutateAsync, isPending })
    }
}));

const testimonySetting: ContentItemSetting = {
    id: '11111111-1111-1111-1111-111111111111',
    contentType: ContentType.Testimony,
    contentItemId: null,
    contentTypeName: 'Testimony',
    contentTypeDescription: 'Your walk with Him',
    contentTypeIconCssClass: 'bi-chat-heart',
    sortOrder: 0,
    hasTitle: true,
    hasAuthor: false,
    isAvailableAsGeneralUserContribution: true,
    tagsAllowed: true,
    showTags: true,
    reactionsAllowed: true,
    showReactions: true,
    linksAllowed: false,
    showLinks: true,
    attachmentsAllowed: false,
    showAttachments: true,
    commentsAllowed: true,
    showComments: true,
    bibleReferenceAllowed: true,
    showBibleReferences: true,
    limitReactionsToLoveOnly: false,
    createdBy: 'system-seed',
    createdWhen: '2026-08-28T12:21:18.308+00:00',
    updatedBy: 'system-seed',
    updatedWhen: '2026-08-28T12:21:18.308+00:00',
    deletedBy: null,
    deletedWhen: null,
    isDeleted: false,
    deletionReason: null
};

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetAvailableForContribution: () => ({
            data: [testimonySetting],
            isLoading: false,
            isError: false
        })
    }
}));

// The shape a RESTFulSense controller answers a validation failure in: `title` carries the
// reason, `errors` the per-parameter messages.
const badRequestWith = (data: unknown): AxiosError => {
    const headers = new AxiosHeaders();

    const response = {
        data,
        status: 400,
        statusText: 'Bad Request',
        headers,
        config: { headers }
    } as AxiosResponse;

    const error = new AxiosError('Request failed with status code 400');
    error.response = response;

    return error;
};

const renderPage = () =>
    render(
        <MemoryRouter>
            <AuthProvider>
                <Contribute />
            </AuthProvider>
        </MemoryRouter>);

const contributeAsync = async (content: string) => {
    await userEvent.type(screen.getByLabelText(/^Testimony/), content);

    // Mandatory under the permission default the form opens on.
    await userEvent.type(
        screen.getByLabelText(/Permission details/), 'By email from the author');

    await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));
};

describe('Contribute', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        isPending = false;
        signInAs(authState);
    });

    it('should render the panel in its add surface', () => {
        // when
        renderPage();

        // then
        expect(screen.getByRole('heading', { name: 'Share what He has done' }))
            .toBeInTheDocument();

        expect(screen.getByText('What are you sharing?')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Submit for review' })).toBeInTheDocument();
    });

    it('should post only the members the API accepts from a caller', async () => {
        // given
        mutateAsync.mockResolvedValue({ id: 'content-item-1' });
        renderPage();

        // when
        await contributeAsync('He kept me through the night shift');

        // then: an untouched optional field goes as null rather than as an empty string
        await waitFor(() => expect(mutateAsync).toHaveBeenCalledWith({
            contentType: ContentType.Testimony,
            title: null,
            author: null,
            content: 'He kept me through the night shift',

            // The basis an untouched form carries: the contributor's own work, shared here by
            // their permission. The narrowest of the four offered, so a form nobody opened the
            // dropdown on has licensed this use and given nothing away.
            shareabilityBasis: ShareabilityBasis.OwnedPermissionGranted,
            sharePermission: 'By email from the author'
        }));
    });

    it('should land on the new item once it is persisted', async () => {
        // given
        mutateAsync.mockResolvedValue({ id: 'content-item-1' });
        renderPage();

        // when
        await contributeAsync('He kept me through the night shift');

        // then
        await waitFor(() => expect(navigate).toHaveBeenCalledWith('/posts/content-item-1'));
        expect(toastError).not.toHaveBeenCalled();
    });

    it('should mark the form up from the API messages and say why, staying put', async () => {
        // given
        mutateAsync.mockRejectedValue(badRequestWith({
            title: 'Content item is invalid, fix the errors and try again.',
            errors: { Content: ['Text is required'] }
        }));

        renderPage();

        // when
        await contributeAsync('x');

        // then
        await waitFor(() =>
            expect(screen.getByLabelText(/^Testimony/)).toHaveClass('is-invalid'));

        expect(screen.getByText('Text is required')).toBeInTheDocument();

        expect(toastError)
            .toHaveBeenCalledWith('Content item is invalid, fix the errors and try again.');

        expect(navigate).not.toHaveBeenCalled();
    });

    it('should still notify when the failure names no field at all', async () => {
        // given: the duplicate-content conflict, which is about the item rather than a field
        mutateAsync.mockRejectedValue(badRequestWith({
            title: 'A content item already exists with the same content.'
        }));

        renderPage();

        // when
        await contributeAsync('He kept me through the night shift');

        // then
        await waitFor(() => expect(toastError)
            .toHaveBeenCalledWith('A content item already exists with the same content.'));

        expect(screen.getByLabelText(/^Testimony/)).not.toHaveClass('is-invalid');
    });

    it('should clear a previous readback before it submits again', async () => {
        // given
        mutateAsync.mockRejectedValueOnce(badRequestWith({
            title: 'Content item is invalid, fix the errors and try again.',
            errors: { Content: ['Text is required'] }
        }));

        mutateAsync.mockResolvedValue({ id: 'content-item-1' });
        renderPage();

        await contributeAsync('x');
        await waitFor(() => expect(screen.getByText('Text is required')).toBeInTheDocument());

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Submit for review' }));

        // then
        await waitFor(() => expect(navigate).toHaveBeenCalledWith('/posts/content-item-1'));
        expect(screen.queryByText('Text is required')).not.toBeInTheDocument();
    });

    it('should leave the page when the contribution is abandoned', async () => {
        // given
        renderPage();

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

        // then
        expect(navigate).toHaveBeenCalledWith('/');
        expect(mutateAsync).not.toHaveBeenCalled();
    });

    it('should freeze the panel while the write is in flight', () => {
        // given
        isPending = true;

        // when
        renderPage();

        // then
        expect(screen.getByRole('button', { name: 'Submit for review' })).toBeDisabled();
    });
});
