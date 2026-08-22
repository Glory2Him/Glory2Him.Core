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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Foundations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService : IApprovalOrchestrationService
    {
        private readonly IApprovalService approvalService;
        private readonly IApprovalReviewWorkflowService approvalReviewWorkflowService;
        private readonly IApprovalCommentService approvalCommentService;
        private readonly IAccessBroker accessBroker;
        private readonly IEventEnvelopeBroker eventEnvelopeBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        // Three services and three brokers. The seven entity services are absent on purpose:
        // the decision reaches its entity as a command event rather than a call (§16.7.1),
        // which is what keeps this inside the dependency-count guidance §12.5 entry 1 is on
        // record as breaking. IApprovalSettingService is absent for a different reason —
        // resolving §8.4 here would put most-specific-wins in a second place beside the
        // decision function (§8.6.1 rule 4).
        public ApprovalOrchestrationService(
            IApprovalService approvalService,
            IApprovalReviewWorkflowService approvalReviewWorkflowService,
            IApprovalCommentService approvalCommentService,
            IAccessBroker accessBroker,
            IEventEnvelopeBroker eventEnvelopeBroker,
            IEventBroker eventBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.approvalService = approvalService;
            this.approvalReviewWorkflowService = approvalReviewWorkflowService;
            this.approvalCommentService = approvalCommentService;
            this.accessBroker = accessBroker;
            this.eventEnvelopeBroker = eventEnvelopeBroker;
            this.eventBroker = eventBroker;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ApprovalVerdict> RetrieveApprovalVerdictAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveApprovalVerdict(entityType, entityId);

                // The envelope exists to capture the ambient caller the tier gate runs against.
                // The verdict is a read, but a privileged one — the moderation view, not a
                // public one (§16.7.2).
                var verdictRequest = new Approval
                {
                    EntityType = entityType,
                    EntityId = entityId
                };

                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: verdictRequest);

                ValidateUserMaySeeApprovalVerdict(envelope.SecurityContext);

                // Unfiltered, because the key is occupied by soft-deleted rows too and a
                // visibility-filtered read would report "no approval" for one that exists
                // (§9.7.2 rule 3).
                ApprovalEntityMatch maybeMatch =
                    await this.approvalService.FindApprovalByEntityAsync(
                        entityType: entityType,
                        entityId: entityId,
                        cancellationToken: cancellationToken);

                ValidateStorageApprovalExists(maybeMatch, entityType, entityId);

                return await ComposeApprovalVerdictAsync(
                    approvalMatch: maybeMatch,
                    entityType: entityType,
                    entityId: entityId,
                    securityContext: envelope.SecurityContext,
                    cancellationToken: cancellationToken);
            });

        // Two questions, deliberately asked separately, because they have different subjects.
        //
        // What is BLOCKING is a property of the approval — the threshold, a standing rejection,
        // the unresolved comments — and is the same for everyone who asks.
        //
        // Whether THIS caller may act on it is a property of the caller, and stays false for
        // people the conditions do not block at all: the author of the content (HR-2), and the
        // reviewer whose own review carried it over the line (§8.6 regardless-rule 1). That is
        // why CanApprove is not merely the negation of IsBlocked.
        private async ValueTask<ApprovalVerdict> ComposeApprovalVerdictAsync(
            ApprovalEntityMatch approvalMatch,
            EntityType entityType,
            Guid entityId,
            SecurityContext securityContext,
            CancellationToken cancellationToken)
        {
            ApprovalConditionsVerdict conditions =
                await this.accessBroker.EvaluateApprovalConditionsByIdAsync(
                    approvalId: approvalMatch.Id,
                    cancellationToken: cancellationToken);

            ValidateStorageApprovalConditionsResolved(conditions, entityType, entityId);

            AccessVerdict decisionVerdict = await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId: approvalMatch.Id,
                decision: ApprovalDecision.Approve,
                isBypassRequested: false,
                bypassReason: null,
                securityContext: securityContext,
                cancellationToken: cancellationToken);

            // Asked as a SEPARATE question rather than inferred from the refusal above. A
            // caller refused an ordinary approve may still be permitted a bypass, and the two
            // close for different reasons: DoNotAllowBypassingSettings shuts the route to
            // everyone including Admin, while the tier check shuts it only to the untiered.
            AccessVerdict bypassVerdict = await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId: approvalMatch.Id,
                decision: ApprovalDecision.Approve,
                isBypassRequested: true,
                bypassReason: BypassProbeReason,
                securityContext: securityContext,
                cancellationToken: cancellationToken);

            IReadOnlyList<ApprovalBlockReason> blockReasons = ComposeBlockReasons(
                approvalStatus: approvalMatch.ApprovalStatus,
                conditions: conditions,
                decisionVerdict: decisionVerdict);

            return new ApprovalVerdict
            {
                ApprovalId = approvalMatch.Id,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = approvalMatch.ApprovalStatus,
                BlockReasons = blockReasons,
                IsBypassAllowedForCurrentUser = bypassVerdict.IsPermitted,
                CanApprove = decisionVerdict.IsPermitted,
                ApprovalCount = conditions.ApprovalCount,
                RequiredNumberOfApprovals = conditions.RequiredNumberOfApprovals,
                UnresolvedApprovalCommentCount = conditions.UnresolvedApprovalCommentCount,
            };
        }
    }
}
