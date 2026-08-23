// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    internal partial class ApprovalService
    {
        // the §16.6 scoped-role suffixes, built from the global role names so the
        // convention has a single source of truth
        private const string ScopedReviewerRoleSuffix = Roles.ReviewerSuffix;
        private const string ScopedPublisherRoleSuffix = Roles.PublisherSuffix;

        // the foundation enforces the same security rules as the orchestration (design
        // §14.6): an exposer may bind to either service directly, so no layer may assume
        // an upstream layer already gated the caller

        private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not authenticated.");
            }

            // no Approval-scoped ReadOnly role exists — approvals are workflow records,
            // not entity-scoped content, so only the global block role applies
            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is blocked from contributing approvals.");
            }
        }

        // Tier 1, row-local: the global Reviewer/Publisher/Admin roles, plus — by the §16.6
        // convention — any entity-scoped "{Entity}-Reviewer"/"{Entity}-Publisher" role.
        //
        // This check only ever sees the caller, so it cannot know which entity type an approval
        // targets: a Tag-Reviewer passes it for a Link's approval. Narrowing to the approval's
        // own entity type was once described as an orchestration concern. That is withdrawn: it
        // lives in the foundation, one tier down, through IAccessBroker — which can read the
        // entity behind the approval where this cannot. (§12.3.1 withdraws the orchestration for
        // ApprovalReview and ApprovalComment; it does not list Approval, whose own orchestration
        // is simply unbuilt. Either way there is nothing to defer to.)
        //
        // Both tiers run, and §14.6 rule 2 makes the duplicate intentional. Note the composition
        // is an AND, so tier 2 must admit everyone tier 1 does — the owner branch is inside the
        // broker decision for exactly that reason, not left here to be ORed in.
        //
        // WHERE THIS IS USED: the modify gate below (paired with the broker) and the two READ
        // paths, which are still row-local. §14.7's "Known gap" paragraph records that.
        private static bool HasReviewRole(SecurityContext securityContext) =>
            securityContext.Roles.Contains(Roles.Reviewer)
                || securityContext.Roles.Contains(Roles.Publisher)
                || securityContext.Roles.Contains(Roles.Admin)
                || securityContext.Roles.Any(role =>
                    role.EndsWith(ScopedReviewerRoleSuffix, StringComparison.Ordinal)
                        || role.EndsWith(ScopedPublisherRoleSuffix, StringComparison.Ordinal));

        // row-level write permission: the submitter who opened the approval may amend it
        // and a review role may act on it — the narrower workflow rules (which status
        // transitions are legal, who may bypass) stay in the orchestration
        private async ValueTask ValidateUserCanModifyStorageApprovalAsync(
            Approval storageApproval,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApproval.CreatedBy == actorUserId;

            if (isOwner is false && HasReviewRole(securityContext) is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not allowed to modify this approval.");
            }
        }

        // Tier 2, cross-entity. HasReviewRole above matches ANY "-Reviewer"/"-Publisher" suffix,
        // so a bare Tag-Reviewer clears it for a ContentItem↔BibleReference association's
        // approval. The broker resolves the entity behind the approval — for an association,
        // both of its endpoints, either of which is enough (§14.7 posture A′ rule 2).
        //
        // The REVIEW tier, not the publisher tier: §14.7 posture D rule 3 has reviewers move an
        // approval between the workflow statuses through this path, so narrowing THIS gate to
        // publishers would refuse the very callers the rule admits.
        //
        // It is not the whole story any more. This gate answers "may this caller touch this
        // row"; moving the status INTO Approved or Rejected additionally asks the §8.6.1
        // decision, which requires the PUBLISHER tier (HR-3: reviewing is vouching, deciding is
        // deciding). So a reviewer still resubmits and reopens, and no longer decides — see
        // ValidateUserMayDecideStorageApprovalAsync.
        private async ValueTask ValidateUserMayAmendStorageApprovalAsync(
            Approval storageApproval,
            SecurityContext securityContext,
            CancellationToken cancellationToken)
        {
            AccessVerdict verdict = await this.accessBroker.MayAmendApprovalAsync(
                approvalId: storageApproval.Id,
                securityContext: securityContext,
                cancellationToken: cancellationToken);

            if (verdict.IsPermitted is false)
            {
                // §14.5: the true reason server-side, nothing about the policy to the caller.
                await this.loggingBroker.LogWarningAsync(
                    $"Approval modification denied for approval {storageApproval.Id}. "
                        + $"{verdict.DenialReason}: {verdict.Explanation} "
                        + "Reported to the caller as unauthorized.");

                throw new UnauthorizedApprovalException(
                    message: "The current user is not allowed to modify this approval.");
            }
        }

        // removing an approval retracts the workflow record itself — the owner may
        // withdraw their own and an Admin may remove anyone's; Reviewers and Publishers
        // act through the approval's status instead
        private async ValueTask ValidateUserCanRemoveStorageApprovalAsync(
            Approval storageApproval,
            SecurityContext securityContext)
        {
            string actorUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            bool isOwner =
                string.IsNullOrWhiteSpace(actorUserId) is false
                    && storageApproval.CreatedBy == actorUserId;

            if (isOwner is false && securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not allowed to remove this approval.");
            }
        }

        // a hard remove destroys the row and its audit trail — Admin only
        private static void ValidateUserCanHardRemoveApproval(SecurityContext securityContext)
        {
            if (securityContext is null || securityContext.IsAuthenticated is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not authenticated.");
            }

            if (securityContext.Roles.Contains(Roles.ReadOnly))
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is blocked from contributing approvals.");
            }

            if (securityContext.Roles.Contains(Roles.Admin) is false)
            {
                throw new UnauthorizedApprovalException(
                    message: "The current user is not allowed to permanently remove this approval.");
            }
        }

        private async ValueTask ValidateOnAddApprovalAsync(
            Approval approval,
            SecurityContext securityContext)
        {
            ValidateApprovalIsNotNull(approval);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approval.Id), Parameter: nameof(Approval.Id)),
                (Rule: IsInvalid(approval.EntityId), Parameter: nameof(Approval.EntityId)),
                (Rule: IsInvalid(approval.CreatedBy), Parameter: nameof(Approval.CreatedBy)),
                (Rule: IsInvalid(approval.UpdatedBy), Parameter: nameof(Approval.UpdatedBy)),
                (Rule: IsInvalid(approval.CreatedWhen), Parameter: nameof(Approval.CreatedWhen)),
                (Rule: IsInvalid(approval.UpdatedWhen), Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: IsGreaterThan(approval.CreatedBy, 255),
                    Parameter: nameof(Approval.CreatedBy)),

                (Rule: IsGreaterThan(approval.UpdatedBy, 255),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: IsGreaterThan(approval.ApprovedByBypassReason, 500),
                    Parameter: nameof(Approval.ApprovedByBypassReason)),

                // An approval is born undecided. Approved and Rejected are the workflow's to
                // record through the modify-side §8.6.1 gate, and the bypass pair is DERIVED
                // from that gate's verdict (§9.7.5) — so none of the three may arrive on add.
                //
                // Without these rules a caller could insert a row already Approved, or one
                // attesting that conditions were waived when no decision ever ran, and nothing
                // downstream reliably undoes either. The derivation only fires on a status
                // CHANGE into an outcome, so a row forged as Approved is never decided at all —
                // IsApplyingOutcome is false for Approved-to-Approved — and its forged pair is
                // pinned in place with it. Both are correctable only by moving the row out of
                // Approved and back through a real decision, which is a repair nobody would
                // know to perform on evidence that looks legitimate.
                (Rule: IsNotContributableStatus(approval.ApprovalStatus),
                    Parameter: nameof(Approval.ApprovalStatus)),

                (Rule: IsSetOnAdd(approval.IsApprovedByBypass),
                    Parameter: nameof(Approval.IsApprovedByBypass)),

                (Rule: IsSetOnAdd(approval.ApprovedByBypassReason),
                    Parameter: nameof(Approval.ApprovedByBypassReason)),

                (Rule: IsNotSame(
                        firstDate: approval.UpdatedWhen,
                        secondDate: approval.CreatedWhen,
                        secondDateName: nameof(Approval.CreatedWhen)),
                    Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approval.CreatedBy),
                    Parameter: nameof(Approval.CreatedBy)),

                (Rule: IsNotSame(
                        first: approval.UpdatedBy,
                        second: approval.CreatedBy,
                        secondName: nameof(Approval.CreatedBy)),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: await IsNotRecentAsync(approval.CreatedWhen),
                    Parameter: nameof(Approval.CreatedWhen)));
        }

        private async ValueTask ValidateOnModifyApprovalAsync(
            Approval approval,
            SecurityContext securityContext)
        {
            ValidateApprovalIsNotNull(approval);
            string currentUserId = await this.securityAuditBroker.GetUserIdAsync(securityContext);

            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approval.Id), Parameter: nameof(Approval.Id)),
                (Rule: IsInvalid(approval.EntityId), Parameter: nameof(Approval.EntityId)),
                (Rule: IsInvalid(approval.CreatedBy), Parameter: nameof(Approval.CreatedBy)),
                (Rule: IsInvalid(approval.UpdatedBy), Parameter: nameof(Approval.UpdatedBy)),
                (Rule: IsInvalid(approval.CreatedWhen), Parameter: nameof(Approval.CreatedWhen)),
                (Rule: IsInvalid(approval.UpdatedWhen), Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: IsGreaterThan(approval.CreatedBy, 255),
                    Parameter: nameof(Approval.CreatedBy)),

                (Rule: IsGreaterThan(approval.UpdatedBy, 255),
                    Parameter: nameof(Approval.UpdatedBy)),

                // Capped to the column (design §7.2) so an over-long bypass reason is refused
                // here as bad input rather than surfacing from SQL as a dependency failure.
                (Rule: IsGreaterThan(approval.ApprovedByBypassReason, 500),
                    Parameter: nameof(Approval.ApprovedByBypassReason)),

                // §7.2: Dismissed belongs to ApprovalReview records only — "Entities and
                // Approval records never hold Dismissed". Add already refuses it through
                // IsNotContributableStatus; unrefused here, the invariant held on one path and
                // not the other, and the status is deliberately unpinned on modify so nothing
                // else stood in the way. ToApprovalState maps Dismissed onto Draft, so a row
                // parked there is one nobody can review or decide until it is moved back.
                (Rule: IsDismissedStatus(approval.ApprovalStatus),
                    Parameter: nameof(Approval.ApprovalStatus)),

                (Rule: IsNotSame(
                        first: currentUserId,
                        second: approval.UpdatedBy),
                    Parameter: nameof(Approval.UpdatedBy)),

                (Rule: IsSame(
                        firstDate: approval.UpdatedWhen,
                        secondDate: approval.CreatedWhen,
                        secondDateName: nameof(Approval.CreatedWhen)),
                    Parameter: nameof(Approval.UpdatedWhen)),

                (Rule: await IsNotRecentAsync(approval.UpdatedWhen),
                    Parameter: nameof(Approval.UpdatedWhen)));
        }

        // Null-check first (a malformed event), then verify the integrity signature against the
        // event name this handler serves and the request direction. The signature is what makes
        // the envelope's SecurityContext trustworthy on the event path: without it a caller who can
        // put a message on this address states their own identity and roles and is believed
        // (design §14.6 rule 4). Verification sits in the receiver, not the transport, because a
        // handler is reachable without going through the broker.
        private async ValueTask ValidateApprovalEventEnvelopeAsync(
            EventEnvelope<Approval> envelope,
            ApprovalEventOperation operation)
        {
            if (envelope is null || envelope.Content is null || envelope.Metadata is null)
            {
                throw new InvalidApprovalEventException(
                    message: "Invalid approval event. " +
                        "The event envelope, its content and metadata are required.");
            }

            string eventName = $"{nameof(Approval)}{operation}";

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidApprovalEventException(
                    message: "Invalid approval event. Integrity verification failed.");
            }
        }

        private static void ValidateAgainstStorageApprovalOnModify(
            Approval inputApproval,
            Approval storageApproval)
        {
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        firstDate: inputApproval.CreatedWhen,
                        secondDate: storageApproval.CreatedWhen,
                        secondDateName: nameof(Approval.CreatedWhen)),
                    Parameter: nameof(Approval.CreatedWhen)),
                (Rule: IsNotSame(
                        first: inputApproval.CreatedBy,
                        second: storageApproval.CreatedBy,
                        secondName: nameof(Approval.CreatedBy)),
                    Parameter: nameof(Approval.CreatedBy)),
                (Rule: IsSame(
                        firstDate: inputApproval.UpdatedWhen,
                        secondDate: storageApproval.UpdatedWhen,
                        secondDateName: nameof(Approval.UpdatedWhen)),
                    Parameter: nameof(Approval.UpdatedWhen)),

                // EntityType and EntityId are IDENTITY, not content: they say which row this
                // approval is about, and an approval must not be repointable at a different
                // entity. Unpinned, a caller authorized for the approval as it stands could
                // move it onto something else in the same write — and the tier-2 gate above,
                // which asks about the STORED row, would have answered for the old target.
                //
                // ApprovalStatus is deliberately NOT pinned. §14.7 posture D rule 3 has
                // reviewers move the status through this very path; pinning it would refuse the
                // operation's purpose. What narrows it is authorization, not a pin — the amend
                // gate for the workflow statuses, and the §8.6.1 decision gate on top of it the
                // moment the payload moves the status into Approved or Rejected.
                (Rule: IsNotSame(
                        first: inputApproval.EntityType,
                        second: storageApproval.EntityType,
                        secondName: nameof(Approval.EntityType)),
                    Parameter: nameof(Approval.EntityType)),

                (Rule: IsNotSame(
                        first: inputApproval.EntityId,
                        second: storageApproval.EntityId,
                        secondName: nameof(Approval.EntityId)),
                    Parameter: nameof(Approval.EntityId)));
        }

        /// <summary>
        /// The bypass pair records that the §8.5 conditions were WAIVED and why, so outside an
        /// approval decision it is pinned to storage: unpinned, an authorized caller could mark
        /// an approval bypassed — or erase an existing waiver and its stated reason — without
        /// any waiver being decided.
        ///
        /// <para>When the modify IS the approval decision, the payload pair is the caller's
        /// bypass REQUEST rather than a write: the §8.6.1 decision refuses a bypass the policy
        /// closes or one with no reason, and what lands is derived from its verdict, never
        /// copied from the payload. So the pin steps aside for exactly the paths that apply an
        /// outcome, and holds on every workflow-status move.</para>
        ///
        /// <para>A rejection waives nothing, so its verdict always derives the pair to
        /// false/null — which CLEARS a stale waiver rather than merely refusing to touch it.
        /// Pinning the rejection instead would strand it: a row bypass-approved, reopened and
        /// then rejected would assert <c>IsApprovedByBypass = true</c> for ever, because the
        /// only path that can rewrite the pair is an outcome and the only outcome left to it
        /// would be pinned. The entity siblings clear on both outcomes for this reason
        /// (<c>AssociationService.Transitions.cs</c>).</para>
        /// </summary>
        private static void ValidateBypassPairAgainstStorageOnModify(
            Approval inputApproval,
            Approval storageApproval)
        {
            if (IsApplyingOutcome(inputApproval, storageApproval))
            {
                return;
            }

            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsNotSame(
                        first: inputApproval.IsApprovedByBypass,
                        second: storageApproval.IsApprovedByBypass,
                        secondName: nameof(Approval.IsApprovedByBypass)),
                    Parameter: nameof(Approval.IsApprovedByBypass)),

                // Coalesced because the column is nullable and "no reason recorded" is the same
                // fact whether it is stored as null or as empty — a caller sending one for the
                // other is not attempting a change worth refusing. Every sibling pin on this
                // same field coalesces for the same reason.
                (Rule: IsNotSame(
                        first: inputApproval.ApprovedByBypassReason ?? string.Empty,
                        second: storageApproval.ApprovedByBypassReason ?? string.Empty,
                        secondName: nameof(Approval.ApprovedByBypassReason)),
                    Parameter: nameof(Approval.ApprovedByBypassReason)));
        }

        // Moving an approval INTO Approved or Rejected is applying the §8.6.1 decision, which
        // the amend gate was never asked about: it answers "may this caller touch this row",
        // and §14.7 posture D rule 3 admits the SUBMITTER there. Without the second question a
        // role-less submitter could approve their own round through modify.
        private static bool IsApplyingOutcome(Approval inputApproval, Approval storageApproval) =>
            inputApproval.ApprovalStatus != storageApproval.ApprovalStatus
                && (inputApproval.ApprovalStatus == ApprovalStatus.Approved
                    || inputApproval.ApprovalStatus == ApprovalStatus.Rejected);

        // An outcome may only be applied to an OPEN round, and that is true of the workflow as
        // much as of a person — so unlike the three tiers beside it, this one is not skipped for
        // the system identity.
        //
        // The distinction is the same one that justifies the workflow's unfiltered read: "is
        // this round open" is a fact about STORAGE, not about who is asking. The three caller
        // tiers ask the second question and are rightly skipped; this asks the first.
        //
        // It lived inside the §8.6.1 decision function, which returns ApprovalNotOpenForReview
        // for any state but Submitted — so skipping that function for the workflow also skipped
        // this, and a Draft round reaching EvaluateApprovalAsync could be driven straight to
        // Approved. No human can do that, Admin included.
        private static void ValidateStorageApprovalRoundIsOpenForOutcome(
            Approval inputApproval,
            Approval storageApproval)
        {
            if (IsApplyingOutcome(inputApproval, storageApproval) is false)
            {
                return;
            }

            if (storageApproval.ApprovalStatus != ApprovalStatus.Submitted)
            {
                throw new InvalidApprovalException(
                    message: $"Approval is {storageApproval.ApprovalStatus}, not Submitted, "
                        + "so there is no open round to decide.");
            }
        }

        // A waiver is DERIVED from a verdict or it does not exist (§9.7.5, §9.7.1 rule 3).
        //
        // On the workflow path there is no verdict to derive from — the decision function is
        // skipped — and the pin that would otherwise catch a payload-asserted pair steps aside
        // for an outcome write (ValidateBypassPairAgainstStorageOnModify returns early when
        // IsApplyingOutcome). Both halves off on the same condition would let a caller record a
        // bypass nothing granted.
        //
        // No caller can express it today: all four orchestration call sites either derive the
        // pair from their own verdict or set it to false first. This refuses the shape anyway,
        // because "nobody currently does" is not a property the type system holds.
        private static void ValidateWorkflowClaimsNoBypass(
            Approval inputApproval,
            Approval storageApproval)
        {
            if (IsApplyingOutcome(inputApproval, storageApproval) is false)
            {
                return;
            }

            if (inputApproval.IsApprovedByBypass)
            {
                throw new InvalidApprovalException(
                    message: "A bypass cannot be recorded on a workflow outcome: it is derived "
                        + "from a decision verdict, and the workflow takes none.");
            }
        }

        /// <summary>
        /// The §8.6.1 gate for the two outcome statuses, asked of the STORED approval and
        /// answered by the same decision function the entity transitions consult. Null when the
        /// payload applies no outcome; the verdict otherwise, because the caller derives the
        /// stored bypass pair from it (§9.7.5).
        ///
        /// <para>Runs in ADDITION to the amend gate rather than instead of it. This is a
        /// deliberate AND of two DIFFERENT questions — may this caller touch this row; may this
        /// caller apply this outcome — not the two-gates-one-question composition that deleted
        /// the owner branch in #251: the submitter keeps every amendment posture D grants them,
        /// and gains an outcome only when the decision function says so.</para>
        /// </summary>
        private async ValueTask<AccessVerdict> ValidateUserMayDecideStorageApprovalAsync(
            Approval inputApproval,
            Approval storageApproval,
            SecurityContext securityContext,
            CancellationToken cancellationToken)
        {
            if (IsApplyingOutcome(inputApproval, storageApproval) is false)
            {
                return null;
            }

            ApprovalDecision decision = inputApproval.ApprovalStatus == ApprovalStatus.Approved
                ? ApprovalDecision.Approve
                : ApprovalDecision.Reject;

            AccessVerdict verdict = await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId: storageApproval.Id,
                decision: decision,
                isBypassRequested: inputApproval.IsApprovedByBypass,
                bypassReason: inputApproval.ApprovedByBypassReason,
                securityContext: securityContext,
                cancellationToken: cancellationToken);

            if (verdict.IsPermitted is false)
            {
                // §14.5: the true reason server-side, nothing about the policy to the caller.
                await this.loggingBroker.LogWarningAsync(
                    $"Approval decision denied for approval {storageApproval.Id}. "
                        + $"{verdict.DenialReason}: {verdict.Explanation} "
                        + "Reported to the caller as unauthorized.");

                throw new UnauthorizedApprovalException(
                    message: "The current user is not allowed to decide this approval.");
            }

            return verdict;
        }

        private static void ValidateOnRetrieveApprovalById(Guid approvalId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)));

        // the deletion reason is caller-supplied free text that lands on the row unchanged,
        // so its storage cap is enforced here rather than left to the column to reject
        private static void ValidateOnRemoveApprovalById(Guid approvalId, string? deletionReason) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)),

                (Rule: IsGreaterThan(deletionReason, 500),
                    Parameter: nameof(Approval.DeletionReason)));

        private static void ValidateOnHardRemoveApprovalById(Guid approvalId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(approvalId), Parameter: nameof(Approval.Id)));

        private static void ValidateStorageApproval(Approval maybeApproval, Guid approvalId)
        {
            if (maybeApproval is null)
            {
                throw new NotFoundApprovalException(
                    message: $"Approval not found with id: {approvalId}.");
            }
        }

        /// <summary>
        /// A retracted approval is closed to writes. Reported as not found rather than as a
        /// validation error, matching the read path (§14.5): a caller who may write to the row
        /// still learns nothing beyond "there is nothing here to write to".
        /// </summary>
        private static void ValidateStorageApprovalIsNotDeleted(Approval storageApproval)
        {
            if (storageApproval.IsDeleted)
            {
                throw new NotFoundApprovalException(
                    message: $"Approval not found with id: {storageApproval.Id}.");
            }
        }

        private static void ValidateApprovalIsNotNull(Approval approval)
        {
            if (approval is null)
            {
                throw new NullApprovalException(message: "Approval is null.");
            }
        }

        private static dynamic IsInvalid(EntityType entityType) => new
        {
            Condition = Enum.IsDefined(entityType) == false,
            Message = "Value is not a supported entity type"
        };

        private static dynamic IsInvalid(Guid id) => new
        {
            Condition = id == Guid.Empty,
            Message = "Id is required"
        };

        private static dynamic IsInvalid(string text) => new
        {
            Condition = string.IsNullOrWhiteSpace(text),
            Message = "Text is required"
        };

        private static dynamic IsInvalid(DateTimeOffset date) => new
        {
            Condition = date == default,
            Message = "Date is required"
        };

        private static dynamic IsNotSame(
            string first,
            string second) => new
            {
                Condition = first != second,
                Message = $"Expected value to be '{first}' but found '{second}'."
            };

        private static dynamic IsNotSame(
            string first,
            string second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Text is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            bool first,
            bool second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            Guid first,
            Guid second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Id is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            EntityType first,
            EntityType second,
            string secondName) => new
            {
                Condition = first != second,
                Message = $"Value is not the same as {secondName}"
            };

        private static dynamic IsNotSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate != secondDate,
                Message = $"Date is not the same as {secondDateName}"
            };

        private static dynamic IsSetOnAdd(bool value) => new
        {
            Condition = value,
            Message = "Value is not allowed on add"
        };

        private static dynamic IsSetOnAdd(string? text) => new
        {
            Condition = string.IsNullOrWhiteSpace(text) is false,
            Message = "Text is not allowed on add"
        };

        // Dismissed is an ApprovalReview verdict, never an Approval's own status (§7.2). Stated
        // as its own rule rather than folded into a whitelist because the other four are all
        // legitimate on modify and only this one is categorically wrong.
        private static dynamic IsDismissedStatus(ApprovalStatus approvalStatus) => new
        {
            Condition = approvalStatus == ApprovalStatus.Dismissed,

            Message = $"Value must not be {nameof(ApprovalStatus.Dismissed)} on an approval",
        };

        // a caller may save work in progress or submit it for review; the remaining states
        // are verdicts, and a verdict is the approval workflow's to record (design §9.7.1
        // rule 1)
        private static dynamic IsNotContributableStatus(ApprovalStatus approvalStatus) => new
        {
            Condition = approvalStatus != ApprovalStatus.Draft
                && approvalStatus != ApprovalStatus.Submitted,

            Message = $"Value must be {nameof(ApprovalStatus.Draft)} " +
                $"or {nameof(ApprovalStatus.Submitted)} on add"
        };

        private static dynamic IsGreaterThan(string? text, int maxLength) => new
        {
            Condition = (text ?? string.Empty).Length > maxLength,
            Message = $"Text exceed max length of {maxLength} characters"
        };

        private static dynamic IsSame(
            DateTimeOffset firstDate,
            DateTimeOffset secondDate,
            string secondDateName) => new
            {
                Condition = firstDate == secondDate,
                Message = $"Date is the same as {secondDateName}"
            };

        private async ValueTask<dynamic> IsNotRecentAsync(DateTimeOffset date)
        {
            var (isNotRecent, startDate, endDate) = await IsDateNotRecentAsync(date);

            return new
            {
                Condition = isNotRecent,
                Message = $"Date is not recent. Expected a value between {startDate} and {endDate} but found {date}"
            };
        }

        private async ValueTask<(bool IsNotRecent, DateTimeOffset StartDate, DateTimeOffset EndDate)>
            IsDateNotRecentAsync(DateTimeOffset date)
        {
            int pastThreshold = 90;
            int futureThreshold = 0;
            DateTimeOffset currentDateTime = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();
            DateTimeOffset startDate = currentDateTime.AddSeconds(-pastThreshold);
            DateTimeOffset endDate = currentDateTime.AddSeconds(futureThreshold);
            bool isNotRecent = date < startDate || date > endDate;

            return (isNotRecent, startDate, endDate);
        }

        private static void Validate(
            string message,
            params (dynamic Rule, string Parameter)[] validations)
        {
            var invalidApprovalException = new InvalidApprovalException(message);

            foreach ((dynamic rule, string parameter) in validations)
            {
                if (rule.Condition)
                {
                    invalidApprovalException.UpsertDataList(
                        key: parameter,
                        value: rule.Message);
                }
            }

            invalidApprovalException.ThrowIfContainsErrors();
        }
    }
}
