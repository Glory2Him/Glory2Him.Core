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
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // An amendment whose approval cannot be found fails closed, and must not reach the
        // decision function — an ungathered subject list there reads as "no scoped route", which
        // HasReviewTier's .Any() skips straight past.
        [Fact]
        public async Task ShouldRefuseAmendmentWhenTheApprovalIsMissingAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval)null);

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayAmendApprovalAsync(
                approvalId,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.ParentApprovalUnavailable);

            this.accessClientMock.Verify(client =>
                client.MayAmendApprovalAsync(It.IsAny<AmendApprovalRequest>()),
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
        /// The subject comes from the approval's target, which is the point: the foundation's
        /// row-local suffix match cannot tell a <c>Tag-Reviewers</c> from a reviewer for the
        /// entity actually under approval, and this is the read that can.
        /// </summary>
        [Theory]
        [InlineData(EntityType.ContentItem, "Testimony")]
        [InlineData(EntityType.Tag, null)]
        [InlineData(EntityType.BibleReference, null)]
        public async Task ShouldNameTheApprovalsOwnSubjectOnAmendmentAsync(
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
            await this.accessBroker.MayAmendApprovalAsync(
                approvalId,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedAmendApprovalRequest.RoleSubjects.Should().ContainSingle();

            this.capturedAmendApprovalRequest.RoleSubjects.Single().EntityType
                .Should().Be(entityType.ToString());

            this.capturedAmendApprovalRequest.RoleSubjects.Single().ContentType
                .Should().Be(expectedContentType);
        }

        /// <summary>
        /// An association names both endpoints here too (§14.7 posture A′ rule 2). Without this,
        /// amending an association's approval would ask for <c>Association-Reviewers</c> — a role
        /// nobody can hold — and only a global reviewer would ever get through.
        /// </summary>
        [Fact]
        public async Task ShouldNameBothEndpointsAsRoleSubjectsForAnAssociationOnAmendmentAsync()
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
            await this.accessBroker.MayAmendApprovalAsync(
                approvalId,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedAmendApprovalRequest.RoleSubjects.Should().HaveCount(2);

            this.capturedAmendApprovalRequest.RoleSubjects.Should().SatisfyRespectively(
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

            this.capturedAmendApprovalRequest.RoleSubjects.Should().NotContain(subject =>
                subject.EntityType == nameof(EntityType.Association));
        }

        /// <summary>
        /// The submitter is the person who wrote the ENTITY, read from storage and never from a
        /// caller's copy — it is what admits the owner, so a payload-supplied value would let
        /// anyone name themselves the submitter and clear the gate on somebody else's approval.
        ///
        /// <para>Not <c>Approval.CreatedBy</c>. The workflow owns Approval rows outright — it
        /// opens them itself when content is submitted — so that column records the system and
        /// never a person. Anchoring the owner branch there would refuse every author their own
        /// resubmission, silently, since §14.7 posture D rule 3 admits the submitter precisely
        /// because they hold no role to fall back on.</para>
        /// </summary>
        [Fact]
        public async Task ShouldSendTheStoredEntityAuthorOnAmendmentAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId,
                EntityType.Tag,
                entityId,
                ApprovalStatus.Submitted);

            // What the workflow really writes there, and precisely why it cannot be the anchor.
            approval.CreatedBy = SystemIdentity.UserId;

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.Tag, entityId, createdBy: "the-entity-author");

            // when
            await this.accessBroker.MayAmendApprovalAsync(
                approvalId,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then: the ENTITY's author. Pinned against the system token rather than against
            // another name, so the test fails loudly if the anchor ever moves back.
            this.capturedAmendApprovalRequest.EntityCreatedBy
                .Should().Be("the-entity-author");

            this.capturedAmendApprovalRequest.EntityCreatedBy
                .Should().NotBe(SystemIdentity.UserId);
        }

        /// <summary>
        /// The gate's whole job is to relay the decision, so the refusal must come back unchanged.
        /// Without this a broker that asked the decision function and then ignored its answer
        /// would permit every amendment and leave the suite green, because the fixture's default
        /// verdict is permitted.
        /// </summary>
        [Fact]
        public async Task ShouldReturnTheAmendmentVerdictUnchangedAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            var refusedVerdict = new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = AccessDenialReason.NotInReviewTier,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "the actor is not in the review tier for this entity",
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
            AccessVerdict actualVerdict = await this.accessBroker.MayAmendApprovalAsync(
                approvalId,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.Should().BeSameAs(refusedVerdict);
        }

        /// <summary>
        /// The actor is resolved through the audit surface, not read off the context. The roles
        /// are the whole input to the review tier, so an actor built with an empty list refuses
        /// every amendment while looking correct; and the user id must come from the same
        /// resolver that stamped <c>CreatedBy</c>, or every actor-versus-author comparison in the
        /// system is meaningless (<c>SubjectId</c> is deliberately a different value here).
        /// </summary>
        [Fact]
        public async Task ShouldResolveTheActorFromTheAuditClientOnAmendmentAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();
            var roles = new[] { "ContentItem-Testimony-Reviewers" };

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
            await this.accessBroker.MayAmendApprovalAsync(
                approvalId,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            this.capturedAmendApprovalRequest.Actor.UserId
                .Should().Be(this.auditResolvedUserId);

            this.capturedAmendApprovalRequest.Actor.UserId
                .Should().NotBe(securityContext.SubjectId);

            this.capturedAmendApprovalRequest.Actor.Roles.Should().BeEquivalentTo(roles);
            this.capturedAmendApprovalRequest.Actor.IsAuthenticated.Should().BeTrue();

            VerifyTheActorWasResolvedFor(securityContext);
        }

        /// <summary>
        /// The amendment decision reads neither the round nor the reviews — §14.7 posture D rule 3
        /// has reviewers move the status through this very path, so a decision consulting the
        /// window would refuse the operation's purpose. This pins the gather to the two reads it
        /// needs, so a later change cannot quietly widen what the decision sees.
        /// </summary>
        [Fact]
        public async Task ShouldNotGatherTheRoundWindowOrReviewsOnAmendmentAsync()
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
            await this.accessBroker.MayAmendApprovalAsync(
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
