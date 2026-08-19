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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    internal partial class ApprovalService
    {
        public ValueTask<ApprovalEntityMatch?> FindApprovalByEntityAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // The probe carries no entity, so the request exists only to anchor the ambient
                // security context the contribution gate runs against — a write-flow primitive,
                // not a public read. Same shape the submit transitions use.
                var findRequest = new Approval
                {
                    EntityType = entityType,
                    EntityId = entityId
                };

                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: findRequest);

                ValidateUserIsAllowedToContribute(envelope.SecurityContext);
                ValidateOnFindApprovalByEntity(entityType, entityId);

                // Deliberately UNFILTERED (§9.7.2 rule 3). This is the difference that makes the
                // probe necessary at all: UX_Approvals_EntityType_EntityId is not filtered on
                // IsDeleted, so a soft-deleted approval STILL OCCUPIES the key. A
                // visibility-filtered lookup would report "does not exist" for a key that does,
                // and the insert it invites could never succeed. The projection below reveals no
                // row body, so nothing leaks that resubmitting would not already disclose.
                IQueryable<Approval> allApprovals =
                    await this.storageBroker.SelectAllApprovalsAsync(cancellationToken);

                // Prefer a LIVE row when one exists, and otherwise the most recently touched
                // soft-deleted one — the row the reinstate branch acts on. The unique index
                // spans deleted rows, so in a consistent store there is at most one match
                // either way; the ordering makes the choice deterministic rather than relying
                // on that.
                Approval? match = allApprovals
                    .Where(approval =>
                        approval.EntityType == entityType
                            && approval.EntityId == entityId)
                    .OrderBy(approval => approval.IsDeleted)
                    .ThenByDescending(approval => approval.UpdatedWhen)
                    .FirstOrDefault();

                if (match is null)
                {
                    return null;
                }

                return new ApprovalEntityMatch
                {
                    Id = match.Id,
                    ApprovalStatus = match.ApprovalStatus,
                    IsDeleted = match.IsDeleted,
                };
            });

        // The probe keys on the pair the unique index keys on, so both halves must be
        // identified. An unresolved entity id would key the lookup off Guid.Empty and match
        // nothing meaningful — reporting "no approval" for every unresolved subject and
        // inviting an insert that collides with whatever really occupies the key.
        private static void ValidateOnFindApprovalByEntity(
            EntityType entityType,
            Guid entityId) =>
            Validate(
                message: "Approval is invalid, fix the errors and try again.",
                (Rule: IsInvalid(entityType), Parameter: nameof(Approval.EntityType)),
                (Rule: IsInvalid(entityId), Parameter: nameof(Approval.EntityId)));
    }
}
