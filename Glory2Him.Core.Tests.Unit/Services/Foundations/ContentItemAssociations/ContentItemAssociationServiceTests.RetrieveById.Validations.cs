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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidContentItemAssociationId = Guid.Empty;

            var invalidContentItemAssociationException = new InvalidContentItemAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedContentItemAssociationValidationException = new ContentItemAssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemAssociationException);

            // when
            ValueTask<ContentItemAssociation> retrieveContentItemAssociationByIdTask =
                this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    invalidContentItemAssociationId,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    retrieveContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentItemAssociationNotFoundAndLogItAsync()
        {
            // given
            Guid someContentItemAssociationId = Guid.NewGuid();
            ContentItemAssociation nullContentItemAssociation = null;

            var notFoundContentItemAssociationException =
                new NotFoundContentItemAssociationException(
                    message: $"Content item association not found with id: {someContentItemAssociationId}.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullContentItemAssociation);

            // when
            ValueTask<ContentItemAssociation> retrieveContentItemAssociationByIdTask =
                this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    retrieveContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentItemAssociationIsSoftDeletedAndLogItAsync()
        {
            // given: even an Admin caller gets not-found for a soft-deleted row —
            // deleted beats privilege
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentItemAssociation storageContentItemAssociation = CreateRandomContentItemAssociation();
            storageContentItemAssociation.IsDeleted = true;
            Guid contentItemAssociationId = storageContentItemAssociation.Id;

            var notFoundContentItemAssociationException = new NotFoundContentItemAssociationException(
                message: $"Content item association not found with id: {contentItemAssociationId}.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            // when
            ValueTask<ContentItemAssociation> retrieveContentItemAssociationByIdTask =
                this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    contentItemAssociationId,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    retrieveContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Content item association read denied. Content item association " +
                        $"{contentItemAssociationId} is soft-deleted; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfNotVisibleAndUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            ContentItemAssociation storageContentItemAssociation = CreateRandomContentItemAssociation();
            storageContentItemAssociation.IsDeleted = false;
            storageContentItemAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItemAssociation.IsPublished = false;
            Guid contentItemAssociationId = storageContentItemAssociation.Id;

            var notFoundContentItemAssociationException = new NotFoundContentItemAssociationException(
                message: $"Content item association not found with id: {contentItemAssociationId}.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            ValueTask<ContentItemAssociation> retrieveContentItemAssociationByIdTask =
                this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    contentItemAssociationId,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    retrieveContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content item association read denied. Content item association " +
                        $"{contentItemAssociationId} is not publicly visible and the caller is not " +
                        "authenticated; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfNotVisibleAndUserIsNotOwnerAndLogItAsync()
        {
            // given
            string randomActorUserId = GetRandomString();
            ContentItemAssociation storageContentItemAssociation = CreateRandomContentItemAssociation();
            storageContentItemAssociation.IsDeleted = false;
            storageContentItemAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItemAssociation.IsPublished = false;
            Guid contentItemAssociationId = storageContentItemAssociation.Id;

            var notFoundContentItemAssociationException = new NotFoundContentItemAssociationException(
                message: $"Content item association not found with id: {contentItemAssociationId}.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<ContentItemAssociation> retrieveContentItemAssociationByIdTask =
                this.contentItemAssociationService.RetrieveContentItemAssociationByIdAsync(
                    contentItemAssociationId,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    retrieveContentItemAssociationByIdTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    contentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content item association read denied. Content item association " +
                        $"{contentItemAssociationId} is not publicly visible and user \"{randomActorUserId}\" " +
                        "is neither the owner nor in a review role; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
