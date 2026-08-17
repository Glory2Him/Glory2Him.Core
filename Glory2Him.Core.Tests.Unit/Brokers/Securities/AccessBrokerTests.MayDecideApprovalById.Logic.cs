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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // A decision on an approval that cannot be found fails closed and never reaches the
        // decision function — an ungathered policy or subject list there would be read as
        // rules that do not exist.
        [Fact]
        public async Task ShouldRefuseDecidingWhenTheApprovalIsMissingAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval)null);

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId,
                ApprovalDecision.Approve,
                isBypassRequested: false,
                bypassReason: null,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.ParentApprovalUnavailable);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The bypass inputs and the decision travel to the decision function verbatim — they are
        /// the caller's REQUEST, and the decision is what turns them into an outcome. The actor is
        /// the audit-resolved one, for the same reason as every other gate: the self-approval bar
        /// compares it against a <c>CreatedBy</c> the same resolver stamped.
        /// </summary>
        [Theory]
        [InlineData(ApprovalDecision.Approve)]
        [InlineData(ApprovalDecision.Reject)]
        public async Task ShouldPassTheDecisionAndBypassRequestThroughUnchangedAsync(
            ApprovalDecision decision)
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            string bypassReason = "the reviewers are unavailable and the launch is today";
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            Approval approval = CreateApproval(
                approvalId,
                EntityType.Tag,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.Tag, entityId, createdBy: "the-entity-author");
            SetupApprovalReviews();
            SetupApprovalComments();
            SetupApprovalSettings();

            // when
            await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId,
                decision,
                isBypassRequested: true,
                bypassReason: bypassReason,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.Decision.Should().Be(decision);
            this.capturedDecideApprovalRequest.IsBypassRequested.Should().BeTrue();
            this.capturedDecideApprovalRequest.BypassReason.Should().Be(bypassReason);
            this.capturedDecideApprovalRequest.EntityCreatedBy.Should().Be("the-entity-author");
            this.capturedDecideApprovalRequest.Actor.UserId.Should().Be(this.auditResolvedUserId);

            VerifyTheActorWasResolvedFor(securityContext);
        }

        /// <summary>
        /// The policy key comes off the STORED approval's target: the entity type always, and the
        /// content type only when the entity is a <c>ContentItem</c> — the one type that scopes
        /// its policies that way (§8.4).
        /// </summary>
        [Theory]
        [InlineData(EntityType.ContentItem, "Testimony")]
        [InlineData(EntityType.Tag, null)]
        public async Task ShouldNameTheStoredEntitysPolicyKeyOnDecideAsync(
            EntityType entityType,
            string expectedContentType)
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId,
                entityType,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(entityType, entityId, createdBy: "the-entity-author");
            SetupApprovalReviews();
            SetupApprovalComments();
            SetupApprovalSettings();

            // when
            await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId,
                ApprovalDecision.Approve,
                isBypassRequested: false,
                bypassReason: null,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.EntityType.Should().Be(entityType.ToString());
            this.capturedDecideApprovalRequest.ContentType.Should().Be(expectedContentType);
        }

        /// <summary>
        /// An association is decided from its two endpoints (§14.7 posture A′ rule 2), its policy
        /// key stays its own type, and its confidence score rides along — it is the one entity
        /// with a score, and the zero-score block reads it.
        /// </summary>
        [Fact]
        public async Task ShouldNameBothEndpointsAndTheStoredScoreForAnAssociationOnDecideAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId,
                EntityType.Association,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupApprovalReviews();
            SetupApprovalComments();
            SetupApprovalSettings();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(entityId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Association
                    {
                        Id = entityId,
                        CreatedBy = "the-entity-author",
                        EntityAType = EntityType.ContentItem,
                        EntityAContentType = ContentType.Testimony,
                        EntityBType = EntityType.BibleReference,
                        EntityBContentType = null,
                        ConfidenceScore = 7.5m,
                    });

            // when
            await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId,
                ApprovalDecision.Approve,
                isBypassRequested: false,
                bypassReason: null,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.RoleSubjects.Should().SatisfyRespectively(
                first =>
                {
                    first.EntityType.Should().Be(nameof(EntityType.ContentItem));
                    first.ContentType.Should().Be(nameof(ContentType.Testimony));
                },
                second =>
                {
                    second.EntityType.Should().Be(nameof(EntityType.BibleReference));
                    second.ContentType.Should().BeNull();
                });

            this.capturedDecideApprovalRequest.ContentType.Should().BeNull();
            this.capturedDecideApprovalRequest.ConfidenceScore.Should().Be(7.5m);
        }

        /// <summary>
        /// The gate's whole job is to relay the decision, so the refusal must come back
        /// unchanged — a broker that asked and then ignored the answer would permit every
        /// outcome while the suite stayed green.
        /// </summary>
        [Fact]
        public async Task ShouldReturnTheDecisionVerdictUnchangedOnDecideByIdAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            var refusedVerdict = new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = AccessDenialReason.NotInPublisherTier,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "the actor holds no publisher-tier role for this entity",
            };

            SetupAccessClientToReturn(refusedVerdict);

            Approval approval = CreateApproval(
                approvalId,
                EntityType.Tag,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.Tag, entityId, createdBy: "the-entity-author");
            SetupApprovalReviews();
            SetupApprovalComments();
            SetupApprovalSettings();

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayDecideApprovalByIdAsync(
                approvalId,
                ApprovalDecision.Approve,
                isBypassRequested: false,
                bypassReason: null,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.Should().BeSameAs(refusedVerdict);
        }
    }
}
