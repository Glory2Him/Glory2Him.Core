import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';
import { ApprovalSettingsPage } from './approvalSettingsPage';

import {
    ApprovalSetting,
    EntityType
} from '../../models/foundations/approvalSettings/approvalSetting';

// WHAT THE TABLE SAYS A POLICY GOVERNS, and where each way out of it leads. The scope column is
// the page's whole reason for existing: the evaluation resolves the most specific row that
// applies, so a reader who cannot tell a content-type row from its entity-type default cannot
// tell which row decided anything.
let approvalSettings: ApprovalSetting[] = [];
let isLoadingApprovalSettings = false;
let isErrorLoadingApprovalSettings = false;

vi.mock('../../services/foundations/approvalSettingService', () => ({
    approvalSettingService: {
        useGetApprovalSettings: () => ({
            data: approvalSettings,
            isLoading: isLoadingApprovalSettings,
            isError: isErrorLoadingApprovalSettings
        })
    }
}));

const approvalSettingsRoute = '/Admin/ApprovalSettings';
const settingId = '11111111-1111-1111-1111-111111111111';

const createApprovalSetting = (
    overrides: Partial<ApprovalSetting> = {}): ApprovalSetting => ({
        id: settingId,
        entityType: EntityType.ContentItem,
        contentType: null,
        isPersonal: null,
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

// Where a click let go to, read off the router rather than off the screen — the origin the page
// hands on is state, and state renders nowhere.
let currentUrl = '';
let handedOn: string | undefined;

const AddressProbe = () => {
    const location = useLocation();

    currentUrl = `${location.pathname}${location.search}`;
    handedOn = (location.state as { from?: string } | null)?.from;

    return <div data-testid="left-for">{currentUrl}</div>;
};

const renderPageAt = (url: string = approvalSettingsRoute) =>
    render(
        <MemoryRouter initialEntries={[url]}>
            <Routes>
                <Route path={approvalSettingsRoute} element={<ApprovalSettingsPage />} />
                <Route path={`${approvalSettingsRoute}/New`} element={<AddressProbe />} />
                <Route
                    path={`${approvalSettingsRoute}/:approvalSettingId`}
                    element={<AddressProbe />} />
            </Routes>
        </MemoryRouter>);

describe('ApprovalSettingsPage', () => {
    beforeEach(() => {
        approvalSettings = [];
        isLoadingApprovalSettings = false;
        isErrorLoadingApprovalSettings = false;
        currentUrl = '';
        handedOn = undefined;
    });

    describe('what a row says it governs', () => {
        it('should name the entity type a policy applies to', () => {
            // given
            approvalSettings = [createApprovalSetting({ entityType: EntityType.Comment })];

            // when
            renderPageAt();

            // then
            expect(within(screen.getByRole('table')).getByText('Comment'))
                .toBeInTheDocument();
        });

        it('should call a row with no content type the entity type default', () => {
            // given: null content type is the tier every content type falls back to
            approvalSettings = [createApprovalSetting({ contentType: null })];

            // when
            renderPageAt();

            // then
            expect(screen.getByText('Default for the entity type')).toBeInTheDocument();
        });

        it('should name the content type a narrower row overrides that default for', () => {
            // given
            approvalSettings = [createApprovalSetting({ contentType: ContentType.Testimony })];

            // when
            renderPageAt();

            // then
            expect(screen.getByText('Testimony')).toBeInTheDocument();
            expect(screen.queryByText('Default for the entity type')).not.toBeInTheDocument();
        });

        it('should call the row with no entity type the global default', () => {
            // given: the tier every entity-type default narrows
            approvalSettings = [createApprovalSetting({ entityType: null })];

            // when
            renderPageAt();

            // then
            expect(screen.getByText('Every entity type')).toBeInTheDocument();
            expect(screen.getByText('The global default')).toBeInTheDocument();
        });

        it('should say which associations a personality row governs', () => {
            // given
            approvalSettings = [
                createApprovalSetting({
                    entityType: EntityType.Association,
                    isPersonal: true
                }),
                createApprovalSetting({
                    id: '22222222-2222-2222-2222-222222222222',
                    entityType: EntityType.Association,
                    isPersonal: false
                })
            ];

            // when
            renderPageAt();

            // then
            expect(screen.getByText('Personal associations only')).toBeInTheDocument();
            expect(screen.getByText('Editorial associations only')).toBeInTheDocument();
        });

        it('should say how many approvals a policy requires', () => {
            // given
            approvalSettings = [createApprovalSetting({ requiredNumberOfApprovals: 3 })];

            // when
            renderPageAt();

            // then
            expect(screen.getByText('3 required')).toBeInTheDocument();
        });

        // NOT REQUIRED IS NOT ZERO REQUIRED. A count beside a policy that asks for no approvals
        // reads as a threshold nobody has met yet, when nothing is actually being waited on.
        it('should say approvals are not required rather than show the count behind that', () => {
            // given
            approvalSettings = [createApprovalSetting({
                requireApprovals: false,
                requiredNumberOfApprovals: 2
            })];

            // when
            renderPageAt();

            // then
            expect(screen.getByText('Not required')).toBeInTheDocument();
            expect(screen.queryByText('2 required')).not.toBeInTheDocument();
        });

        // EVERY GATE THE DETAIL PAGE EDITS, LISTED HERE. A summary that omits one reads as a
        // full account of the effective policy while a gate nobody can see is holding items
        // shut — the zero-score gate is editable on the detail page and was the one missing.
        it('should show a pill for every gate the policy can hold an item shut with', () => {
            // given
            approvalSettings = [createApprovalSetting()];

            // when
            renderPageAt();

            // then
            const table = within(screen.getByRole('table'));

            [
                'Auto-approve',
                'Self-approval',
                'Blocks on reject',
                'Blocks on zero score',
                'Comments resolved',
                'Re-approve on change',
                'No bypass'
            ].forEach(gate => expect(table.getByText(gate)).toBeInTheDocument());
        });

        // The pill is always drawn; only its styling says which way the gate is set, so on and
        // off are pinned in both directions rather than by presence alone.
        it('should show the zero score gate as on when the policy blocks on it', () => {
            // given
            approvalSettings = [createApprovalSetting({ blockOnZeroApprovalScore: true })];

            // when
            renderPageAt();

            // then
            expect(screen.getByText('Blocks on zero score')).toHaveClass('bg-primary-subtle');
        });

        it('should show the zero score gate as off when the policy does not block on it', () => {
            // given
            approvalSettings = [createApprovalSetting({ blockOnZeroApprovalScore: false })];

            // when
            renderPageAt();

            // then
            expect(screen.getByText('Blocks on zero score')).toHaveClass('bg-body-secondary');
        });
    });

    describe('an empty section', () => {
        // Nothing seeds approval settings, so empty is the honest first state of this page and
        // must not be dressed as a failure the administrator should report.
        it('should read as no policy yet rather than as an error', () => {
            // when
            renderPageAt();

            // then
            expect(screen.getByRole('alert')).toHaveTextContent('No approval settings yet');
            expect(screen.queryByRole('table')).not.toBeInTheDocument();
        });

        it('should still offer the way to create the first one', () => {
            // when
            renderPageAt();

            // then
            expect(screen.getByRole('button', { name: /New approval setting/ }))
                .toBeInTheDocument();
        });

        it('should say a read failed when the read actually failed', () => {
            // given
            isErrorLoadingApprovalSettings = true;

            // when
            renderPageAt();

            // then
            expect(screen.getByRole('alert'))
                .toHaveTextContent('We could not load the approval settings right now.');
        });
    });

    describe('the ways out', () => {
        it('should hand the create page somewhere to come back to', async () => {
            // given
            renderPageAt();

            // when
            await userEvent.click(
                screen.getByRole('button', { name: /New approval setting/ }));

            // then
            expect(currentUrl).toBe(`${approvalSettingsRoute}/New`);
            expect(handedOn).toBe(approvalSettingsRoute);
        });

        it('should open the row Manage was taken on', async () => {
            // given
            approvalSettings = [createApprovalSetting()];
            renderPageAt();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Manage' }));

            // then
            expect(currentUrl).toBe(`${approvalSettingsRoute}/${settingId}`);
            expect(handedOn).toBe(approvalSettingsRoute);
        });
    });
});
