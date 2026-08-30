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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // A review whose approval cannot be found fails closed. It must not reach the decision
        // function at all, because an ungathered review list there reads as the permissive answer.
        [Fact]
        public async Task ShouldRefuseAndNotAskTheAccessClientWhenTheApprovalIsNotFoundAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval)null);

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayRecordApprovalReviewAsync(
                approvalId,
                isAmendingOwnReview: false,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason
                .Should().Be(AccessDenialReason.ApprovalNotOpenForReview);

            actualVerdict.Explanation.Should().Contain(approvalId.ToString());
            this.capturedRecordReviewRequest.Should().BeNull();

            this.accessClientMock.Verify(client =>
                client.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Never);

            VerifyTheActorWasResolvedFor(securityContext);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.auditClientMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        // The self-review bar compares the actor against the ENTITY's author, and only the entity
        // row carries it. The content type comes back on the same read, which is what lets a
        // review role scoped to one content type be recognised.
        //
        // Association is absent on purpose — it is the one type authorised from something other
        // than itself, so it yields TWO subjects and has its own test below.
        [Theory]
        [InlineData(EntityType.ContentItem, "Testimony")]
        [InlineData(EntityType.Tag, null)]
        [InlineData(EntityType.Reaction, null)]
        [InlineData(EntityType.BibleReference, null)]
        [InlineData(EntityType.Comment, null)]
        [InlineData(EntityType.Link, null)]
        [InlineData(EntityType.Attachment, null)]
        public async Task ShouldTraverseToTheEntityAuthorOnRecordReviewAsync(
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
            await this.accessBroker.MayRecordApprovalReviewAsync(
                approvalId,
                isAmendingOwnReview: false,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedRecordReviewRequest.EntityCreatedBy
                .Should().Be("the-entity-author");

            this.capturedRecordReviewRequest.RoleSubjects.Should().ContainSingle();

            this.capturedRecordReviewRequest.RoleSubjects.Single().EntityType
                .Should().Be(entityType.ToString());

            this.capturedRecordReviewRequest.RoleSubjects.Single().ContentType
                .Should().Be(expectedContentType);

            VerifyEntityAuthorRead(entityType, entityId);
            VerifyRecordReviewGatherReads(approvalId);

            this.auditClientMock.Verify(client =>
                client.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.auditClientMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// An association is authorised from its two endpoints and from nothing else (§14.7
        /// posture A′ rule 2), so the decision must name both and holding a role for either is
        /// enough.
        ///
        /// <para>The subject that must NOT appear is <c>Association</c> itself. Composing one
        /// from the approval's own <c>EntityType</c> — which is what this used to do — asks
        /// whether the actor holds <c>Association-Reviewers</c>, a role
        /// <c>Roles.cs</c> deliberately never issues. That is why this half of the gate failed
        /// closed rather than open: an endpoint-scoped publisher was refused along with everyone
        /// else, and only a global role got through.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNameBothEndpointsAsRoleSubjectsForAnAssociationOnRecordReviewAsync()
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
            await this.accessBroker.MayRecordApprovalReviewAsync(
                approvalId,
                isAmendingOwnReview: false,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedRecordReviewRequest.RoleSubjects.Should().HaveCount(2);

            this.capturedRecordReviewRequest.RoleSubjects.Should().SatisfyRespectively(
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

            this.capturedRecordReviewRequest.RoleSubjects.Should().NotContain(subject =>
                subject.EntityType == nameof(EntityType.Association));

            this.capturedRecordReviewRequest.EntityCreatedBy
                .Should().Be("the-entity-author");

            VerifyEntityAuthorRead(EntityType.Association, entityId);
            VerifyRecordReviewGatherReads(approvalId);

            this.auditClientMock.Verify(client =>
                client.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.auditClientMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// A missing association leaves no endpoints to derive, so the fallback is the coarse
        /// <c>Association</c> subject — which no role can match, because none is issued. That is
        /// the fail-closed answer stated positively: the gate refuses on role grounds rather than
        /// silently emitting an empty subject list, which
        /// <c>HasReviewTier</c>'s <c>.Any()</c> would read as "no scoped route" and skip past.
        /// </summary>
        [Fact]
        public async Task ShouldFallBackToTheCoarseSubjectWhenTheAssociationIsMissingOnRecordReviewAsync()
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

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(entityId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Glory2Him.Core.Models.Foundations.Associations.Association)null);

            // when
            await this.accessBroker.MayRecordApprovalReviewAsync(
                approvalId,
                isAmendingOwnReview: false,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedRecordReviewRequest.RoleSubjects.Should().ContainSingle();

            this.capturedRecordReviewRequest.RoleSubjects.Single().EntityType
                .Should().Be(nameof(EntityType.Association));

            this.capturedRecordReviewRequest.EntityCreatedBy.Should().BeEmpty();
        }

        // An entity row that has gone missing leaves the author unknown. Empty is the fail-closed
        // answer: the decision never treats blank as matching blank, so it cannot pass HR-1.
        [Fact]
        public async Task ShouldSendAnEmptyAuthorWhenTheEntityRowIsMissingOnRecordReviewAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId,
                EntityType.ContentItem,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(entityId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Glory2Him.Core.Models.Foundations.ContentItems.ContentItem)null);

            // when
            await this.accessBroker.MayRecordApprovalReviewAsync(
                approvalId,
                isAmendingOwnReview: false,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedRecordReviewRequest.EntityCreatedBy.Should().BeEmpty();

            this.capturedRecordReviewRequest.RoleSubjects.Single().ContentType
                .Should().BeNull();

            VerifyEntityAuthorRead(EntityType.ContentItem, entityId);
            VerifyRecordReviewGatherReads(approvalId);

            this.auditClientMock.Verify(client =>
                client.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.auditClientMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldResolveTheActorFromTheAuditClientOnRecordReviewAsync()
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
            await this.accessBroker.MayRecordApprovalReviewAsync(
                approvalId,
                isAmendingOwnReview: false,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            this.capturedRecordReviewRequest.Actor.UserId
                .Should().Be(this.auditResolvedUserId);

            this.capturedRecordReviewRequest.Actor.UserId
                .Should().NotBe(securityContext.SubjectId);

            this.capturedRecordReviewRequest.Actor.Roles.Should().BeEquivalentTo(roles);
            this.capturedRecordReviewRequest.Actor.IsAuthenticated.Should().BeTrue();

            VerifyEntityAuthorRead(EntityType.ContentItem, entityId);
            VerifyRecordReviewGatherReads(approvalId);

            VerifyTheActorWasResolvedFor(securityContext);

            this.accessClientMock.Verify(client =>
                client.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.auditClientMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        // The one-active-review-per-reviewer rule is applied over these rows, so a dismissed or
        // soft-deleted one has to arrive intact rather than pre-filtered.
        [Fact]
        public async Task ShouldSendTheApprovalStateAndUnfilteredReviewsOnRecordReviewAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid otherApprovalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId,
                EntityType.Tag,
                entityId,
                ApprovalStatus.Submitted);

            ApprovalReview activeReview = CreateApprovalReview(
                approvalId,
                createdBy: "reviewer-active",
                statusId: ApprovalStatus.Approved);

            ApprovalReview dismissedReview = CreateApprovalReview(
                approvalId,
                createdBy: "reviewer-dismissed",
                statusId: ApprovalStatus.Dismissed);

            ApprovalReview deletedReview = CreateApprovalReview(
                approvalId,
                createdBy: "reviewer-deleted",
                statusId: ApprovalStatus.Rejected,
                isDeleted: true);

            ApprovalReview foreignReview = CreateApprovalReview(
                otherApprovalId,
                createdBy: "reviewer-on-another-approval",
                statusId: ApprovalStatus.Approved);

            SetupApprovalById(approval);
            SetupApprovalReviews(activeReview, dismissedReview, deletedReview, foreignReview);
            SetupEntityAuthor(EntityType.Tag, entityId, createdBy: "the-tag-author");

            // when
            await this.accessBroker.MayRecordApprovalReviewAsync(
                approvalId,
                isAmendingOwnReview: true,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            this.capturedRecordReviewRequest.ApprovalState
                .Should().Be(ApprovalState.Submitted);

            this.capturedRecordReviewRequest.IsAmendingOwnReview.Should().BeTrue();
            this.capturedRecordReviewRequest.ExistingReviews.Should().HaveCount(3);

            this.capturedRecordReviewRequest.ExistingReviews.Should().ContainSingle(review =>
                review.CreatedBy == "reviewer-dismissed"
                    && review.Verdict == ReviewVerdict.Dismissed
                    && review.IsDeleted == false);

            this.capturedRecordReviewRequest.ExistingReviews.Should().ContainSingle(review =>
                review.CreatedBy == "reviewer-deleted"
                    && review.Verdict == ReviewVerdict.Rejected
                    && review.IsDeleted == true);

            this.capturedRecordReviewRequest.ExistingReviews.Should().NotContain(review =>
                review.CreatedBy == "reviewer-on-another-approval");

            VerifyEntityAuthorRead(EntityType.Tag, entityId);
            VerifyRecordReviewGatherReads(approvalId);

            this.auditClientMock.Verify(client =>
                client.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.auditClientMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnTheRecordReviewVerdictUnchangedAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            var refusedVerdict = new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = AccessDenialReason.SelfReviewNeverPermitted,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "the actor authored the content",
            };

            SetupAccessClientToReturn(refusedVerdict);

            Approval approval = CreateApproval(
                approvalId,
                EntityType.ContentItem,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);
            SetupEntityAuthor(EntityType.ContentItem, entityId, createdBy: "the-entity-author");

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayRecordApprovalReviewAsync(
                approvalId,
                isAmendingOwnReview: false,
                CreateAuthenticatedSecurityContext(),
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.Should().BeSameAs(refusedVerdict);

            VerifyEntityAuthorRead(EntityType.ContentItem, entityId);
            VerifyRecordReviewGatherReads(approvalId);

            this.auditClientMock.Verify(client =>
                client.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayRecordApprovalReviewAsync(It.IsAny<RecordReviewRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.auditClientMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        private void VerifyRecordReviewGatherReads(Guid approvalId)
        {
            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
        }
    }
}
