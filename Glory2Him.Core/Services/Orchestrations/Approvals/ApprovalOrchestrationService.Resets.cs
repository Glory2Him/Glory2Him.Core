// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    /// <summary>
    /// §8.6 HR-4's administrator override, reached from the moderation screen: a round that landed
    /// on an outcome by accident is put back where it was.
    /// </summary>
    internal partial class ApprovalOrchestrationService
    {
        public ValueTask<ApprovalOutcome> ResetApprovalAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnResetApproval(entityType, entityId);

                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(
                        content: new Approval
                        {
                            EntityType = entityType,
                            EntityId = entityId
                        });

                // ADMINISTRATORS ALONE, and asked first. §8.6 HR-4 makes moving a row OUT of a
                // terminal state an override, and the override is what keeps "terminal" meaning
                // anything: a state a publisher could edit out of would not be terminal at all.
                // The entity's own transition verb already gates it this way, and this gate is
                // the same rule asked one layer up rather than a second, weaker one.
                ValidateUserMayResetApproval(envelope.SecurityContext);

                // §14.5 rule 3, and the same probe every other operation on this service asks:
                // a taken-down subject is not found for every caller, Administrators included,
                // so a round whose entity has gone cannot be reset either.
                bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

                ValidateStorageEntityIsVisible(isEntityVisible, entityType, entityId);

                // Unfiltered, for the same reason the verdict and the decision read it that way:
                // a soft-deleted row still occupies the key (§9.7.2 rule 3).
                ApprovalEntityMatch approvalMatch =
                    await this.approvalService.FindApprovalByEntityAsync(
                        entityType: entityType,
                        entityId: entityId,
                        cancellationToken: cancellationToken);

                ValidateStorageApprovalExists(approvalMatch, entityType, entityId);

                Approval storageApproval =
                    await this.approvalService.RetrieveApprovalByIdAsync(
                        approvalId: approvalMatch.Id,
                        cancellationToken: cancellationToken);

                // ONLY A DECIDED ROUND HAS ANYTHING TO RESET. A Draft or an already-open
                // Submitted round is refused rather than quietly re-written: the control that
                // reaches this operation is offered only on a decided round, so a request for
                // any other is a caller disagreeing with the server about what it is looking at,
                // and answering it would dismiss a live round's reviews for nothing.
                ValidateStorageApprovalIsDecided(storageApproval, entityType, entityId);

                storageApproval.ApprovalStatus = ApprovalStatus.Submitted;

                // THE BYPASS PAIR IS CLEARED, not carried. It records how THIS decision was
                // reached (§9.7.5), and after a reset there is no decision — a round put back
                // for review must not still claim a waiver for an outcome that has been taken
                // away. The same clearing every real outcome already performs, applied to the
                // outcome being undone.
                storageApproval.IsApprovedByBypass = false;
                storageApproval.ApprovedByBypassReason = null;

                // A person pressed Reset, so the audit records them rather than the workflow.
                Approval resetApproval = await this.approvalService.ModifyApprovalAsync(
                    approval: storageApproval,
                    attribution: WorkflowAttribution.DecidingCaller,
                    cancellationToken: cancellationToken);

                // §12.5.3 BR12's exception, and the half of it that had never been built. An
                // override ALWAYS dismisses the active reviews, regardless of
                // RequireReapprovalOnChange — those reviews produced the verdict being overruled,
                // and re-opening the round on their strength would let the override be undone by
                // the very reviews it overrode.
                //
                // Ordered AFTER the status write on purpose. Dismissal publishes
                // ApprovalReview-Dismissed, which this service subscribes to and answers by
                // re-testing the round; doing it while the round still read Approved would
                // re-test a round nobody may act on, and the foundation refuses a dismissal
                // against a terminal round in any case (§8.8 regardless-rule 1).
                await DismissStaleApprovalReviewsAsync(
                    approvalId: resetApproval.Id,
                    cancellationToken: cancellationToken);

                // The entity follows, as a SYNC rather than a second decision (§9.8). The
                // command carries Submitted, and the entity's transition verb answers it under
                // the workflow identity — which is what forces IsPublished = false and clears
                // PublishDate, taking an approved item off the public site. That unpublishing is
                // the point of the operation rather than a side effect of it: a reset exists to
                // recover from something that landed on an outcome by accident, and leaving it
                // publicly visible while it waits for a second verdict would recover nothing.
                await PublishEntityApprovalCommandAsync(
                    approval: resetApproval,
                    cancellationToken: cancellationToken);

                return new ApprovalOutcome
                {
                    ApprovalId = resetApproval.Id,
                    EntityType = resetApproval.EntityType,
                    EntityId = resetApproval.EntityId,
                    ApprovalStatus = resetApproval.ApprovalStatus,
                    IsApprovedByBypass = resetApproval.IsApprovedByBypass,
                    ApprovedByBypassReason = resetApproval.ApprovedByBypassReason,
                    IsEntitySyncRequested = true,
                };
            });
    }
}
