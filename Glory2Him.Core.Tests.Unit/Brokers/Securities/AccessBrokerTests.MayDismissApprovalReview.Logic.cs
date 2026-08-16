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
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // A dismissal whose approval cannot be found fails closed, and must not reach the decision
        // function — an ungathered subject list there reads as "no scoped route", which
        // HasPublisherTier's .Any() skips straight past.
        [Fact]
        public async Task ShouldRefuseDismissalWhenTheApprovalIsMissingAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval)null);

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayDismissApprovalReviewAsync(
                approvalId,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.ParentApprovalUnavailable);

            this.accessClientMock.Verify(client =>
                client.MayDismissApprovalReviewAsync(It.IsAny<DismissReviewRequest>()),
                    Times.Never);

            VerifyTheActorWasResolvedFor(securityContext);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.auditClientMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The subject is derived from the approval's target, which is the whole point: the
        /// foundation's row-local suffix match cannot tell a <c>Tag-Publisher</c> from a publisher
        /// for the entity actually under review, and this is the read that can.
        /// </summary>
        [Theory]
        [InlineData(EntityType.ContentItem, "Testimony")]
        [InlineData(EntityType.Tag, null)]
        [InlineData(EntityType.BibleReference, null)]
        public async Task ShouldNameTheApprovalsOwnSubjectOnDismissalAsync(
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

            // when
            await this.accessBroker.MayDismissApprovalReviewAsync(
                approvalId,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedDismissReviewRequest.RoleSubjects.Should().ContainSingle();

            this.capturedDismissReviewRequest.RoleSubjects.Single().EntityType
                .Should().Be(entityType.ToString());

            this.capturedDismissReviewRequest.RoleSubjects.Single().ContentType
                .Should().Be(expectedContentType);
        }

        /// <summary>
        /// An association names both endpoints here too (§14.7 posture A′ rule 2). Without this,
        /// dismissal on an association's approval would ask for <c>Association-Publisher</c> — a
        /// role nobody can hold — and only a global publisher would ever get through.
        /// </summary>
        [Fact]
        public async Task ShouldNameBothEndpointsAsRoleSubjectsForAnAssociationOnDismissalAsync()
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
            SetupEntityAuthor(EntityType.Association, entityId, createdBy: "the-entity-author");

            // when
            await this.accessBroker.MayDismissApprovalReviewAsync(
                approvalId,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedDismissReviewRequest.RoleSubjects.Should().HaveCount(2);

            this.capturedDismissReviewRequest.RoleSubjects.Should().SatisfyRespectively(
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

            this.capturedDismissReviewRequest.RoleSubjects.Should().NotContain(subject =>
                subject.EntityType == nameof(EntityType.Association));
        }

        /// <summary>
        /// The gate's whole job is to relay the decision, so the refusal must come back unchanged.
        /// Every other test here inspects the request that went out; without this one a broker
        /// that asked the decision function and then ignored its answer would permit every
        /// dismissal and leave the suite green, because the fixture's default verdict is permitted.
        /// </summary>
        [Fact]
        public async Task ShouldReturnTheDismissalVerdictUnchangedAsync()
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
                Explanation = "the actor is not in the publisher tier for this entity",
            };

            SetupAccessClientToReturn(refusedVerdict);

            Approval approval = CreateApproval(
                approvalId,
                EntityType.Tag,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.Tag, entityId, createdBy: "the-entity-author");

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayDismissApprovalReviewAsync(
                approvalId,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.Should().BeSameAs(refusedVerdict);
        }

        /// <summary>
        /// The actor is resolved through the audit surface, not read off the context. Both halves
        /// matter and neither is decoration: the roles are the whole input to the publisher tier,
        /// so an actor built with an empty list refuses every dismissal while looking correct; and
        /// the user id must come from the same resolver that stamped <c>CreatedBy</c>, because two
        /// resolvers make every actor-versus-author comparison in the system meaningless
        /// (<c>SubjectId</c> is deliberately a different value in this fixture so the wrong source
        /// cannot pass).
        /// </summary>
        [Fact]
        public async Task ShouldResolveTheActorFromTheAuditClientOnDismissalAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            var roles = new[] { "ContentItem-Testimony-Publisher" };

            SecurityContext securityContext = CreateSecurityContext(
                roles: roles,
                isAuthenticated: true);

            Approval approval = CreateApproval(
                approvalId,
                EntityType.ContentItem,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.ContentItem, entityId, createdBy: "the-entity-author");

            // when
            await this.accessBroker.MayDismissApprovalReviewAsync(
                approvalId,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDismissReviewRequest.Actor.UserId
                .Should().Be(this.auditResolvedUserId);

            this.capturedDismissReviewRequest.Actor.UserId
                .Should().NotBe(securityContext.SubjectId);

            this.capturedDismissReviewRequest.Actor.Roles.Should().BeEquivalentTo(roles);
            this.capturedDismissReviewRequest.Actor.IsAuthenticated.Should().BeTrue();

            VerifyTheActorWasResolvedFor(securityContext);
        }

        /// <summary>
        /// Dismissal consults neither the round window nor the existing reviews, and that is a
        /// rule rather than an omission: §8.8 fires it exactly as the round is re-opened by an
        /// amendment, so a decision that looked at either would refuse in the case it exists to
        /// serve. This pins the gather to the two reads it needs — the approval, and the entity
        /// behind it — so a later change cannot quietly widen what the decision sees.
        /// </summary>
        [Fact]
        public async Task ShouldNotGatherTheRoundWindowOrExistingReviewsOnDismissalAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId,
                EntityType.Tag,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.Tag, entityId, createdBy: "the-entity-author");

            // when
            await this.accessBroker.MayDismissApprovalReviewAsync(
                approvalId,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(entityId, It.IsAny<CancellationToken>()),
                    Times.Once);

            // No review gather, no settings resolution — the two reads above are the whole of it.
            this.storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}
