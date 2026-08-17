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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // A row as storage would hold it, sitting in review. The transitions all authorize
        // against what is STORED, so these fillers pin the stored shape rather than the
        // caller's — which is the difference that matters for these operations.
        private static Association CreateApprovableStorageAssociation() =>
            CreateStorageAssociationInStatus(ApprovalStatus.Submitted);

        private static Association CreateSubmittableStorageAssociation() =>
            CreateStorageAssociationInStatus(ApprovalStatus.Draft);

        private static Association CreateStorageAssociationInStatus(ApprovalStatus status)
        {
            Association association = CreateRandomAssociation();
            association.ApprovalStatus = status;
            association.IsPublished = false;
            association.PublishDate = null;
            association.IsDeleted = false;

            return association;
        }

        // The caller's half of an approve: an id plus the three IApproval values, and nothing
        // else that matters. Approving does not carry content.
        private static Association CreateApprovalDecision(Guid associationId) =>
            new Association
            {
                Id = associationId,
                ApprovalStatus = ApprovalStatus.Approved,
                IsPublished = true,
                PublishDate = GetRandomDateTimeOffset()
            };

        // A bypass REQUEST: the caller asks by setting the pair, and the verdict decides what is
        // recorded. Since the bypass folded into the widened verb it is the same decision with
        // two more fields set, which is exactly what this expresses.
        private static Association CreateBypassApprovalDecision(Guid associationId)
        {
            Association association = CreateApprovalDecision(associationId);
            association.IsApprovedByBypass = true;
            association.ApprovedByBypassReason = GetRandomString();

            return association;
        }

        // The Admin override's target: a terminal row re-opened for a second round. Publication
        // is not asked for — the validation refuses a published non-approved row, and the
        // do-work derives it off regardless.
        private static Association CreateReopenDecision(Guid associationId) =>
            new Association
            {
                Id = associationId,
                ApprovalStatus = ApprovalStatus.Submitted,
                IsPublished = false,
                PublishDate = null
            };

        // A stored row in a terminal state, published as an approved one would be, so a test can
        // assert the override actually unpublishes it rather than finding it already false.
        private static Association CreateTerminalStorageAssociation(
            ApprovalStatus terminalStatus)
        {
            Association association = CreateStorageAssociationInStatus(terminalStatus);
            association.IsPublished = terminalStatus == ApprovalStatus.Approved;

            association.PublishDate = association.IsPublished
                ? GetRandomDateTimeOffset()
                : null;

            return association;
        }

        // The context ApprovalOrchestrationService mints for the workflow's own writes. Roleless
        // on purpose: the flag is the whole of its authority, so a test that passes with roles
        // attached would not be proving the flag did anything.
        private static SecurityContext CreateSystemSecurityContext() =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = [],
                IsSystemIdentity = true
            };

        private static Association CreateRejectionDecision(Guid associationId) =>
            new Association
            {
                Id = associationId,
                ApprovalStatus = ApprovalStatus.Rejected,
                IsPublished = false,
                PublishDate = null
            };

        // ModelVersion is drawn to a FIXED length rather than by GetRandomString(). The column
        // caps at 128 and set-confidence validates that cap, while GetRandomString() draws two
        // to ten mnemonic words — which reaches past 128 at the top of its range. A decision
        // this helper calls valid must never be invalid on the draw, and when it was, the
        // symptom was a rare failure in a test about something else entirely: the transition
        // threw a validation exception before reaching the storage read the test was pinning.
        // ConfidenceReason is left free because its cap is 500, out of the draw's reach.
        private static Association CreateConfidenceDecision(Guid associationId) =>
            new Association
            {
                Id = associationId,
                ConfidenceScore = GetRandomConfidenceScore(),
                ConfidenceReason = GetRandomString(),
                SourceBatchId = Guid.NewGuid(),
                ModelVersion = GetRandomStringWithLengthOf(64)
            };

        // A human correction: a score with the machine provenance deliberately cleared.
        private static Association CreateHumanConfidenceDecision(Guid associationId) =>
            new Association
            {
                Id = associationId,
                ConfidenceScore = GetRandomConfidenceScore(),
                ConfidenceReason = GetRandomString(),
                SourceBatchId = null,
                ModelVersion = null
            };

        private static Association CreateAnchorAssociation(int sortOrder)
        {
            Association anchorAssociation = CreateRandomAssociation();
            anchorAssociation.SortOrder = sortOrder;

            return anchorAssociation;
        }

        // The effective ids are PERSISTED computed columns with a private setter, so an
        // in-memory row has them at default(Guid) while a row read from the database has them
        // filled in. The scope-collision check reads the COLUMN — which is exactly what lets it
        // translate to SQL instead of running as a client-side method call — so a test row
        // standing in for a stored one has to carry the value the database would have computed.
        private static Association WithDatabaseComputedEffectiveIds(Association association)
        {
            typeof(Association)
                .GetProperty(nameof(Association.EntityAEffectiveId))
                .SetValue(
                    association,
                    association.EntityAScope == Scope.AllVersions
                        ? association.EntityAGroupId
                        : association.EntityAKeyId);

            typeof(Association)
                .GetProperty(nameof(Association.EntityBEffectiveId))
                .SetValue(
                    association,
                    association.EntityBScope == Scope.AllVersions
                        ? association.EntityBGroupId
                        : association.EntityBKeyId);

            return association;
        }
    }
}
