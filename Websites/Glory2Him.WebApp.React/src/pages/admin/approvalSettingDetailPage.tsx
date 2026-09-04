import { useEffect, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { Card } from '../../components/coreUI/card';
import { FormSwitch } from '../../components/coreUI/formSwitch';
import { Spinner } from '../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { approvalSettingService } from '../../services/foundations/approvalSettingService';
import { useDocumentTitle } from '../useDocumentTitle';
import { extractApiErrorMessage } from './apiErrorMessage';

import {
    ContentType,
    contentTypeLabels,
    contentTypeMembers
} from '../../models/foundations/contentItemSettings/contentType';

import {
    allowsContentTypeScope,
    ApprovalSetting,
    EntityType,
    entityTypeLabels,
    entityTypeMembers,
    newApprovalSetting
} from '../../models/foundations/approvalSettings/approvalSetting';

// ONE §8.4 POLICY ROW, created or amended. The same page does both, because the fields are the
// same and the only real difference is whether the scope is still the caller's to choose.
//
// SCOPE IS CREATE-ONLY. Moving a row from one scope to another is not editing this policy, it is
// writing a different one — and the two filtered unique indexes would refuse it as a duplicate
// anyway. So the pickers are live on New and read back as plain text afterwards.
//
// THE ID IS MINTED HERE. The service refuses an empty Guid and never generates one, so a create
// makes its own before anything is sent.
const approvalSettingsRoute = '/Admin/ApprovalSettings';
const newRouteSegment = 'New';

// A caller may ask for fewer approvals than one, and the foundation would refuse it — so the
// input refuses it first rather than letting the save round-trip into a 400.
const minimumRequiredApprovals = 1;

// Only the boolean members can be wired to a switch, so a mistyped field name below is a compile
// error rather than a switch that silently never moves. -? strips the optional modifier, or every
// optional member would smuggle `undefined` into the union and satisfy nothing.
type ApprovalSettingFlag = {
    [TField in keyof ApprovalSetting]-?:
    ApprovalSetting[TField] extends boolean ? TField : never
}[keyof ApprovalSetting];

type PolicyField = {
    field: ApprovalSettingFlag;
    label: string;
    help: string;
};

// The gates, in the order §8.5 reads them: what is required, then what holds it shut, then who
// may step past it.
const policyFields: ReadonlyArray<PolicyField> = [
    {
        field: 'autoApproveIfAllApprovalRequirementsMet',
        label: 'Approve automatically once every requirement is met',
        help: 'No human click. With approvals not required, this approves on submission.'
    },
    {
        field: 'allowSelfApproval',
        label: 'Allow the contributor to approve their own work',
        help: 'Off by default, and off is the safe posture: HR-2 keeps an author out of their '
            + 'own round.'
    },
    {
        field: 'blockOnReject',
        label: 'A rejected review blocks approval',
        help: 'One standing rejection holds the round shut however many approvals it has.'
    },
    {
        field: 'blockOnZeroApprovalScore',
        label: 'A zero confidence score blocks approval',
        help: 'A null score does not block — it means the confidence process has not run yet, '
            + 'not that the content was judged worthless.'
    },
    {
        field: 'requireReapprovalOnChange',
        label: 'Editing an approved item sends it back for review',
        help: 'Reviews already cast are dismissed as evidence about superseded text.'
    },
    {
        field: 'requireReviewCommentResolutionBeforeApprovals',
        label: 'Every review comment must be settled first',
        help: 'Informational comments are created settled and never hold anything shut.'
    },
    {
        field: 'doNotAllowBypassingSettings',
        label: 'Nobody may bypass these requirements',
        help: 'Shuts the bypass route to everyone, administrators included.'
    },
];

// isNew comes from the ROUTE rather than from the id, because create and edit are gated on
// different security points and a router cannot gate two things it cannot tell apart. The id
// check stays as the fallback for anything reaching the shared route directly.
export const ApprovalSettingDetailPage = ({ isNew = false }: { isNew?: boolean }) => {
    const { approvalSettingId = '' } = useParams();
    const navigate = useNavigate();
    const location = useLocation();

    const isCreating = isNew || approvalSettingId === newRouteSegment;

    const { data: approvalSetting, isLoading, isError } =
        approvalSettingService.useGetApprovalSettingById(
            approvalSettingId, isCreating === false);

    const addApprovalSetting = approvalSettingService.useAddApprovalSetting();
    const updateApprovalSetting = approvalSettingService.useUpdateApprovalSetting();
    const isSaving = addApprovalSetting.isPending || updateApprovalSetting.isPending;

    const [editModel, setEditModel] = useState<ApprovalSetting | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);

    // A shallow copy, so an abandoned edit never mutates the row still on screen behind it. On
    // create there is nothing to copy — the model is minted once and then left alone, which is
    // why the id is generated in the initialiser rather than on every render.
    useEffect(() => {
        if (isCreating) {
            setEditModel((current) =>
                current ?? newApprovalSetting(crypto.randomUUID()));

            return;
        }

        if (approvalSetting != null) {
            setEditModel({ ...approvalSetting });
        }
    }, [isCreating, approvalSetting]);

    const setField = <TField extends keyof ApprovalSetting>(
        field: TField,
        value: ApprovalSetting[TField]) =>
        setEditModel((current) =>
            current == null ? current : { ...current, [field]: value });

    const heading = isCreating
        ? 'New approval setting'
        : editModel == null
            ? 'Approval setting'
            : `${entityTypeLabels[editModel.entityType] ?? 'Approval'} settings`;

    useDocumentTitle(`${heading} — Admin — Glory 2 Him`);

    const crumbs: BreadcrumbItem[] = [
        { title: 'Admin' },
        { title: 'Approval Settings', href: approvalSettingsRoute },
        { title: heading, isActive: true },
    ];

    const backRoute =
        (location.state as { from?: string } | null)?.from ?? approvalSettingsRoute;

    const goBack = () => navigate(backRoute);

    // CHANGING THE ENTITY TYPE CAN INVALIDATE THE CONTENT TYPE, and the database enforces that
    // pairing with a CHECK constraint rather than the service — so a bad pair comes back as a
    // dependency failure with no field to hang it on. Clearing it here means that never happens.
    const setEntityType = (entityType: EntityType) =>
        setEditModel((current) =>
            current == null
                ? current
                : {
                    ...current,
                    entityType,
                    contentType: allowsContentTypeScope(entityType)
                        ? current.contentType
                        : null
                });

    const saveAsync = async () => {
        if (editModel == null) {
            return;
        }

        setActionError(null);

        try {
            if (isCreating) {
                await addApprovalSetting.mutateAsync(editModel);
            } else {
                await updateApprovalSetting.mutateAsync(editModel);
            }

            goBack();
        } catch (error) {
            setActionError(extractApiErrorMessage(
                error,
                'The approval setting could not be saved. A setting may already exist for '
                    + 'this scope.'));
        }
    };

    const isMissing = isCreating === false && (isError || approvalSetting == null);

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">{heading}</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            <div className="d-flex justify-content-end mb-3">
                <Button color="secondary" onClick={goBack}>
                    <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>
                    Back to Approval Settings
                </Button>
            </div>

            {actionError != null && (
                <div className="alert alert-danger" role="alert">{actionError}</div>
            )}

            {isLoading && isCreating === false ? (
                <div className="text-center py-5"><Spinner /></div>
            ) : isMissing || editModel == null ? (
                <div className="alert alert-danger" role="alert">
                    We could not load this approval setting right now. It may have been removed.
                </div>
            ) : (
                <>
                    <Card cssClass="mb-4" headerContent="Scope">
                        <p className="text-body-secondary small">
                            What this policy governs. Fixed once the row exists — moving a policy
                            to another scope is writing a different policy, not editing this one.
                        </p>

                        <div className="row g-3">
                            <div className="col-md-6">
                                <label className="form-label" htmlFor="approval-entity-type">
                                    Entity type
                                </label>

                                {isCreating ? (
                                    <select
                                        id="approval-entity-type"
                                        className="form-select"
                                        value={editModel.entityType}
                                        onChange={(event) =>
                                            setEntityType(
                                                Number(event.target.value) as EntityType)}>
                                        {entityTypeMembers.map((entityType) => (
                                            <option key={entityType} value={entityType}>
                                                {entityTypeLabels[entityType]}
                                            </option>
                                        ))}
                                    </select>
                                ) : (
                                    <p className="form-control-plaintext fw-semibold mb-0">
                                        {entityTypeLabels[editModel.entityType] ?? 'Unknown'}
                                    </p>
                                )}
                            </div>

                            <div className="col-md-6">
                                <label className="form-label" htmlFor="approval-content-type">
                                    Content type
                                </label>

                                {isCreating && allowsContentTypeScope(editModel.entityType) ? (
                                    <select
                                        id="approval-content-type"
                                        className="form-select"
                                        value={editModel.contentType ?? ''}
                                        onChange={(event) => setField(
                                            'contentType',
                                            event.target.value === ''
                                                ? null
                                                : Number(event.target.value) as ContentType)}>
                                        <option value="">
                                            Every content type (the default)
                                        </option>
                                        {contentTypeMembers.map((contentType) => (
                                            <option key={contentType} value={contentType}>
                                                {contentTypeLabels[contentType]}
                                            </option>
                                        ))}
                                    </select>
                                ) : (
                                    <p className="form-control-plaintext mb-0">
                                        {editModel.contentType == null
                                            ? 'Every content type (the default)'
                                            : contentTypeLabels[editModel.contentType]}
                                    </p>
                                )}

                                {allowsContentTypeScope(editModel.entityType) === false && (
                                    <div className="form-text">
                                        Only content items are scoped by content type.
                                    </div>
                                )}
                            </div>
                        </div>
                    </Card>

                    <Card cssClass="mb-4" headerContent="Approvals required">
                        <FormSwitch
                            label="Approving reviews are required"
                            value={editModel.requireApprovals}
                            onValueChange={(value) => setField('requireApprovals', value)} />

                        <div className="mb-3" style={{ maxWidth: '12rem' }}>
                            <label className="form-label" htmlFor="approval-required-count">
                                How many
                            </label>

                            <input
                                id="approval-required-count"
                                className="form-control"
                                type="number"
                                min={minimumRequiredApprovals}
                                disabled={editModel.requireApprovals === false}
                                value={editModel.requiredNumberOfApprovals}
                                onChange={(event) => {
                                    const parsed = Number(event.target.value);

                                    setField(
                                        'requiredNumberOfApprovals',
                                        Number.isFinite(parsed)
                                            ? Math.max(minimumRequiredApprovals, parsed)
                                            : minimumRequiredApprovals);
                                }} />
                        </div>
                    </Card>

                    <Card cssClass="mb-4" headerContent="Gates">
                        {policyFields.map((policyField) => (
                            <div className="mb-3" key={policyField.field}>
                                <FormSwitch
                                    label={policyField.label}
                                    value={editModel[policyField.field]}
                                    onValueChange={(value) =>
                                        setField(policyField.field, value)} />

                                <div className="form-text mt-0">{policyField.help}</div>
                            </div>
                        ))}
                    </Card>

                    <div className="d-flex gap-2 mb-4">
                        <Button
                            color="primary"
                            disabled={isSaving}
                            onClick={() => void saveAsync()}>
                            {isSaving
                                ? 'Saving…'
                                : isCreating ? 'Create setting' : 'Save settings'}
                        </Button>

                        {/* Reset restores the row as stored; on a create there is no stored row
                            to go back to, so the second action leaves instead. */}
                        <Button
                            color="outline-secondary"
                            disabled={isSaving}
                            onClick={() => isCreating || approvalSetting == null
                                ? goBack()
                                : setEditModel({ ...approvalSetting })}>
                            {isCreating ? 'Cancel' : 'Reset'}
                        </Button>
                    </div>
                </>
            )}
        </>
    );
};
