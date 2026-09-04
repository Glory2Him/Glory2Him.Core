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
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        /// <summary>
        /// §14.5 rule 3 / §9.7.6 rule 3. A soft-deleted entity is not found for EVERY caller,
        /// so the probe an approval gate asks must answer on VISIBILITY rather than on presence.
        ///
        /// <para>Every arm, because the flaw this replaces was per-arm: the arms are the raw
        /// by-id reads and this repository has no EF global query filters, so a tombstone answers
        /// with its author on all eight exactly as it did before the takedown. A theory over one
        /// entity type would have passed against the old author probe too.</para>
        /// </summary>
        [Theory]
        [InlineData(EntityType.ContentItem)]
        [InlineData(EntityType.Tag)]
        [InlineData(EntityType.Reaction)]
        [InlineData(EntityType.BibleReference)]
        [InlineData(EntityType.Comment)]
        [InlineData(EntityType.Link)]
        [InlineData(EntityType.Attachment)]
        [InlineData(EntityType.Association)]
        public async Task ShouldReportEveryEntityTypesRemovedRowAsNotVisibleAsync(
            EntityType entityType)
        {
            // given: a row that reads back, carries its author, and has been taken down
            var entityId = Guid.NewGuid();

            SetupEntityAuthor(entityType, entityId, GetRandomString(), isDeleted: true);

            // when
            bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                entityType,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            isEntityVisible.Should().BeFalse();
        }

        /// <summary>
        /// The other half of the same theory: a live row on every arm is visible, so the probe is
        /// seen to distinguish the two rather than to refuse everything.
        /// </summary>
        [Theory]
        [InlineData(EntityType.ContentItem)]
        [InlineData(EntityType.Tag)]
        [InlineData(EntityType.Reaction)]
        [InlineData(EntityType.BibleReference)]
        [InlineData(EntityType.Comment)]
        [InlineData(EntityType.Link)]
        [InlineData(EntityType.Attachment)]
        [InlineData(EntityType.Association)]
        public async Task ShouldReportEveryEntityTypesLiveRowAsVisibleAsync(EntityType entityType)
        {
            // given
            var entityId = Guid.NewGuid();

            SetupEntityAuthor(entityType, entityId, GetRandomString(), isDeleted: false);

            // when
            bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                entityType,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            isEntityVisible.Should().BeTrue();
        }

        /// <summary>
        /// A row that could not be read at all is reported invisible for the same reason a
        /// deleted one is: neither is a subject an approval may act on, and failing closed is the
        /// only safe direction when the entity behind an approval has gone.
        /// </summary>
        [Fact]
        public async Task ShouldReportAnUnreadableEntityAsNotVisibleAsync()
        {
            // given: no row stubbed, so the store answers null
            var entityId = Guid.NewGuid();

            // when
            bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                EntityType.Link,
                entityId,
                TestContext.Current.CancellationToken);

            // then
            isEntityVisible.Should().BeFalse();
        }

        /// <summary>
        /// §9.7.6 rule 3 travels to the decision function as a fact on the request, so the two
        /// tiers §14.6 rule 2 asks for are genuinely both fed. Failing CLOSED is asserted for the
        /// unreadable case: an approval whose subject cannot be read is no more decidable than
        /// one whose subject was taken down.
        /// </summary>
        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task ShouldCarryTheSubjectsRemovalIntoTheDecisionAsync(
            bool isEntityDeleted,
            bool expectedIsSubjectDeleted)
        {
            // given
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            SetupEntityAuthor(
                EntityType.ContentItem,
                entityId,
                GetRandomString(),
                isDeleted: isEntityDeleted);

            SetupApprovalById(new Approval
            {
                Id = approvalId,
                EntityType = EntityType.ContentItem,
                EntityId = entityId,
                ApprovalStatus = ApprovalStatus.Submitted,
            });

            // when
            await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId: approvalId,
                decision: ApprovalDecision.Approve,
                isBypassRequested: false,
                bypassReason: null,
                securityContext: CreateAuthenticatedSecurityContext(),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.IsSubjectDeleted
                .Should().Be(expectedIsSubjectDeleted);
        }

        /// <summary>
        /// An approval whose entity row cannot be read at all reports its subject as REMOVED,
        /// which is what stops an approval outliving a hard-removed item being decidable.
        /// </summary>
        [Fact]
        public async Task ShouldCarryAnUnreadableSubjectIntoTheDecisionAsRemovedAsync()
        {
            // given: the approval exists, its entity does not
            var approvalId = Guid.NewGuid();

            SetupApprovalById(new Approval
            {
                Id = approvalId,
                EntityType = EntityType.ContentItem,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = ApprovalStatus.Submitted,
            });

            // when
            await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId: approvalId,
                decision: ApprovalDecision.Approve,
                isBypassRequested: false,
                bypassReason: null,
                securityContext: CreateAuthenticatedSecurityContext(),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.IsSubjectDeleted.Should().BeTrue();
        }
    }
}
