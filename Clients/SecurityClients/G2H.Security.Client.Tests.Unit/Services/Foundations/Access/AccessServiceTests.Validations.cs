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
using G2H.Security.Client.Models.Foundations.Access.Exceptions;
using G2H.Security.Client.Models.Securities;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Access
{
    public partial class AccessServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnEvaluateApprovalConditionsIfRequestIsNullAsync()
        {
            // given
            ApprovalConditionsRequest? nullApprovalConditionsRequest = null;

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<ApprovalConditionsVerdict> evaluateApprovalConditionsTask =
                this.accessService.EvaluateApprovalConditionsAsync(
                    nullApprovalConditionsRequest!);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    evaluateApprovalConditionsTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldThrowValidationExceptionOnEvaluateApprovalConditionsIfEntityTypeIsInvalidAsync(
            string? invalidEntityType)
        {
            // given
            var invalidApprovalConditionsRequest = new ApprovalConditionsRequest
            {
                CandidatePolicies = new List<ApprovalPolicy>(),
                EntityType = invalidEntityType!,
                ContentType = null,
                Reviews = new List<ReviewRecord>(),
                ApprovalComments = new List<ApprovalCommentRecord>(),
                ConfidenceScore = null,
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(ApprovalConditionsRequest.EntityType),
                value: "Text is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<ApprovalConditionsVerdict> evaluateApprovalConditionsTask =
                this.accessService.EvaluateApprovalConditionsAsync(
                    invalidApprovalConditionsRequest);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    evaluateApprovalConditionsTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnEvaluateApprovalConditionsIfListsAreNullAsync()
        {
            // given
            var invalidApprovalConditionsRequest = new ApprovalConditionsRequest
            {
                CandidatePolicies = null!,
                EntityType = GetRandomString(),
                ContentType = null,
                Reviews = null!,
                ApprovalComments = null!,
                ConfidenceScore = null,
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(ApprovalConditionsRequest.CandidatePolicies),
                value: "List is required");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(ApprovalConditionsRequest.Reviews),
                value: "List is required");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(ApprovalConditionsRequest.ApprovalComments),
                value: "List is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<ApprovalConditionsVerdict> evaluateApprovalConditionsTask =
                this.accessService.EvaluateApprovalConditionsAsync(
                    invalidApprovalConditionsRequest);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    evaluateApprovalConditionsTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayRecordApprovalReviewIfRequestIsNullAsync()
        {
            // given
            RecordReviewRequest? nullRecordReviewRequest = null;

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayRecordApprovalReviewTask =
                this.accessService.MayRecordApprovalReviewAsync(nullRecordReviewRequest!);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayRecordApprovalReviewTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayRecordApprovalReviewIfActorAndListsAreNullAsync()
        {
            // given
            var invalidRecordReviewRequest = new RecordReviewRequest
            {
                Actor = null!,
                RoleSubjects = null!,
                EntityCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                ExistingReviews = null!,
                IsAmendingOwnReview = false,
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(RecordReviewRequest.Actor),
                value: "Actor is required");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(RecordReviewRequest.RoleSubjects),
                value: "List is required");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(RecordReviewRequest.ExistingReviews),
                value: "List is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayRecordApprovalReviewTask =
                this.accessService.MayRecordApprovalReviewAsync(invalidRecordReviewRequest);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayRecordApprovalReviewTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayRecordApprovalReviewIfActorRolesAreNullAsync()
        {
            // given
            var actorWithoutRoles = new AccessActor
            {
                UserId = GetRandomString(),
                Roles = null!,
                IsAuthenticated = true,
            };

            var invalidRecordReviewRequest = new RecordReviewRequest
            {
                Actor = actorWithoutRoles,
                RoleSubjects = new List<RoleSubject>(),
                EntityCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                ExistingReviews = new List<ReviewRecord>(),
                IsAmendingOwnReview = false,
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(RecordReviewRequest.Actor),
                value: "Actor is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayRecordApprovalReviewTask =
                this.accessService.MayRecordApprovalReviewAsync(invalidRecordReviewRequest);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayRecordApprovalReviewTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayDecideApprovalIfRequestIsNullAsync()
        {
            // given
            DecideApprovalRequest? nullDecideApprovalRequest = null;

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayDecideApprovalTask =
                this.accessService.MayDecideApprovalAsync(nullDecideApprovalRequest!);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayDecideApprovalTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldThrowValidationExceptionOnMayDecideApprovalIfEntityTypeIsInvalidAsync(
            string? invalidEntityType)
        {
            // given
            var invalidDecideApprovalRequest = new DecideApprovalRequest
            {
                Actor = CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Publishers }),

                Decision = ApprovalDecision.Approve,
                RoleSubjects = new List<RoleSubject>(),
                CandidatePolicies = new List<ApprovalPolicy>(),
                EntityType = invalidEntityType!,
                ContentType = null,
                EntityCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                Reviews = new List<ReviewRecord>(),
                ApprovalComments = new List<ApprovalCommentRecord>(),
                ConfidenceScore = null,
                IsBypassRequested = false,
                BypassReason = null,
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(DecideApprovalRequest.EntityType),
                value: "Text is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayDecideApprovalTask =
                this.accessService.MayDecideApprovalAsync(invalidDecideApprovalRequest);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayDecideApprovalTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayDecideApprovalIfActorAndListsAreNullAsync()
        {
            // given
            var invalidDecideApprovalRequest = new DecideApprovalRequest
            {
                Actor = null!,
                Decision = ApprovalDecision.Approve,
                RoleSubjects = null!,
                CandidatePolicies = null!,
                EntityType = GetRandomString(),
                ContentType = null,
                EntityCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                Reviews = null!,
                ApprovalComments = null!,
                ConfidenceScore = null,
                IsBypassRequested = false,
                BypassReason = null,
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(DecideApprovalRequest.Actor),
                value: "Actor is required");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(DecideApprovalRequest.RoleSubjects),
                value: "List is required");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(DecideApprovalRequest.CandidatePolicies),
                value: "List is required");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(DecideApprovalRequest.Reviews),
                value: "List is required");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(DecideApprovalRequest.ApprovalComments),
                value: "List is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayDecideApprovalTask =
                this.accessService.MayDecideApprovalAsync(invalidDecideApprovalRequest);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayDecideApprovalTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }
        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayAmendApprovalIfRequestIsNullAsync()
        {
            // given
            AmendApprovalRequest? nullAmendApprovalRequest = null;

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayAmendApprovalTask =
                this.accessService.MayAmendApprovalAsync(nullAmendApprovalRequest!);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayAmendApprovalTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayAmendApprovalIfActorIsNullAsync()
        {
            // given: an ungathered actor must be a validation error rather than a decision,
            // because a null actor would otherwise reach IsActorUsable and read as unauthenticated
            var requestWithoutActor = new AmendApprovalRequest
            {
                Actor = null!,
                RoleSubjects = new List<RoleSubject>(),
                EntityCreatedBy = GetRandomString(),
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(AmendApprovalRequest.Actor),
                value: "Actor is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayAmendApprovalTask =
                this.accessService.MayAmendApprovalAsync(requestWithoutActor);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayAmendApprovalTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        /// <summary>
        /// A null subject list would crash rather than refuse — HasReviewTier reaches straight
        /// for .Any() on it. An EMPTY list stays legal and refuses on its own merits.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayAmendApprovalIfRoleSubjectsIsNullAsync()
        {
            // given
            var requestWithoutRoleSubjects = new AmendApprovalRequest
            {
                Actor = CreateRandomAccessActor(roles: new List<string> { RoleNames.Reviewers }),
                RoleSubjects = null!,
                EntityCreatedBy = GetRandomString(),
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.UpsertDataList(
                key: nameof(AmendApprovalRequest.RoleSubjects),
                value: "List is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayAmendApprovalTask =
                this.accessService.MayAmendApprovalAsync(requestWithoutRoleSubjects);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayAmendApprovalTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldRefuseAmendingAnApprovalWhenRoleSubjectsIsEmptyAsync()
        {
            // given
            var requestWithNoSubjects = new AmendApprovalRequest
            {
                Actor = CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.ReviewersFor("ContentItem") }),

                RoleSubjects = new List<RoleSubject>(),
                EntityCreatedBy = GetRandomString(),
            };

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(requestWithNoSubjects);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInReviewTier);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayRecordApprovalCommentIfRequestIsNullAsync()
        {
            // given
            RecordApprovalCommentRequest? nullRecordApprovalCommentRequest = null;

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayRecordApprovalCommentTask =
                this.accessService.MayRecordApprovalCommentAsync(nullRecordApprovalCommentRequest!);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayRecordApprovalCommentTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayRecordApprovalCommentIfActorIsNullAsync()
        {
            // given: an ungathered actor must be a validation error rather than a decision,
            // because a null actor would otherwise reach IsActorUsable and read as unauthenticated
            var requestWithoutActor = new RecordApprovalCommentRequest
            {
                Actor = null!,
                ApprovalState = ApprovalState.Submitted,
                IsParentApprovalDeleted = false,
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.AddData(
                key: nameof(RecordApprovalCommentRequest.Actor),
                values: "Actor is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayRecordApprovalCommentTask =
                this.accessService.MayRecordApprovalCommentAsync(requestWithoutActor);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayRecordApprovalCommentTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayAmendApprovalCommentIfRequestIsNullAsync()
        {
            // given
            AmendApprovalCommentRequest? nullAmendApprovalCommentRequest = null;

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayAmendApprovalCommentTask =
                this.accessService.MayAmendApprovalCommentAsync(nullAmendApprovalCommentRequest!);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayAmendApprovalCommentTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayAmendApprovalCommentIfActorIsNullAsync()
        {
            // given: an ungathered actor must be a validation error rather than a decision,
            // because a null actor would otherwise reach IsActorUsable and read as unauthenticated
            var requestWithoutActor = new AmendApprovalCommentRequest
            {
                Actor = null!,
                CommentCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                IsParentApprovalDeleted = false,
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.AddData(
                key: nameof(AmendApprovalCommentRequest.Actor),
                values: "Actor is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayAmendApprovalCommentTask =
                this.accessService.MayAmendApprovalCommentAsync(requestWithoutActor);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayAmendApprovalCommentTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayResolveApprovalCommentIfRequestIsNullAsync()
        {
            // given
            ResolveApprovalCommentRequest? nullResolveApprovalCommentRequest = null;

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayResolveApprovalCommentTask =
                this.accessService.MayResolveApprovalCommentAsync(nullResolveApprovalCommentRequest!);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayResolveApprovalCommentTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnMayResolveApprovalCommentIfActorIsNullAsync()
        {
            // given: an ungathered actor must be a validation error rather than a decision,
            // because a null actor would otherwise reach IsActorUsable and read as unauthenticated
            var requestWithoutActor = new ResolveApprovalCommentRequest
            {
                Actor = null!,
                CommentCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                IsParentApprovalDeleted = false,
            };

            var invalidArgumentAccessException = new InvalidArgumentAccessException(
                message: "Invalid access argument. Please correct the error and try again.");

            invalidArgumentAccessException.AddData(
                key: nameof(ResolveApprovalCommentRequest.Actor),
                values: "Actor is required");

            var expectedAccessValidationException = new AccessValidationException(
                message: "Access validation errors occurred, please try again.",
                innerException: invalidArgumentAccessException);

            // when
            ValueTask<AccessVerdict> mayResolveApprovalCommentTask =
                this.accessService.MayResolveApprovalCommentAsync(requestWithoutActor);

            AccessValidationException actualAccessValidationException =
                await Assert.ThrowsAsync<AccessValidationException>(
                    mayResolveApprovalCommentTask.AsTask);

            // then
            actualAccessValidationException.Should()
                .BeEquivalentTo(expectedAccessValidationException);
        }
    }
}
