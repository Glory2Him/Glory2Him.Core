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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidLinkId = Guid.Empty;

            var invalidLinkException = new InvalidLinkException(
                message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.UpsertDataList(
                key: nameof(Link.Id),
                value: "Id is required");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: invalidLinkException);

            // when
            ValueTask<Link> removeLinkByIdTask =
                this.linkService.RemoveLinkByIdAsync(
                    invalidLinkId,
                    cancellationToken: TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    removeLinkByIdTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfDeletionReasonExceedsMaxLengthAndLogItAsync()
        {
            // given
            Guid someLinkId = Guid.NewGuid();
            string invalidDeletionReason = GetRandomStringWithLengthOf(501);

            var invalidLinkException = new InvalidLinkException(
                message: "Link is invalid, fix the errors and try again.");

            invalidLinkException.UpsertDataList(
                key: nameof(Link.DeletionReason),
                value: $"Text exceed max length of {invalidDeletionReason.Length - 1} characters");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: invalidLinkException);

            // when
            ValueTask<Link> removeLinkByIdTask =
                this.linkService.RemoveLinkByIdAsync(
                    someLinkId,
                    deletionReason: invalidDeletionReason,
                    cancellationToken: TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    removeLinkByIdTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfLinkNotFoundAndLogItAsync()
        {
            // given
            Guid someLinkId = Guid.NewGuid();
            Link noLink = null;

            var notFoundLinkException = new NotFoundLinkException(
                message: $"Link not found with id: {someLinkId}.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: notFoundLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noLink);

            // when
            ValueTask<Link> removeLinkByIdTask =
                this.linkService.RemoveLinkByIdAsync(
                    someLinkId,
                    cancellationToken: TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    removeLinkByIdTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Guid someLinkId = Guid.NewGuid();

            var unauthorizedLinkException = new UnauthorizedLinkException(
                message: "The current user is not authenticated.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> removeLinkByIdTask =
                this.linkService.RemoveLinkByIdAsync(
                    someLinkId,
                    cancellationToken: TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    removeLinkByIdTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.LinkReadOnly)]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsBlockedFromContributingAndLogItAsync(
            string blockedRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRole);
            Guid someLinkId = Guid.NewGuid();

            var unauthorizedLinkException = new UnauthorizedLinkException(
                message: "The current user is blocked from contributing links.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: unauthorizedLinkException);

            // when
            ValueTask<Link> removeLinkByIdTask =
                this.linkService.RemoveLinkByIdAsync(
                    someLinkId,
                    cancellationToken: TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    removeLinkByIdTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotOwnerAndNotAdminAndLogItAsync()
        {
            // given
            string randomActorUserId = GetRandomString();
            Link storageLink = CreateRandomLink();
            Guid someLinkId = storageLink.Id;

            var unauthorizedLinkException = new UnauthorizedLinkException(
                message: "The current user is not allowed to remove this link.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: unauthorizedLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<Link> removeLinkByIdTask =
                this.linkService.RemoveLinkByIdAsync(
                    someLinkId,
                    cancellationToken: TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    removeLinkByIdTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateLinkAsync(
                    It.IsAny<Link>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAllowedAndLinkIsAlreadyDeletedAndLogItAsync()
        {
            // given: permission is checked before the idempotent short-circuit, so an
            // unauthorized caller learns nothing about the row's deletion state
            string randomActorUserId = GetRandomString();
            Link alreadyDeletedLink = CreateRandomLink();
            alreadyDeletedLink.IsDeleted = true;
            Guid someLinkId = alreadyDeletedLink.Id;

            var unauthorizedLinkException = new UnauthorizedLinkException(
                message: "The current user is not allowed to remove this link.");

            var expectedLinkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: unauthorizedLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<Link> removeLinkByIdTask =
                this.linkService.RemoveLinkByIdAsync(
                    someLinkId,
                    cancellationToken: TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    removeLinkByIdTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
