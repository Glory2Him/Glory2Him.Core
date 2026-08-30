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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Securities;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Access
{
    public partial class AccessServiceTests
    {
        [Fact]
        public async Task ShouldRefuseRecordingAReviewWhenTheActorIsNotAuthenticatedAsync()
        {
            // given
            AccessActor unauthenticatedActor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.Reviewers },
                isAuthenticated: false);

            RecordReviewRequest recordReviewRequest =
                CreateRandomRecordReviewRequest(actor: unauthenticatedActor);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldRefuseRecordingAReviewWhenTheActorUserIdIsBlankAsync(
            string? invalidUserId)
        {
            // given
            var actorWithoutUserId = new AccessActor
            {
                UserId = invalidUserId!,
                Roles = new List<string> { RoleNames.Reviewers },
                IsAuthenticated = true,
            };

            RecordReviewRequest recordReviewRequest =
                CreateRandomRecordReviewRequest(actor: actorWithoutUserId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Fact]
        public async Task ShouldRefuseRecordingAReviewWhenTheActorHoldsNoReviewTierRoleAsync()
        {
            // given
            string entityType = GetRandomString();

            AccessActor readOnlyActor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.ReadOnly });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: readOnlyActor,

                roleSubjects: new List<RoleSubject>
                {
                    CreateRandomRoleSubject(entityType: entityType),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInReviewTier);
        }

        /// <summary>
        /// The review tier across an association's two endpoints — §14.7 posture A′ rule 2, and
        /// the case the endpoint derivation exists to serve. <c>HasReviewTier</c> keeps its own
        /// <c>.Any()</c> over the subject list, separate from the publisher tier's, so proving
        /// either endpoint satisfies the publisher tier proves nothing here.
        ///
        /// <para>Both ends are exercised so neither passes by position, and both spellings of the
        /// scoped role are, because a reviewer for one endpoint and a publisher for one endpoint
        /// each clear the review tier by different routes (the publisher tier widens into it).</para>
        /// </summary>
        [Theory]
        [InlineData("ContentItem", false)]
        [InlineData("BibleReference", false)]
        [InlineData("ContentItem", true)]
        [InlineData("BibleReference", true)]
        public async Task ShouldPermitRecordingAReviewWhenScopedToEitherAssociationEndpointAsync(
            string scopedEndpoint,
            bool asPublisher)
        {
            // given
            string scopedRole = asPublisher
                ? RoleNames.PublishersFor(scopedEndpoint)
                : RoleNames.ReviewersFor(scopedEndpoint);

            AccessActor endpointActor = CreateRandomAccessActor(
                roles: new List<string> { scopedRole });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: endpointActor,

                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                    new RoleSubject { EntityType = "BibleReference", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
        }

        /// <summary>
        /// The other side of the same rule: a role scoped to an entity type that is neither
        /// endpoint clears nothing. This is #190's headline case at the review tier.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseRecordingAReviewWhenScopedToNeitherAssociationEndpointAsync()
        {
            // given
            AccessActor unrelatedActor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.ReviewersFor("Tag") });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: unrelatedActor,

                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "ContentItem", ContentType = "Testimony" },
                    new RoleSubject { EntityType = "BibleReference", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInReviewTier);
        }

        [Theory]
        [InlineData(RoleNames.Reviewers)]
        [InlineData(RoleNames.Publishers)]
        [InlineData(RoleNames.Administrators)]
        public async Task ShouldPermitRecordingAReviewForEachGlobalReviewTierRoleAsync(
            string globalRole)
        {
            // given
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string> { globalRole });

            RecordReviewRequest recordReviewRequest =
                CreateRandomRecordReviewRequest(actor: actor);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);

            actualVerdict.Explanation.Should()
                .Be("Actor may record a review on this approval.");
        }

        [Fact]
        public async Task ShouldPermitRecordingAReviewForAnEntityTypeScopedReviewerRoleAsync()
        {
            // given
            string entityType = GetRandomString();

            AccessActor scopedReviewer = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.ReviewersFor(entityType) });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: scopedReviewer,

                roleSubjects: new List<RoleSubject>
                {
                    CreateRandomRoleSubject(entityType: entityType),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldPermitRecordingAReviewForAContentTypeScopedReviewerRoleAsync()
        {
            // given
            string entityType = GetRandomString();
            string contentType = GetRandomString();

            AccessActor scopedReviewer = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.ReviewersFor(entityType, contentType) });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: scopedReviewer,

                roleSubjects: new List<RoleSubject>
                {
                    CreateRandomRoleSubject(
                        entityType: entityType,
                        contentType: contentType),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldRefuseRecordingAReviewWhenTheScopedRoleIsForADifferentEntityTypeAsync()
        {
            // given
            string entityType = GetRandomString();
            string differentEntityType = GetRandomString();

            AccessActor scopedReviewerElsewhere = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.ReviewersFor(differentEntityType) });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: scopedReviewerElsewhere,

                roleSubjects: new List<RoleSubject>
                {
                    CreateRandomRoleSubject(entityType: entityType),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInReviewTier);
        }

        // HR-1. Unconditional: no role and no setting buys the author a review of their own work.
        [Fact]
        public async Task ShouldRefuseRecordingAReviewWhenTheActorAuthoredTheEntityAsync()
        {
            // given
            string authorId = GetRandomString();
            string entityType = GetRandomString();
            string contentType = GetRandomString();

            AccessActor authorHoldingEveryRole = CreateRandomAccessActor(
                userId: authorId,

                roles: new List<string>
                {
                    RoleNames.Reviewers,
                    RoleNames.Publishers,
                    RoleNames.Administrators,
                    RoleNames.ReviewersFor(entityType),
                    RoleNames.ReviewersFor(entityType, contentType),
                    RoleNames.PublishersFor(entityType),
                    RoleNames.PublishersFor(entityType, contentType),
                });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: authorHoldingEveryRole,
                entityCreatedBy: authorId,

                roleSubjects: new List<RoleSubject>
                {
                    CreateRandomRoleSubject(
                        entityType: entityType,
                        contentType: contentType),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.SelfReviewNeverPermitted);
        }

        [Theory]
        [InlineData(ApprovalState.Draft)]
        [InlineData(ApprovalState.Approved)]
        [InlineData(ApprovalState.Rejected)]
        public async Task ShouldRefuseRecordingAReviewWhenTheApprovalIsNotSubmittedAsync(
            ApprovalState closedApprovalState)
        {
            // given
            RecordReviewRequest recordReviewRequest =
                CreateRandomRecordReviewRequest(approvalState: closedApprovalState);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ApprovalNotOpenForReview);
        }

        [Fact]
        public async Task ShouldPermitRecordingAReviewWhenTheApprovalIsSubmittedAsync()
        {
            // given
            RecordReviewRequest recordReviewRequest =
                CreateRandomRecordReviewRequest(approvalState: ApprovalState.Submitted);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        // §7.7 rule 1 bars a SECOND active review by the same actor.
        [Fact]
        public async Task ShouldRefuseRecordingASecondActiveReviewByTheSameActorAsync()
        {
            // given
            string actorId = GetRandomString();

            AccessActor actor = CreateRandomAccessActor(
                userId: actorId,
                roles: new List<string> { RoleNames.Reviewers });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: actor,

                existingReviews: new List<ReviewRecord>
                {
                    CreateRandomReviewRecord(createdBy: actorId),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ActiveReviewAlreadyRecorded);
        }

        [Theory]
        [InlineData(ReviewVerdict.Dismissed, false)]
        [InlineData(ReviewVerdict.Approved, true)]
        public async Task ShouldPermitRecordingAReviewWhenTheActorsOnlyPriorReviewIsNotActiveAsync(
            ReviewVerdict priorVerdict,
            bool isPriorReviewDeleted)
        {
            // given
            string actorId = GetRandomString();

            AccessActor actor = CreateRandomAccessActor(
                userId: actorId,
                roles: new List<string> { RoleNames.Reviewers });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: actor,

                existingReviews: new List<ReviewRecord>
                {
                    CreateRandomReviewRecord(
                        createdBy: actorId,
                        verdict: priorVerdict,
                        isDeleted: isPriorReviewDeleted),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldPermitRecordingAReviewWhenTheActorIsAmendingTheirOwnReviewAsync()
        {
            // given
            string actorId = GetRandomString();

            AccessActor actor = CreateRandomAccessActor(
                userId: actorId,
                roles: new List<string> { RoleNames.Reviewers });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: actor,
                isAmendingOwnReview: true,

                existingReviews: new List<ReviewRecord>
                {
                    CreateRandomReviewRecord(createdBy: actorId),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldPermitRecordingAReviewWhenTheActiveReviewBelongsToAnotherReviewerAsync()
        {
            // given
            string actorId = GetRandomString();

            AccessActor actor = CreateRandomAccessActor(
                userId: actorId,
                roles: new List<string> { RoleNames.Reviewers });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: actor,

                existingReviews: new List<ReviewRecord>
                {
                    CreateRandomReviewRecord(createdBy: GetRandomString()),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

    }
}
