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
    allowsPersonalScope,
    ApprovalSetting,
    EntityType,
    entityTypeLabelOf,
    entityTypeLabels,
    entityTypeMembers,
    newApprovalSetting,
    scopeLabelOf
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

// ConfidenceScore's own 0.00-10.00 scale (design §13.5, §8.6.2) — the thresholds compare against
// it directly, on a narrower scale a fractional value like the design's own 7.5 suggestion could
// not even be entered.
const minimumConfidenceThreshold = 0;
const maximumConfidenceThreshold = 10;
const confidenceThresholdStep = 0.01;

// THE PAIR HAS AN ORDER AS WELL AS A RANGE (design §8.6.2): approve-above must not sit below
// reject-below, or a score between them satisfies §8.6.2's rule 1 and its rule 2 at once and
// Berean has two verdicts to cast for one score. The foundation refuses that pair and
// CK_ApprovalSetting_AIThresholdOrder stands behind it, so — in the same spirit as
// minimumRequiredApprovals above — the form refuses it before the save round-trips into a 400.
//
// IT REFUSES RATHER THAN CORRECTS, and that is the one place this rule cannot follow "How many".
// Clamping each box against the other would rewrite a number that was deliberately typed:
// entering 8 to reject below, with 3 still sitting in approve above, would silently store 3, and
// a policy that means something other than what the administrator entered is worse than a save
// they are asked to fix. So both boxes keep what was typed and the save is held instead.
const thresholdOrderMessageId = 'approval-ai-threshold-order';

const thresholdOrderMessage =
    'Approve above must not be below Reject below: a score between the two would file both a '
        + 'rejection and an approval.';

// Only the boolean members can be wired to a switch, so a mistyped field name below is a compile
// error rather than a switch that silently never moves. -? strips the optional modifier, or every
// optional member would smuggle `undefined` into the union and satisfy nothing.
type ApprovalSettingFlag = {
    [TField in keyof ApprovalSetting]-?:
    ApprovalSetting[TField] extends boolean ? TField : never
}[keyof ApprovalSetting];

// The same trick for the number boxes, so a draft is keyed on a field the model really has
// rather than on a string nothing checks. A nullable member is a union rather than a number and
// stays out on its own, which is what keeps the scope pickers off this list.
type ApprovalSettingNumber = {
    [TField in keyof ApprovalSetting]-?:
    ApprovalSetting[TField] extends number ? TField : never
}[keyof ApprovalSetting];

// What is in a number box while it is being typed, per field. A field with no entry here is not
// being typed in and reads its number off the model.
type NumberDrafts = Partial<Record<ApprovalSettingNumber, string>>;

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

    // WHAT IS IN A NUMBER BOX WHILE IT IS BEING TYPED, held per field and only for as long as
    // the typing lasts. AN EMPTY BOX IS THE REASON THIS EXISTS: Number('') is 0 and
    // Number.isFinite(0) is true, so clamping the raw value on every keystroke stored 0 the
    // instant a box was cleared to retype it — and with the ordering rule above, that 0 put the
    // approval threshold under the rejection one, painted both boxes invalid and refused the
    // save, for a box that had only been emptied. An empty box is "no value yet" instead: the
    // field keeps the last number it was given and nothing is written until one is typed.
    const [numberDrafts, setNumberDrafts] = useState<NumberDrafts>({});

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
            setNumberDrafts({});
            setEditModel({ ...approvalSetting });
        }
    }, [isCreating, approvalSetting]);

    const setField = <TField extends keyof ApprovalSetting>(
        field: TField,
        value: ApprovalSetting[TField]) =>
        setEditModel((current) =>
            current == null ? current : { ...current, [field]: value });

    // A box shows its draft while one is being typed and the stored number otherwise, so a
    // reload, a reset, or simply leaving the box puts the two back in step.
    const numberValueOf = (
        editedSetting: ApprovalSetting,
        field: ApprovalSettingNumber): string | number =>
        numberDrafts[field] ?? editedSetting[field];

    // The draft is kept whatever was typed; the model is written only from text a number can be
    // read out of. An empty box, a lone minus sign, a half-typed exponent — none of them is a
    // value anybody chose, so the field keeps what it had until one is. maximum is optional
    // because "How many" has a floor and no ceiling: the foundation sets none either.
    const setNumberField = (
        field: ApprovalSettingNumber,
        text: string,
        minimum: number,
        maximum?: number) => {
        setNumberDrafts((current): NumberDrafts => ({ ...current, [field]: text }));

        const parsed = Number(text);

        if (text.trim() === '' || Number.isFinite(parsed) === false) {
            return;
        }

        const atLeastMinimum = Math.max(minimum, parsed);

        setField(
            field,
            maximum == null ? atLeastMinimum : Math.min(atLeastMinimum, maximum));
    };

    // Leaving the box ends its draft, so what is on screen is what would be saved: a box left
    // empty reads back the number still stored rather than sitting blank in front of a save that
    // would write something else.
    const endNumberDraft = (field: ApprovalSettingNumber) =>
        setNumberDrafts((current): NumberDrafts => ({ ...current, [field]: undefined }));

    const heading = isCreating
        ? 'New approval setting'
        : editModel == null
            ? 'Approval setting'
            : editModel.entityType == null
                ? 'Global approval settings'
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

    // CHANGING THE ENTITY TYPE CAN INVALIDATE WHAT NARROWS IT — a content type belongs to
    // ContentItem alone and a personality to Association alone — and the database enforces both
    // pairings with CHECK constraints rather than the service, so a bad pair comes back as a
    // dependency failure with no field to hang it on. Clearing them here means that never
    // happens. Null is the global tier, which nothing narrows.
    const setEntityType = (entityType: EntityType | null) =>
        setEditModel((current) =>
            current == null
                ? current
                : {
                    ...current,
                    entityType,
                    contentType: allowsContentTypeScope(entityType)
                        ? current.contentType
                        : null,
                    isPersonal: allowsPersonalScope(entityType)
                        ? current.isPersonal
                        : null
                });

    // ISAIALLOWEDTOVOTE REQUIRES ISAIREVIEWEROFFERED (design §8.6.2), and storage refuses the
    // pair the other way round (CK_ApprovalSetting_AIVoteRequiresAIReviewer) — so switching the
    // reviewer off clears the vote in the same update, mirroring how choosing an entity type
    // above clears whatever narrowing it can no longer carry.
    const setAIReviewerOffered = (isAIReviewerOffered: boolean) =>
        setEditModel((current) =>
            current == null
                ? current
                : {
                    ...current,
                    isAIReviewerOffered,
                    isAIAllowedToVote: isAIReviewerOffered && current.isAIAllowedToVote
                });

    // Derived on every render rather than checked on save alone, so the message arrives while the
    // two boxes that caused it are still in front of the reader. UNCONDITIONAL of the vote
    // switch, because the foundation is: it refuses the order however isAIAllowedToVote reads, so
    // a guard that looked only while Berean may vote would let exactly the rows it skipped
    // round-trip into the 400 this exists to prevent.
    const isThresholdOrderInvalid = editModel != null
        && editModel.aiApprovalConfidenceApprovalThreshold
            < editModel.aiApprovalConfidenceRejectionThreshold;

    // Only when there is something to describe, so a well-ordered pair is not announced as
    // carrying an empty error. Both boxes point at the one message: the rule is about the PAIR,
    // and is-invalid on its own is a colour rather than a sentence.
    const thresholdOrderAttributes = isThresholdOrderInvalid
        ? { 'aria-invalid': true, 'aria-describedby': thresholdOrderMessageId }
        : {};

    // A reset puts the drafts back with the row: a box mid-edit must not keep showing the text
    // that was being typed over a number that has just been restored under it.
    const resetEdit = () => {
        if (isCreating || approvalSetting == null) {
            goBack();

            return;
        }

        setNumberDrafts({});
        setEditModel({ ...approvalSetting });
    };

    const saveAsync = async () => {
        if (editModel == null) {
            return;
        }

        // Held here rather than sent. The alert repeats what already sits under the two boxes
        // because the button is at the far end of a long form and the fields may be scrolled off
        // it — the same reason a refused save says anything at all.
        if (isThresholdOrderInvalid) {
            setActionError(thresholdOrderMessage);

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
                                        value={editModel.entityType ?? ''}
                                        onChange={(event) =>
                                            setEntityType(
                                                event.target.value === ''
                                                    ? null
                                                    : Number(event.target.value) as EntityType)}>
                                        <option value="">
                                            Every entity type (the global default)
                                        </option>
                                        {entityTypeMembers.map((entityType) => (
                                            <option key={entityType} value={entityType}>
                                                {entityTypeLabels[entityType]}
                                            </option>
                                        ))}
                                    </select>
                                ) : (
                                    <p className="form-control-plaintext fw-semibold mb-0">
                                        {entityTypeLabelOf(editModel.entityType)}
                                    </p>
                                )}
                            </div>

                            {/* WHAT NARROWS THE ROW below its entity type, and there is at most
                                one thing that can: a content type on ContentItem, a personality
                                on Association, nothing on anything else — so the column shows
                                whichever applies rather than a picker that would be refused. */}
                            {allowsContentTypeScope(editModel.entityType) ? (
                                <div className="col-md-6">
                                    <label className="form-label" htmlFor="approval-content-type">
                                        Content type
                                    </label>

                                    {isCreating ? (
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
                                </div>
                            ) : allowsPersonalScope(editModel.entityType) ? (
                                <div className="col-md-6">
                                    <label className="form-label" htmlFor="approval-personality">
                                        Which associations
                                    </label>

                                    {isCreating ? (
                                        <select
                                            id="approval-personality"
                                            className="form-select"
                                            value={editModel.isPersonal == null
                                                ? ''
                                                : editModel.isPersonal ? 'personal' : 'editorial'}
                                            onChange={(event) => setField(
                                                'isPersonal',
                                                event.target.value === ''
                                                    ? null
                                                    : event.target.value === 'personal')}>
                                            <option value="">
                                                Every association (the default)
                                            </option>
                                            <option value="personal">
                                                Personal associations only
                                            </option>
                                            <option value="editorial">
                                                Editorial associations only
                                            </option>
                                        </select>
                                    ) : (
                                        <p className="form-control-plaintext mb-0">
                                            {editModel.isPersonal == null
                                                ? 'Every association (the default)'
                                                : scopeLabelOf(editModel)}
                                        </p>
                                    )}

                                    <div className="form-text">
                                        A personal association is one a user keeps for
                                        themselves &mdash; their own reaction, their own tag on
                                        something &mdash; whichever end of it the personal
                                        entity sits on. Everything else is editorial.
                                    </div>
                                </div>
                            ) : (
                                <div className="col-md-6">
                                    <label className="form-label">Narrowed by</label>
                                    <p className="form-control-plaintext mb-0">Nothing</p>

                                    <div className="form-text">
                                        Only content items are narrowed by content type, and
                                        only associations by personality.
                                    </div>
                                </div>
                            )}
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
                                value={numberValueOf(editModel, 'requiredNumberOfApprovals')}
                                onBlur={() => endNumberDraft('requiredNumberOfApprovals')}
                                onChange={(event) => setNumberField(
                                    'requiredNumberOfApprovals',
                                    event.target.value,
                                    minimumRequiredApprovals)} />
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

                    <Card cssClass="mb-4" headerContent="AI reviewer (Berean)">
                        <p className="text-body-secondary small">
                            Offers an automated first pass on a round, under its own system
                            identity (design §8.6.2). It always comments in words once asked; the
                            vote below is an additional, optional step.
                        </p>

                        <FormSwitch
                            label="Offer Berean as a reviewer"
                            value={editModel.isAIReviewerOffered}
                            onValueChange={setAIReviewerOffered} />

                        <div className="form-text mt-0 mb-3">
                            With this off, Berean is never offered and performs no action of
                            any kind.
                        </div>

                        <FormSwitch
                            label="Allow Berean to additionally cast a vote"
                            value={editModel.isAIAllowedToVote}
                            disabled={editModel.isAIReviewerOffered === false}
                            onValueChange={(value) => setField('isAIAllowedToVote', value)} />

                        <div className="form-text mt-0 mb-3">
                            With this off, Berean still comments with what it believes the
                            verdict should be and the score behind it — a human casts the vote.
                        </div>

                        <div className="row g-3">
                            <div className="col-md-6">
                                <label
                                    className="form-label"
                                    htmlFor="approval-ai-rejection-threshold">
                                    Reject below
                                </label>

                                {/* DISABLED WHILE THE VOTE IS OFF (§8.6.2 reads the thresholds
                                    only when Berean may cast one) — EXCEPT while the pair is
                                    the reason the save is being refused. The order rule holds
                                    whatever the vote switch says, so a box that is at once the
                                    fault and out of reach would strand an edit in front of a
                                    message it could not answer. */}
                                <input
                                    id="approval-ai-rejection-threshold"
                                    className={isThresholdOrderInvalid
                                        ? 'form-control is-invalid'
                                        : 'form-control'}
                                    type="number"
                                    min={minimumConfidenceThreshold}
                                    max={maximumConfidenceThreshold}
                                    step={confidenceThresholdStep}
                                    disabled={editModel.isAIAllowedToVote === false
                                        && isThresholdOrderInvalid === false}
                                    {...thresholdOrderAttributes}
                                    value={numberValueOf(
                                        editModel,
                                        'aiApprovalConfidenceRejectionThreshold')}
                                    onBlur={() => endNumberDraft(
                                        'aiApprovalConfidenceRejectionThreshold')}
                                    onChange={(event) => setNumberField(
                                        'aiApprovalConfidenceRejectionThreshold',
                                        event.target.value,
                                        minimumConfidenceThreshold,
                                        maximumConfidenceThreshold)} />

                                <div className="form-text">
                                    A confidence score below this files a rejected review.
                                </div>
                            </div>

                            <div className="col-md-6">
                                <label
                                    className="form-label"
                                    htmlFor="approval-ai-approval-threshold">
                                    Approve above
                                </label>

                                <input
                                    id="approval-ai-approval-threshold"
                                    className={isThresholdOrderInvalid
                                        ? 'form-control is-invalid'
                                        : 'form-control'}
                                    type="number"
                                    min={minimumConfidenceThreshold}
                                    max={maximumConfidenceThreshold}
                                    step={confidenceThresholdStep}
                                    disabled={editModel.isAIAllowedToVote === false
                                        && isThresholdOrderInvalid === false}
                                    {...thresholdOrderAttributes}
                                    value={numberValueOf(
                                        editModel,
                                        'aiApprovalConfidenceApprovalThreshold')}
                                    onBlur={() => endNumberDraft(
                                        'aiApprovalConfidenceApprovalThreshold')}
                                    onChange={(event) => setNumberField(
                                        'aiApprovalConfidenceApprovalThreshold',
                                        event.target.value,
                                        minimumConfidenceThreshold,
                                        maximumConfidenceThreshold)} />

                                <div className="form-text">
                                    A confidence score above this files an approved review.
                                </div>
                            </div>
                        </div>

                        {/* d-block because this sits beside the row rather than beside either
                            input, and Bootstrap only reveals invalid-feedback next to the
                            is-invalid control it follows. */}
                        {isThresholdOrderInvalid && (
                            <div
                                className="invalid-feedback d-block"
                                id={thresholdOrderMessageId}>
                                {thresholdOrderMessage}
                            </div>
                        )}
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
                            onClick={resetEdit}>
                            {isCreating ? 'Cancel' : 'Reset'}
                        </Button>
                    </div>
                </>
            )}
        </>
    );
};
