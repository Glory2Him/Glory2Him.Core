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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmitIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.UpsertDataList(
                key: nameof(BibleReference.Id),
                value: "Id is required");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            // when
            ValueTask<BibleReference> submitTask =
                this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            // an invalid id never reaches storage
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectBibleReferenceByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNotAuthenticatedAsync(
            SecurityContext unauthenticatedContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedContext;

            // when
            ValueTask<BibleReference> submitTask =
                this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<BibleReferenceValidationException>(submitTask.AsTask);

            // then: the contribution gate refuses before any row is read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectBibleReferenceByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.BibleReferenceReadOnly)]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsBlockedFromContributingAsync(
            string blockingRole)
        {
            // given: a read-only caller is blocked from every write, submit included, before the
            // row is even read
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockingRole);

            var unauthorizedBibleReferenceException =
                new UnauthorizedBibleReferenceException(
                    message: "The current user is blocked from contributing bible references.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedBibleReferenceException);

            // when
            ValueTask<BibleReference> submitTask =
                this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectBibleReferenceByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid bibleReferenceId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    bibleReferenceId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((BibleReference)null);

            var notFoundBibleReferenceException =
                new NotFoundBibleReferenceException(
                    message: $"Bible reference not found with id: {bibleReferenceId}.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: notFoundBibleReferenceException);

            // when
            ValueTask<BibleReference> submitTask =
                this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                    bibleReferenceId,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateBibleReferenceAsync(
                        It.IsAny<BibleReference>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsSoftDeletedAsync()
        {
            // given: a soft-removed row is reported as not-found, matching the read posture
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            BibleReference storageBibleReference = CreateSubmittableStorageBibleReference();
            storageBibleReference.IsDeleted = true;

            SetupBibleReferenceStorageRead(storageBibleReference);

            var notFoundBibleReferenceException =
                new NotFoundBibleReferenceException(
                    message: $"Bible reference not found with id: {storageBibleReference.Id}.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: notFoundBibleReferenceException);

            // when
            ValueTask<BibleReference> submitTask =
                this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                    storageBibleReference.Id,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateBibleReferenceAsync(
                        It.IsAny<BibleReference>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNeitherOwnerNorPublisherAsync(
            string[] roles)
        {
            // given: a caller who neither owns the row nor holds the publisher tier may not
            // submit it. A Reviewer is included among the role sets: they hold write permission
            // on content, but moving a submission status is never theirs (§8.6 HR-3).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            BibleReference storageBibleReference = CreateSubmittableStorageBibleReference();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"not-the-owner-{Guid.NewGuid()}");

            SetupBibleReferenceStorageRead(storageBibleReference);

            var unauthorizedBibleReferenceException =
                new UnauthorizedBibleReferenceException(
                    message: "The current user is not allowed to submit this bible reference.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedBibleReferenceException);

            // when
            ValueTask<BibleReference> submitTask =
                this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                    storageBibleReference.Id,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateBibleReferenceAsync(
                        It.IsAny<BibleReference>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnSubmitIfTheStoredRowIsNotDraftAsync(
            ApprovalStatus storageStatus)
        {
            // given: only a Draft may be submitted (issue #111 case 7). A row already Submitted
            // or Approved is not a fresh submission — re-submitting one would either re-open a
            // decided item or re-announce a pending one. The caller is the owner, so this proves
            // the state gate stands on its own, after authorization passes.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            BibleReference storageBibleReference = CreateSubmittableStorageBibleReference();
            storageBibleReference.ApprovalStatus = storageStatus;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageBibleReference.CreatedBy);

            SetupBibleReferenceStorageRead(storageBibleReference);

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference cannot be submitted from status " +
                        $"{storageStatus}.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            // when
            ValueTask<BibleReference> submitTask =
                this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                    storageBibleReference.Id,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(submitTask.AsTask);

            // then: nothing written, nothing announced
            actualException.Should().BeEquivalentTo(expectedBibleReferenceValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateBibleReferenceAsync(
                        It.IsAny<BibleReference>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        It.IsAny<BibleReferenceEventOperation>()),
                Times.Never);
        }
    }
}
