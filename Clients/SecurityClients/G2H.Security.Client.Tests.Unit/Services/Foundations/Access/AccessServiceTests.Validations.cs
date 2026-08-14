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
                Comments = new List<ApprovalCommentRecord>(),
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
                Comments = null!,
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
                key: nameof(ApprovalConditionsRequest.Comments),
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
                    roles: new List<string> { RoleNames.Publisher }),

                Decision = ApprovalDecision.Approve,
                RoleSubjects = new List<RoleSubject>(),
                CandidatePolicies = new List<ApprovalPolicy>(),
                EntityType = invalidEntityType!,
                ContentType = null,
                EntityCreatedBy = GetRandomString(),
                ApprovalState = ApprovalState.Submitted,
                Reviews = new List<ReviewRecord>(),
                Comments = new List<ApprovalCommentRecord>(),
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
                Comments = null!,
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
                key: nameof(DecideApprovalRequest.Comments),
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
    }
}
