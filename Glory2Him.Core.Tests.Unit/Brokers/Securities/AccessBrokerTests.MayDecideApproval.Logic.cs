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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        // The self-approval bar compares the actor against CreatedBy, and CreatedBy is stamped by
        // the audit broker. Resolving the actor from SecurityContext.SubjectId instead would make
        // that comparison answer "not the author" for the author.
        [Fact]
        public async Task ShouldResolveTheActorFromTheSecurityAuditBrokerOnDecideAsync()
        {
            // given
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.ContentItem,
                entityId: Guid.NewGuid(),
                securityContext: securityContext);

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.Actor.UserId
                .Should().Be(this.auditResolvedUserId);

            this.capturedDecideApprovalRequest.Actor.UserId
                .Should().NotBe(securityContext.SubjectId);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldCarryRolesAndAuthenticationFromTheSecurityContextOnDecideAsync(
            bool isAuthenticated)
        {
            // given
            var roles = new List<string> { Roles.Publisher, "ContentItem-Testimony-Reviewer" };

            SecurityContext securityContext = CreateSecurityContext(
                roles: roles,
                isAuthenticated: isAuthenticated);

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.ContentItem,
                entityId: Guid.NewGuid(),
                securityContext: securityContext);

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.Actor.Roles
                .Should().BeEquivalentTo(roles);

            this.capturedDecideApprovalRequest.Actor.IsAuthenticated
                .Should().Be(isAuthenticated);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        // Which reviews and comments count is a rule, and the rules live in the decision function.
        // A dismissed or soft-deleted row filtered out here would make half the decision silently.
        [Fact]
        public async Task ShouldGatherDismissedAndDeletedReviewsAndCommentsUnfilteredOnDecideAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId,
                EntityType.ContentItem,
                entityId,
                ApprovalStatus.Submitted);

            ApprovalReview activeReview = CreateApprovalReview(
                approvalId,
                reviewerId: "reviewer-active",
                statusId: ApprovalStatus.Approved,
                isDeleted: false,
                createdBy: "author-of-active-row");

            ApprovalReview dismissedReview = CreateApprovalReview(
                approvalId,
                reviewerId: "reviewer-dismissed",
                statusId: ApprovalStatus.Dismissed);

            ApprovalReview deletedReview = CreateApprovalReview(
                approvalId,
                reviewerId: "reviewer-deleted",
                statusId: ApprovalStatus.Rejected,
                isDeleted: true);

            ApprovalComment openComment = CreateApprovalComment(approvalId, isResolved: false);

            ApprovalComment deletedComment = CreateApprovalComment(
                approvalId,
                isResolved: true,
                isDeleted: true);

            SetupApprovals(approval);
            SetupApprovalReviews(activeReview, dismissedReview, deletedReview);
            SetupApprovalComments(openComment, deletedComment);

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.ContentItem,
                entityId: entityId,
                securityContext: CreateAuthenticatedSecurityContext());

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.Reviews.Should().HaveCount(3);

            this.capturedDecideApprovalRequest.Reviews.Should().ContainSingle(review =>
                review.ReviewerId == "reviewer-active"
                    && review.CreatedBy == "author-of-active-row"
                    && review.Verdict == ReviewVerdict.Approved
                    && review.IsDeleted == false);

            this.capturedDecideApprovalRequest.Reviews.Should().ContainSingle(review =>
                review.ReviewerId == "reviewer-dismissed"
                    && review.Verdict == ReviewVerdict.Dismissed
                    && review.IsDeleted == false);

            this.capturedDecideApprovalRequest.Reviews.Should().ContainSingle(review =>
                review.ReviewerId == "reviewer-deleted"
                    && review.Verdict == ReviewVerdict.Rejected
                    && review.IsDeleted == true);

            this.capturedDecideApprovalRequest.Comments.Should().HaveCount(2);

            this.capturedDecideApprovalRequest.Comments.Should().ContainSingle(comment =>
                comment.IsResolved == false && comment.IsDeleted == false);

            this.capturedDecideApprovalRequest.Comments.Should().ContainSingle(comment =>
                comment.IsResolved == true && comment.IsDeleted == true);

            VerifyDecideStorageReadsForAnExistingApproval();

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldExcludeReviewsAndCommentsOfAnotherApprovalOnDecideAsync()
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

            ApprovalReview ownReview = CreateApprovalReview(
                approvalId,
                reviewerId: "reviewer-on-this-approval",
                statusId: ApprovalStatus.Approved);

            ApprovalReview foreignReview = CreateApprovalReview(
                otherApprovalId,
                reviewerId: "reviewer-on-another-approval",
                statusId: ApprovalStatus.Approved);

            ApprovalComment ownComment = CreateApprovalComment(approvalId, isResolved: false);
            ApprovalComment foreignComment = CreateApprovalComment(otherApprovalId, isResolved: true);

            SetupApprovals(approval);
            SetupApprovalReviews(ownReview, foreignReview);
            SetupApprovalComments(ownComment, foreignComment);

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.Tag,
                entityId: entityId,
                securityContext: CreateAuthenticatedSecurityContext());

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.Reviews.Should().ContainSingle(review =>
                review.ReviewerId == "reviewer-on-this-approval");

            this.capturedDecideApprovalRequest.Comments.Should().ContainSingle(comment =>
                comment.IsResolved == false);

            VerifyDecideStorageReadsForAnExistingApproval();

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        // Settings ARE filtered, unlike reviews and comments: a deleted row is skipped at every
        // tier, so it is not a candidate at all rather than a candidate the decision discounts.
        [Fact]
        public async Task ShouldOnlyOfferLiveSettingsOfTheSameEntityTypeAsCandidatePoliciesAsync()
        {
            // given
            Guid entityId = Guid.NewGuid();

            var narrowSetting = new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.ContentItem,
                ContentType = ContentType.Story,
                RequireApprovals = true,
                RequiredNumberOfApprovals = 3,
                AutoApproveIfAllApprovalRequirementsMet = true,
                AllowSelfApproval = true,
                BlockOnReject = true,
                BlockOnZeroApprovalScore = true,
                RequireReapprovalOnChange = false,
                RequireReviewCommentResolutionBeforeApprovals = false,
                DoNotAllowBypassingSettings = true,
                IsDeleted = false,
            };

            var defaultTierSetting = new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.ContentItem,
                ContentType = null,
                IsDeleted = false,
            };

            var deletedSetting = new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.ContentItem,
                ContentType = ContentType.Quote,
                IsDeleted = true,
            };

            var otherEntityTypeSetting = new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.Tag,
                ContentType = null,
                IsDeleted = false,
            };

            SetupApprovalSettings(
                narrowSetting,
                defaultTierSetting,
                deletedSetting,
                otherEntityTypeSetting);

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.ContentItem,
                entityId: entityId,
                securityContext: CreateAuthenticatedSecurityContext());

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.CandidatePolicies.Should().HaveCount(2);

            this.capturedDecideApprovalRequest.CandidatePolicies
                .Should().ContainSingle(policy => policy.ContentType == null);

            ApprovalPolicy narrowPolicy = this.capturedDecideApprovalRequest.CandidatePolicies
                .Single(policy => policy.ContentType == "Story");

            narrowPolicy.EntityType.Should().Be("ContentItem");
            narrowPolicy.RequireApprovals.Should().BeTrue();
            narrowPolicy.RequiredNumberOfApprovals.Should().Be(3);
            narrowPolicy.AutoApproveIfAllApprovalRequirementsMet.Should().BeTrue();
            narrowPolicy.AllowSelfApproval.Should().BeTrue();
            narrowPolicy.BlockOnReject.Should().BeTrue();
            narrowPolicy.BlockOnZeroApprovalScore.Should().BeTrue();
            narrowPolicy.RequireReapprovalOnChange.Should().BeFalse();
            narrowPolicy.RequireReviewCommentResolutionBeforeApprovals.Should().BeFalse();
            narrowPolicy.DoNotAllowBypassingSettings.Should().BeTrue();

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        // Draft and Submitted are states an ApprovalReview row can never legally hold. A stored
        // row carrying one is corrupt, and counting it as an approval would let corruption meet a
        // threshold — so anything that is not an explicit Approved or Rejected is Dismissed.
        [Theory]
        [InlineData(ApprovalStatus.Approved, ReviewVerdict.Approved)]
        [InlineData(ApprovalStatus.Rejected, ReviewVerdict.Rejected)]
        [InlineData(ApprovalStatus.Dismissed, ReviewVerdict.Dismissed)]
        [InlineData(ApprovalStatus.Draft, ReviewVerdict.Dismissed)]
        [InlineData(ApprovalStatus.Submitted, ReviewVerdict.Dismissed)]
        public async Task ShouldMapReviewStatusToReviewVerdictOnDecideAsync(
            ApprovalStatus reviewStatus,
            ReviewVerdict expectedVerdict)
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId,
                EntityType.ContentItem,
                entityId,
                ApprovalStatus.Submitted);

            ApprovalReview approvalReview = CreateApprovalReview(
                approvalId,
                reviewerId: "the-reviewer",
                statusId: reviewStatus);

            SetupApprovals(approval);
            SetupApprovalReviews(approvalReview);

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.ContentItem,
                entityId: entityId,
                securityContext: CreateAuthenticatedSecurityContext());

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.Reviews.Should().ContainSingle();

            this.capturedDecideApprovalRequest.Reviews.Single().Verdict
                .Should().Be(expectedVerdict);

            VerifyDecideStorageReadsForAnExistingApproval();

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        // Dismissed is a verdict a review holds and an approval never does. Should one appear on
        // an approval row it lands on Draft — the state from which nothing may be decided.
        [Theory]
        [InlineData(ApprovalStatus.Submitted, ApprovalState.Submitted)]
        [InlineData(ApprovalStatus.Approved, ApprovalState.Approved)]
        [InlineData(ApprovalStatus.Rejected, ApprovalState.Rejected)]
        [InlineData(ApprovalStatus.Draft, ApprovalState.Draft)]
        [InlineData(ApprovalStatus.Dismissed, ApprovalState.Draft)]
        public async Task ShouldMapApprovalStatusToApprovalStateOnDecideAsync(
            ApprovalStatus approvalStatus,
            ApprovalState expectedApprovalState)
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval approval = CreateApproval(
                approvalId,
                EntityType.ContentItem,
                entityId,
                approvalStatus);

            SetupApprovals(approval);

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.ContentItem,
                entityId: entityId,
                securityContext: CreateAuthenticatedSecurityContext());

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.ApprovalState
                .Should().Be(expectedApprovalState);

            VerifyDecideStorageReadsForAnExistingApproval();

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        // No approval row means no open round. Inventing Submitted here would let a decision be
        // applied to an entity that never entered review.
        [Fact]
        public async Task ShouldSendDraftWithNoReviewsWhenNoApprovalRowExistsAsync()
        {
            // given
            Guid entityId = Guid.NewGuid();
            Guid anotherEntityId = Guid.NewGuid();

            Approval approvalOfAnotherEntity = CreateApproval(
                Guid.NewGuid(),
                EntityType.ContentItem,
                anotherEntityId,
                ApprovalStatus.Submitted);

            Approval approvalOfAnotherEntityType = CreateApproval(
                Guid.NewGuid(),
                EntityType.Tag,
                entityId,
                ApprovalStatus.Submitted);

            SetupApprovals(approvalOfAnotherEntity, approvalOfAnotherEntityType);

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.ContentItem,
                entityId: entityId,
                securityContext: CreateAuthenticatedSecurityContext());

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.ApprovalState
                .Should().Be(ApprovalState.Draft);

            this.capturedDecideApprovalRequest.Reviews.Should().BeEmpty();
            this.capturedDecideApprovalRequest.Comments.Should().BeEmpty();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                    Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()),
                    Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        // A closed approval still occupies the (EntityType, EntityId) key. Filtering the lookup on
        // IsDeleted would answer "no round" for a row that has one.
        [Fact]
        public async Task ShouldFindASoftDeletedApprovalOnDecideAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid entityId = Guid.NewGuid();

            Approval softDeletedApproval = CreateApproval(
                approvalId,
                EntityType.ContentItem,
                entityId,
                ApprovalStatus.Submitted,
                isDeleted: true);

            ApprovalReview approvalReview = CreateApprovalReview(
                approvalId,
                reviewerId: "reviewer-on-the-deleted-approval",
                statusId: ApprovalStatus.Approved);

            SetupApprovals(softDeletedApproval);
            SetupApprovalReviews(approvalReview);

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.ContentItem,
                entityId: entityId,
                securityContext: CreateAuthenticatedSecurityContext());

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.ApprovalState
                .Should().Be(ApprovalState.Submitted);

            this.capturedDecideApprovalRequest.Reviews.Should().ContainSingle(review =>
                review.ReviewerId == "reviewer-on-the-deleted-approval");

            VerifyDecideStorageReadsForAnExistingApproval();

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldCarryTheQuerySuppliedFieldsOntoTheDecideRequestAsync()
        {
            // given
            Guid entityId = Guid.NewGuid();

            var roleSubjects = new List<RoleSubject>
            {
                new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                new RoleSubject { EntityType = "Tag", ContentType = null },
            };

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.Association,
                entityId: entityId,
                securityContext: CreateAuthenticatedSecurityContext(),
                contentType: ContentType.BibleStudy,
                roleSubjects: roleSubjects,
                entityCreatedBy: "the-entity-author",
                confidenceScore: 0.75m,
                decision: ApprovalDecision.Reject,
                isBypassRequested: true,
                bypassReason: "the recorded reason");

            // when
            await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            this.capturedDecideApprovalRequest.EntityType.Should().Be("Association");
            this.capturedDecideApprovalRequest.ContentType.Should().Be("BibleStudy");
            this.capturedDecideApprovalRequest.EntityCreatedBy.Should().Be("the-entity-author");
            this.capturedDecideApprovalRequest.ConfidenceScore.Should().Be(0.75m);
            this.capturedDecideApprovalRequest.Decision.Should().Be(ApprovalDecision.Reject);
            this.capturedDecideApprovalRequest.IsBypassRequested.Should().BeTrue();
            this.capturedDecideApprovalRequest.BypassReason.Should().Be("the recorded reason");
            this.capturedDecideApprovalRequest.RoleSubjects.Should().BeSameAs(roleSubjects);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        // The broker gathers; it never second-guesses the answer.
        [Fact]
        public async Task ShouldReturnTheDecideVerdictUnchangedAsync()
        {
            // given
            var refusedVerdict = new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = AccessDenialReason.ApprovalThresholdNotMet,
                Explanation = "2 of 3 approvals",
            };

            SetupAccessClientToReturn(refusedVerdict);

            ApprovalDecisionQuery approvalDecisionQuery = CreateApprovalDecisionQuery(
                entityType: EntityType.ContentItem,
                entityId: Guid.NewGuid(),
                securityContext: CreateAuthenticatedSecurityContext());

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayDecideApprovalAsync(
                approvalDecisionQuery,
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.Should().BeSameAs(refusedVerdict);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.accessClientMock.Verify(client =>
                client.MayDecideApprovalAsync(It.IsAny<DecideApprovalRequest>()),
                    Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessClientMock.VerifyNoOtherCalls();
        }

        private void VerifyDecideStorageReadsForAnExistingApproval()
        {
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalReviewsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalCommentsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
        }
    }
}
