import { useMemo } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { Card } from '../../components/coreUI/card';
import { DataTable } from '../../components/coreUI/dataTable';
import { Spinner } from '../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { DataTableColumn } from '../../models/coreUI/dataTableColumn';
import { approvalSettingService } from '../../services/foundations/approvalSettingService';
import { contentTypeLabels } from '../../models/foundations/contentItemSettings/contentType';
import { useDocumentTitle } from '../useDocumentTitle';

import {
    ApprovalSetting,
    entityTypeLabels
} from '../../models/foundations/approvalSettings/approvalSetting';

// THE APPROVAL POLICY, LISTED. Each row is one §8.4 setting: how many approvals a thing needs
// before it can be approved, and which gates hold it shut until they are met. The evaluation
// resolves the most specific row that applies — a content-type row beats its entity-type
// default — so this table is read as a hierarchy rather than a flat list, and the scope column
// says which tier each row is.
//
// CLIENT-SIDE PAGING, unlike the content item settings list beside it. That one pages against
// the server because a content item override exists per ITEM and the set is unbounded; this set
// is bounded by entity types times content types, so it is read whole and DataTable pages it.
//
// CREATE IS THE PRIMARY ACTION HERE, which is the other thing that differs. Content item
// settings are seeded, so that page only ever edits; nothing seeds approval settings, so this
// one opens empty and the first useful thing it does is make a row.
const approvalSettingsRoute = '/Admin/ApprovalSettings';

const crumbs: BreadcrumbItem[] = [
    { title: 'Admin' },
    { title: 'Approval Settings', href: approvalSettingsRoute, isActive: true },
];

const scopeOf = (approvalSetting: ApprovalSetting): string =>
    approvalSetting.contentType == null
        ? 'Default for the entity type'
        : contentTypeLabels[approvalSetting.contentType] ?? 'Content type';

const featurePill = (label: string, isOn: boolean) => (
    <span
        key={label}
        className={`badge rounded-pill fw-normal me-1 ${isOn
            ? 'bg-primary-subtle text-primary-emphasis'
            : 'bg-body-secondary text-body-tertiary'}`}>
        {label}
    </span>
);

export const ApprovalSettingsPage = () => {
    useDocumentTitle('Approval Settings — Admin — Glory 2 Him');

    const navigate = useNavigate();
    const location = useLocation();

    const { data: approvalSettings, isLoading, isError } =
        approvalSettingService.useGetApprovalSettings();

    // The origin travels so the detail page's Back returns here rather than guessing at history.
    const from = `${location.pathname}${location.search}`;

    const manageApprovalSetting = (approvalSettingId: string) =>
        navigate(`${approvalSettingsRoute}/${approvalSettingId}`, { state: { from } });

    const createApprovalSetting = () =>
        navigate(`${approvalSettingsRoute}/New`, { state: { from } });

    // value drives DataTable's search and sort; cellTemplate draws the cell. Both are given
    // so a moderator can type "Quote" or "Devotional" and find the row that governs it.
    const columns: ReadonlyArray<DataTableColumn<ApprovalSetting>> = useMemo(() => [
        {
            title: 'Applies to',
            sortable: true,
            value: (approvalSetting) =>
                `${entityTypeLabels[approvalSetting.entityType] ?? 'Unknown'} `
                    + scopeOf(approvalSetting),
            cellTemplate: (approvalSetting) => (
                <>
                    <div className="fw-semibold">
                        {entityTypeLabels[approvalSetting.entityType] ?? 'Unknown'}
                    </div>
                    <div className="small text-body-secondary">
                        {scopeOf(approvalSetting)}
                    </div>
                </>
            )
        },
        {
            title: 'Approvals',
            sortable: true,
            value: (approvalSetting) => approvalSetting.requireApprovals
                ? approvalSetting.requiredNumberOfApprovals
                : 0,
            cellTemplate: (approvalSetting) => approvalSetting.requireApprovals
                ? (
                    <span className="badge text-bg-primary">
                        {approvalSetting.requiredNumberOfApprovals} required
                    </span>
                )
                : <span className="badge text-bg-secondary">Not required</span>
        },
        {
            title: 'Gates',
            value: () => '',
            cellTemplate: (approvalSetting) => (
                <>
                    {featurePill('Auto-approve', approvalSetting
                        .autoApproveIfAllApprovalRequirementsMet)}
                    {featurePill('Self-approval', approvalSetting.allowSelfApproval)}
                    {featurePill('Blocks on reject', approvalSetting.blockOnReject)}
                    {featurePill('Comments resolved', approvalSetting
                        .requireReviewCommentResolutionBeforeApprovals)}
                    {featurePill('Re-approve on change', approvalSetting
                        .requireReapprovalOnChange)}
                    {featurePill('No bypass', approvalSetting.doNotAllowBypassingSettings)}
                </>
            )
        }
    ], []);

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">Approval Settings</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            <p className="text-body-secondary">
                One row per scope: how many approving reviews something needs, and which
                conditions hold approval shut until they are met. A row naming a content type
                beats the entity type&rsquo;s own default for that type; everything else falls
                back to the default.
            </p>

            <div className="d-flex justify-content-end mb-3">
                <Button color="primary" onClick={createApprovalSetting}>
                    <i className="bi bi-plus-lg me-1" aria-hidden="true"></i>
                    New approval setting
                </Button>
            </div>

            <Card>
                {isLoading ? (
                    <div className="text-center py-5"><Spinner /></div>
                ) : isError ? (
                    <div className="alert alert-danger mb-0" role="alert">
                        We could not load the approval settings right now. Please try again later.
                    </div>
                ) : (approvalSettings?.length ?? 0) === 0 ? (
                    // Not a failure, and said as such: nothing seeds these, so an empty table is
                    // the honest first state rather than a sign something went wrong.
                    <div className="alert alert-info mb-0" role="alert">
                        No approval settings yet. Until one exists, nothing is required before an
                        item can be approved.
                    </div>
                ) : (
                    <DataTable
                        items={approvalSettings ?? []}
                        columns={columns}
                        pageSize={10}
                        rowActions={(approvalSetting: ApprovalSetting) => (
                            <Button
                                color="outline-primary"
                                cssClass="btn-sm"
                                onClick={() => manageApprovalSetting(approvalSetting.id)}>
                                Manage
                            </Button>
                        )} />
                )}
            </Card>
        </>
    );
};
