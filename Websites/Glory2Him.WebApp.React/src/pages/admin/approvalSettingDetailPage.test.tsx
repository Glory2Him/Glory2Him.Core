import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';
import { ApprovalSettingDetailPage } from './approvalSettingDetailPage';

import {
    ApprovalSetting,
    EntityType
} from '../../models/foundations/approvalSettings/approvalSetting';

// ONE POLICY ROW, WRITTEN OR AMENDED. Two things are asserted here: that a create carries an id
// of its own — the service refuses an empty Guid and mints none — and that a scope the database
// would refuse can never be assembled on screen, because that refusal comes back as a dependency
// failure with no field to hang it on.
let approvalSetting: ApprovalSetting | null = null;
let isLoadingApprovalSetting = false;
let isErrorLoadingApprovalSetting = false;
let saveOutcome: 'succeeds' | 'refuses' = 'succeeds';
const added = vi.fn();
const updated = vi.fn();

vi.mock('../../services/foundations/approvalSettingService', () => ({
    approvalSettingService: {
        useGetApprovalSettingById: () => ({
            data: approvalSetting,
            isLoading: isLoadingApprovalSetting,
            isError: isErrorLoadingApprovalSetting
        }),

        useAddApprovalSetting: () => ({
            isPending: false,

            mutateAsync: async (created: ApprovalSetting) => {
                added(created);

                if (saveOutcome === 'refuses') {
                    throw new Error('refused');
                }

                return created;
            }
        }),

        useUpdateApprovalSetting: () => ({
            isPending: false,

            mutateAsync: async (amended: ApprovalSetting) => {
                updated(amended);

                if (saveOutcome === 'refuses') {
                    throw new Error('refused');
                }

                return amended;
            }
        })
    }
}));

const settingId = '11111111-1111-1111-1111-111111111111';
const mintedId = '99999999-9999-9999-9999-999999999999';
const approvalSettingsRoute = '/Admin/ApprovalSettings';
const listRoute = `${approvalSettingsRoute}?page=2`;

const createApprovalSetting = (
    overrides: Partial<ApprovalSetting> = {}): ApprovalSetting => ({
        id: settingId,
        entityType: EntityType.ContentItem,
        contentType: ContentType.Testimony,
        requireApprovals: true,
        requiredNumberOfApprovals: 2,
        autoApproveIfAllApprovalRequirementsMet: false,
        allowSelfApproval: false,
        blockOnReject: true,
        blockOnZeroApprovalScore: false,
        requireReapprovalOnChange: true,
        requireReviewCommentResolutionBeforeApprovals: true,
        doNotAllowBypassingSettings: false,
        createdBy: 'admin',
        createdWhen: '2026-09-01T09:00:00.000+00:00',
        updatedBy: 'admin',
        updatedWhen: '2026-09-01T09:00:00.000+00:00',
        isDeleted: false,
        ...overrides
    });

const ListStub = () => {
    const location = useLocation();

    return <div data-testid="list">{`${location.pathname}${location.search}`}</div>;
};

const renderCreatePage = (from?: string) =>
    render(
        <MemoryRouter
            initialEntries={[{
                pathname: `${approvalSettingsRoute}/New`,
                state: from == null ? null : { from }
            }]}>
            <Routes>
                <Route path={approvalSettingsRoute} element={<ListStub />} />
                <Route
                    path={`${approvalSettingsRoute}/New`}
                    element={<ApprovalSettingDetailPage isNew />} />
            </Routes>
        </MemoryRouter>);

const renderEditPage = (from?: string) =>
    render(
        <MemoryRouter
            initialEntries={[{
                pathname: `${approvalSettingsRoute}/${settingId}`,
                state: from == null ? null : { from }
            }]}>
            <Routes>
                <Route path={approvalSettingsRoute} element={<ListStub />} />
                <Route
                    path={`${approvalSettingsRoute}/:approvalSettingId`}
                    element={<ApprovalSettingDetailPage />} />
            </Routes>
        </MemoryRouter>);

// FormSwitch draws its label beside the input rather than bound to it, so a switch is reached
// through the form-check it lives in rather than by label text.
const switchFor = (label: string): HTMLInputElement => {
    const input = screen.getByText(label)
        .closest('.form-check')
        ?.querySelector('input');

    if (input == null) {
        throw new Error(`No switch found for "${label}".`);
    }

    return input as HTMLInputElement;
};

describe('ApprovalSettingDetailPage', () => {
    beforeEach(() => {
        approvalSetting = createApprovalSetting();
        isLoadingApprovalSetting = false;
        isErrorLoadingApprovalSetting = false;
        saveOutcome = 'succeeds';
        added.mockReset();
        updated.mockReset();

        // The id a create mints. Stubbed so the write can be asserted against a known value
        // rather than only against "not empty".
        vi.stubGlobal('crypto', { randomUUID: () => mintedId });
    });

    afterEach(() => vi.unstubAllGlobals());

    describe('writing a new policy', () => {
        // THE CALLER MINTS THE ID. The service refuses an empty Guid and never generates one,
        // so a create that sends nothing is a 400 rather than a row.
        it('should carry an id of its own into the create', async () => {
            // given
            renderCreatePage(listRoute);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(
                    expect.objectContaining({ id: mintedId })));

            expect(updated).not.toHaveBeenCalled();
        });

        it('should open on one required approval rather than on none', () => {
            // given: the shape §8.5 evaluates against, and the least surprising default
            renderCreatePage();

            // then
            expect(screen.getByLabelText('How many')).toHaveValue(1);
            expect(switchFor('Approving reviews are required')).toBeChecked();
        });

        it('should let the scope be chosen while the row is still being written', () => {
            // when
            renderCreatePage();

            // then
            expect(screen.getByLabelText('Entity type')).toBeInstanceOf(HTMLSelectElement);
            expect(screen.getByLabelText('Content type')).toBeInstanceOf(HTMLSelectElement);
        });

        it('should write the chosen scope rather than the default it opened on', async () => {
            // given
            renderCreatePage();

            // when
            await userEvent.selectOptions(
                screen.getByLabelText('Content type'), String(ContentType.Quote));

            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    entityType: EntityType.ContentItem,
                    contentType: ContentType.Quote
                })));
        });

        // ONLY A CONTENT ITEM CARRIES A CONTENT TYPE (§8.4), and a SQL CHECK constraint is what
        // enforces it — so a bad pair comes back as a dependency failure naming no field. The
        // form clears it instead of explaining it afterwards.
        it('should drop a content type that the chosen entity type cannot carry', async () => {
            // given
            renderCreatePage();

            await userEvent.selectOptions(
                screen.getByLabelText('Content type'), String(ContentType.Quote));

            // when
            await userEvent.selectOptions(
                screen.getByLabelText('Entity type'), String(EntityType.Comment));

            // then
            expect(screen.queryByLabelText('Content type')).not.toBeInstanceOf(
                HTMLSelectElement);

            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    entityType: EntityType.Comment,
                    contentType: null
                })));
        });

        it('should say why only some entity types are scoped by content type', async () => {
            // given
            renderCreatePage();

            // when
            await userEvent.selectOptions(
                screen.getByLabelText('Entity type'), String(EntityType.Tag));

            // then
            expect(screen.getByText('Only content items are scoped by content type.'))
                .toBeInTheDocument();
        });
    });

    describe('amending a policy that exists', () => {
        // MOVING A ROW TO ANOTHER SCOPE IS WRITING A DIFFERENT POLICY, and the filtered unique
        // indexes would refuse it as a duplicate — so the scope is read back, not offered.
        it('should show the scope without offering to change it', () => {
            // when
            renderEditPage();

            // then
            expect(screen.queryByLabelText('Entity type')).not.toBeInstanceOf(
                HTMLSelectElement);

            expect(screen.getByText('Content item')).toBeInTheDocument();
            expect(screen.getByText('Testimony')).toBeInTheDocument();
        });

        // THE WHOLE ROW GOES BACK. The foundation compares CreatedBy and CreatedWhen against
        // storage before it will accept the write, so an edit is the fetched row with the policy
        // changed — never a fresh object.
        it('should send the audit fields back beside the change', async () => {
            // given
            renderEditPage(listRoute);

            // when
            await userEvent.click(switchFor('Allow the contributor to approve their own work'));
            await userEvent.click(screen.getByRole('button', { name: 'Save settings' }));

            // then
            await waitFor(() =>
                expect(updated).toHaveBeenCalledWith(expect.objectContaining({
                    id: settingId,
                    allowSelfApproval: true,
                    createdBy: 'admin',
                    createdWhen: '2026-09-01T09:00:00.000+00:00'
                })));

            expect(added).not.toHaveBeenCalled();
        });

        it('should restore the stored row when the edit is reset', async () => {
            // given
            renderEditPage();
            await userEvent.click(switchFor('A rejected review blocks approval'));
            expect(switchFor('A rejected review blocks approval')).not.toBeChecked();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Reset' }));

            // then
            expect(switchFor('A rejected review blocks approval')).toBeChecked();
        });

        it('should say the row could not be read rather than show an empty form', () => {
            // given
            approvalSetting = null;
            isErrorLoadingApprovalSetting = true;

            // when
            renderEditPage();

            // then
            expect(screen.getByRole('alert'))
                .toHaveTextContent('We could not load this approval setting right now.');

            expect(screen.queryByRole('button', { name: 'Save settings' }))
                .not.toBeInTheDocument();
        });
    });

    describe('the ways out', () => {
        it('should return to the view the page was opened from', async () => {
            // given
            renderEditPage(listRoute);

            // when
            await userEvent.click(
                screen.getByRole('button', { name: /Back to Approval Settings/ }));

            // then
            expect(screen.getByTestId('list')).toHaveTextContent(listRoute);
        });

        it('should fall back to the bare list when there is no origin to honour', async () => {
            // given: a pasted link, a refresh
            renderEditPage();

            // when
            await userEvent.click(
                screen.getByRole('button', { name: /Back to Approval Settings/ }));

            // then
            expect(screen.getByTestId('list')).toHaveTextContent(approvalSettingsRoute);
        });

        it('should leave the same way Back does once the save goes through', async () => {
            // given
            renderEditPage(listRoute);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Save settings' }));

            // then
            await waitFor(() =>
                expect(screen.getByTestId('list')).toHaveTextContent(listRoute));
        });

        // A DUPLICATE SCOPE IS A 409 the server is the authority on, and the page has to be
        // still standing to say so.
        it('should hold the reader on the form when the save is refused', async () => {
            // given
            saveOutcome = 'refuses';
            renderCreatePage(listRoute);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            expect(await screen.findByRole('alert'))
                .toHaveTextContent('A setting may already exist for this scope.');

            expect(screen.queryByTestId('list')).not.toBeInTheDocument();
        });
    });
});
