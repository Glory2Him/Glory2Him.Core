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
        isAIReviewerOffered: false,
        isAIAllowedToVote: false,
        aiApprovalConfidenceRejectionThreshold: 2.5,
        aiApprovalConfidenceApprovalThreshold: 7.5,
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

// The thresholds only open once Berean may vote, so every threshold case starts by walking the
// two switches the way an administrator would.
const allowBereanToVote = async () => {
    await userEvent.click(screen.getByText('Offer Berean as a reviewer'));
    await userEvent.click(screen.getByText('Allow Berean to additionally cast a vote'));
};

// Reject below 8, approve above 3 — the pair §8.6.2 forbids, typed rather than assembled, so the
// assertions are about what the boxes do to a reader who enters it.
const invertTheThresholds = async () => {
    await allowBereanToVote();
    await userEvent.clear(screen.getByLabelText('Reject below'));
    await userEvent.type(screen.getByLabelText('Reject below'), '8');
    await userEvent.clear(screen.getByLabelText('Approve above'));
    await userEvent.type(screen.getByLabelText('Approve above'), '3');
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

        // THE HOUSE POLICY, not the entity's C# defaults: a content-type row narrows a seeded
        // default, so it opens matching that default (ApprovalSettingSeedData) and the
        // administrator changes only what they mean to.
        it('should open on the seeded house policy rather than on a looser one', () => {
            // given
            renderCreatePage();

            // then
            expect(screen.getByLabelText('How many')).toHaveValue(2);
            expect(switchFor('Approving reviews are required')).toBeChecked();
            expect(switchFor('A rejected review blocks approval')).toBeChecked();
            expect(switchFor('A zero confidence score blocks approval')).toBeChecked();
        });

        // Berean (§8.6.2) ships off on a new row, the same posture ApprovalSettingSeedData
        // ships everywhere, and the vote switch it gates opens disabled to say so before the
        // reader ever tries it.
        it('should open with Berean off and the vote switch disabled', () => {
            // given
            renderCreatePage();

            // then
            expect(switchFor('Offer Berean as a reviewer')).not.toBeChecked();
            expect(switchFor('Allow Berean to additionally cast a vote')).toBeDisabled();
        });

        it('should enable the vote switch once Berean is offered', async () => {
            // given
            renderCreatePage();

            // when
            await userEvent.click(screen.getByText('Offer Berean as a reviewer'));

            // then
            expect(switchFor('Allow Berean to additionally cast a vote')).not.toBeDisabled();
        });

        // ISAIALLOWEDTOVOTE CANNOT OUTLIVE ISAIREVIEWEROFFERED (storage refuses the pair the
        // other way round, CK_ApprovalSetting_AIVoteRequiresAIReviewer) — so switching the
        // reviewer back off clears a vote the reader had already turned on, the same way
        // choosing a new entity type clears a content type it can no longer carry.
        it('should clear the vote when Berean is switched back off', async () => {
            // given
            renderCreatePage();
            await userEvent.click(screen.getByText('Offer Berean as a reviewer'));
            await userEvent.click(screen.getByText('Allow Berean to additionally cast a vote'));
            expect(switchFor('Allow Berean to additionally cast a vote')).toBeChecked();

            // when
            await userEvent.click(screen.getByText('Offer Berean as a reviewer'));

            // then
            expect(switchFor('Allow Berean to additionally cast a vote')).not.toBeChecked();
            expect(switchFor('Allow Berean to additionally cast a vote')).toBeDisabled();
        });

        // THE THRESHOLDS ARE READ ONLY WHEN THE VOTE IS CAST (§8.6.2) — disabled until then,
        // mirroring how "How many" is disabled while approvals are not required.
        it('should keep the confidence thresholds disabled until Berean may vote', async () => {
            // given
            renderCreatePage();

            // then
            expect(screen.getByLabelText('Reject below')).toBeDisabled();
            expect(screen.getByLabelText('Approve above')).toBeDisabled();

            // when
            await userEvent.click(screen.getByText('Offer Berean as a reviewer'));
            await userEvent.click(screen.getByText('Allow Berean to additionally cast a vote'));

            // then
            expect(screen.getByLabelText('Reject below')).not.toBeDisabled();
            expect(screen.getByLabelText('Approve above')).not.toBeDisabled();
        });

        it('should write the chosen Berean settings', async () => {
            // given
            renderCreatePage();
            await userEvent.click(screen.getByText('Offer Berean as a reviewer'));
            await userEvent.click(screen.getByText('Allow Berean to additionally cast a vote'));

            // when
            await userEvent.clear(screen.getByLabelText('Reject below'));
            await userEvent.type(screen.getByLabelText('Reject below'), '3');
            await userEvent.clear(screen.getByLabelText('Approve above'));
            await userEvent.type(screen.getByLabelText('Approve above'), '8');
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    isAIReviewerOffered: true,
                    isAIAllowedToVote: true,
                    aiApprovalConfidenceRejectionThreshold: 3,
                    aiApprovalConfidenceApprovalThreshold: 8
                })));
        });

        // THE THRESHOLDS HAVE AN ORDER AS WELL AS A RANGE (§8.6.2): inverted, a score between
        // them satisfies the reject-below rule and the approve-above rule at once. The
        // foundation refuses the pair and CK_ApprovalSetting_AIThresholdOrder stands behind it,
        // so the form has to refuse it first — the same job "How many" does for its own floor.
        it('should refuse the save while approve-above sits below reject-below', async () => {
            // given
            renderCreatePage(listRoute);
            await invertTheThresholds();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            expect(await screen.findByRole('alert')).toHaveTextContent(
                'Approve above must not be below Reject below');

            expect(added).not.toHaveBeenCalled();
            expect(screen.queryByTestId('list')).not.toBeInTheDocument();
        });

        // IT REFUSES RATHER THAN CORRECTS. Clamping one box against the other would store a
        // number nobody chose, and a policy that means something other than what was entered is
        // worse than a save the administrator is asked to fix.
        it('should keep both thresholds exactly as they were typed', async () => {
            // given
            renderCreatePage();

            // when
            await invertTheThresholds();

            // then
            expect(screen.getByLabelText('Reject below')).toHaveValue(8);
            expect(screen.getByLabelText('Approve above')).toHaveValue(3);
        });

        // THE RULE IS ABOUT THE PAIR, so both boxes are the fault and both point at the one
        // message: is-invalid on its own is a colour, and a sentence beside a field is not a
        // sentence attached to it.
        it('should attach the reason to both threshold boxes', async () => {
            // given
            renderCreatePage();

            // when
            await invertTheThresholds();

            // then
            const message = screen.getByText(
                'Approve above must not be below Reject below: a score between the two would '
                    + 'file both a rejection and an approval.');

            for (const label of ['Reject below', 'Approve above']) {
                expect(screen.getByLabelText(label))
                    .toHaveAttribute('aria-invalid', 'true');

                expect(screen.getByLabelText(label))
                    .toHaveAttribute('aria-describedby', message.id);
            }
        });

        it('should let the save through once the pair is put back in order', async () => {
            // given
            renderCreatePage();
            await invertTheThresholds();

            // when
            await userEvent.clear(screen.getByLabelText('Approve above'));
            await userEvent.type(screen.getByLabelText('Approve above'), '9');
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    aiApprovalConfidenceRejectionThreshold: 8,
                    aiApprovalConfidenceApprovalThreshold: 9
                })));
        });

        // EQUAL IS PERMITTED, and the foundation permits it too: it closes the middle band
        // rather than overlapping the two rules, and it is the pair the fail-closed defaults
        // fall back to.
        it('should accept a pair that meets in the middle', async () => {
            // given
            renderCreatePage();
            await allowBereanToVote();

            // when
            await userEvent.clear(screen.getByLabelText('Reject below'));
            await userEvent.type(screen.getByLabelText('Reject below'), '5');
            await userEvent.clear(screen.getByLabelText('Approve above'));
            await userEvent.type(screen.getByLabelText('Approve above'), '5');
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    aiApprovalConfidenceRejectionThreshold: 5,
                    aiApprovalConfidenceApprovalThreshold: 5
                })));
        });

        // THE GUARD MUST NOT TRAP THE ROW. The foundation refuses the order however the vote
        // switch reads, so the form refuses it there too — which means the two boxes stay
        // reachable while they are the fault, rather than leaving an edit in front of a message
        // it has no way to answer.
        it('should still refuse the order with the vote off, and leave it fixable',
            async () => {
                // given
                renderCreatePage(listRoute);
                await invertTheThresholds();

                // when
                await userEvent.click(
                    screen.getByText('Allow Berean to additionally cast a vote'));

                await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

                // then
                expect(await screen.findByRole('alert')).toHaveTextContent(
                    'Approve above must not be below Reject below');

                expect(added).not.toHaveBeenCalled();
                expect(screen.getByLabelText('Reject below')).not.toBeDisabled();
                expect(screen.getByLabelText('Approve above')).not.toBeDisabled();

                // and the row goes in once the fault is answered
                await userEvent.clear(screen.getByLabelText('Approve above'));
                await userEvent.type(screen.getByLabelText('Approve above'), '9');
                await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

                await waitFor(() =>
                    expect(added).toHaveBeenCalledWith(expect.objectContaining({
                        isAIAllowedToVote: false,
                        aiApprovalConfidenceRejectionThreshold: 8,
                        aiApprovalConfidenceApprovalThreshold: 9
                    })));
            });

        // Once the pair is back in order the boxes answer to the vote switch again, so turning
        // the vote off still shuts them — the fault is what re-opened them, not the visit.
        it('should shut the thresholds again once the pair is in order', async () => {
            // given
            renderCreatePage();
            await invertTheThresholds();

            // when
            await userEvent.clear(screen.getByLabelText('Approve above'));
            await userEvent.type(screen.getByLabelText('Approve above'), '9');

            await userEvent.click(
                screen.getByText('Allow Berean to additionally cast a vote'));

            // then
            expect(screen.getByLabelText('Reject below')).toBeDisabled();
            expect(screen.getByLabelText('Approve above')).toBeDisabled();
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

        it('should say why only some entity types are narrowed', async () => {
            // given
            renderCreatePage();

            // when
            await userEvent.selectOptions(
                screen.getByLabelText('Entity type'), String(EntityType.Tag));

            // then
            expect(screen.getByText(
                'Only content items are narrowed by content type, and only associations by '
                    + 'personality.')).toBeInTheDocument();
        });

        // THE GLOBAL TIER (§8.4): entity type null, the one row every default narrows.
        it('should write the global default when no entity type is chosen', async () => {
            // given
            renderCreatePage();

            // when
            await userEvent.selectOptions(screen.getByLabelText('Entity type'), '');
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    entityType: null,
                    contentType: null,
                    isPersonal: null
                })));
        });

        // THE PERSONALITY TIER (§8.4): offered for Association alone, and it is the row that
        // lets a user's own reaction skip the review an editorial placement gets.
        it('should offer the personality of an association and write it', async () => {
            // given
            renderCreatePage();

            // when
            await userEvent.selectOptions(
                screen.getByLabelText('Entity type'), String(EntityType.Association));

            await userEvent.selectOptions(
                screen.getByLabelText('Which associations'), 'personal');

            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    entityType: EntityType.Association,
                    contentType: null,
                    isPersonal: true
                })));
        });

        it('should drop a personality that the chosen entity type cannot carry', async () => {
            // given
            renderCreatePage();

            await userEvent.selectOptions(
                screen.getByLabelText('Entity type'), String(EntityType.Association));

            await userEvent.selectOptions(
                screen.getByLabelText('Which associations'), 'editorial');

            // when
            await userEvent.selectOptions(
                screen.getByLabelText('Entity type'), String(EntityType.Comment));

            // then
            expect(screen.queryByLabelText('Which associations')).not.toBeInTheDocument();

            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    entityType: EntityType.Comment,
                    isPersonal: null
                })));
        });
    });

    // A BOX HAS TO BE EMPTIABLE TO BE RETYPABLE. Number('') is 0 and Number.isFinite(0) is true,
    // so a clamp that reads the box on every keystroke stores 0 the instant it is cleared —
    // which, with the ordering rule, turned "select all and retype" into an inverted pair, two
    // red boxes and a refused save that the administrator had done nothing to deserve. An empty
    // box means "no value yet": the field keeps the number it had until another is typed.
    describe('clearing a number box to retype it', () => {
        it('should not drop a cleared threshold to zero', async () => {
            // given
            renderCreatePage();
            await allowBereanToVote();

            // when
            await userEvent.clear(screen.getByLabelText('Approve above'));

            // then
            expect(screen.getByLabelText('Approve above')).toHaveValue(null);
            expect(screen.getByLabelText('Approve above')).not.toHaveClass('is-invalid');
            expect(screen.getByLabelText('Reject below')).not.toHaveClass('is-invalid');

            expect(screen.queryByText(
                'Approve above must not be below Reject below: a score between the two would '
                    + 'file both a rejection and an approval.')).not.toBeInTheDocument();
        });

        it('should save the number a box still empty was left holding', async () => {
            // given
            renderCreatePage(listRoute);
            await allowBereanToVote();

            // when
            await userEvent.clear(screen.getByLabelText('Approve above'));
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    aiApprovalConfidenceApprovalThreshold: 7.5
                })));
        });

        // WHAT IS ON SCREEN IS WHAT WOULD BE SAVED. Leaving the box ends the draft, so a box
        // abandoned empty reads its number back rather than sitting blank over a save that would
        // write something else.
        it('should read the kept number back once the box is left', async () => {
            // given
            renderCreatePage();
            await allowBereanToVote();

            // when
            await userEvent.clear(screen.getByLabelText('Approve above'));
            await userEvent.click(screen.getByLabelText('Reject below'));

            // then
            expect(screen.getByLabelText('Approve above')).toHaveValue(7.5);
        });

        it('should take the number typed in after the box is cleared', async () => {
            // given
            renderCreatePage(listRoute);
            await allowBereanToVote();

            // when
            await userEvent.clear(screen.getByLabelText('Approve above'));
            await userEvent.type(screen.getByLabelText('Approve above'), '9');
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    aiApprovalConfidenceRejectionThreshold: 2.5,
                    aiApprovalConfidenceApprovalThreshold: 9
                })));
        });

        // THE RANGE STILL BINDS A NUMBER THAT WAS TYPED (§13.5's 0.00-10.00 scale) — it is only
        // the absence of one that no longer counts as a choice.
        it('should still hold a typed threshold inside the confidence scale', async () => {
            // given
            renderCreatePage(listRoute);
            await allowBereanToVote();

            // when
            await userEvent.clear(screen.getByLabelText('Approve above'));
            await userEvent.type(screen.getByLabelText('Approve above'), '15');
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    aiApprovalConfidenceApprovalThreshold: 10
                })));
        });

        // THE SAME RULE ON THE BOX BESIDE IT: the floor is for a count that was asked for, not
        // for one rubbed out on the way to another.
        it('should not drop a cleared required count to its floor', async () => {
            // given
            renderCreatePage(listRoute);

            // when
            await userEvent.clear(screen.getByLabelText('How many'));

            // then
            expect(screen.getByLabelText('How many')).toHaveValue(null);

            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    requiredNumberOfApprovals: 2
                })));
        });

        // A caller may ask for fewer approvals than one and the foundation refuses it, so a
        // typed zero is still lifted to the floor rather than round-tripped into a 400.
        it('should still lift a typed count to its floor', async () => {
            // given
            renderCreatePage(listRoute);

            // when
            await userEvent.clear(screen.getByLabelText('How many'));
            await userEvent.type(screen.getByLabelText('How many'), '0');
            await userEvent.click(screen.getByRole('button', { name: 'Create setting' }));

            // then
            await waitFor(() =>
                expect(added).toHaveBeenCalledWith(expect.objectContaining({
                    requiredNumberOfApprovals: 1
                })));
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

        it('should name the global default by what it is', () => {
            // given
            approvalSetting = createApprovalSetting({ entityType: null, contentType: null });

            // when
            renderEditPage();

            // then
            expect(screen.getByRole('heading', { name: 'Global approval settings' }))
                .toBeInTheDocument();

            expect(screen.getByText('Every entity type')).toBeInTheDocument();
        });

        it('should read a personality row back as plain text', () => {
            // given
            approvalSetting = createApprovalSetting({
                entityType: EntityType.Association,
                contentType: null,
                isPersonal: true
            });

            // when
            renderEditPage();

            // then
            expect(screen.queryByLabelText('Which associations')).not.toBeInstanceOf(
                HTMLSelectElement);

            expect(screen.getByText('Personal associations only')).toBeInTheDocument();
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
