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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidAssociationId = Guid.Empty;

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidAssociationException);

            // when
            ValueTask<Association> retrieveAssociationByIdTask =
                this.associationService.RetrieveAssociationByIdAsync(
                    invalidAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    retrieveAssociationByIdTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfAssociationNotFoundAndLogItAsync()
        {
            // given
            Guid someAssociationId = Guid.NewGuid();
            Association nullAssociation = null;

            var notFoundAssociationException =
                new NotFoundAssociationException(
                    message: $"Content item association not found with id: {someAssociationId}.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: notFoundAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullAssociation);

            // when
            ValueTask<Association> retrieveAssociationByIdTask =
                this.associationService.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    retrieveAssociationByIdTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfAssociationIsSoftDeletedAndLogItAsync()
        {
            // given: even an Admin caller gets not-found for a soft-deleted row —
            // deleted beats privilege
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.IsDeleted = true;
            Guid associationId = storageAssociation.Id;

            var notFoundAssociationException = new NotFoundAssociationException(
                message: $"Content item association not found with id: {associationId}.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: notFoundAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            // when
            ValueTask<Association> retrieveAssociationByIdTask =
                this.associationService.RetrieveAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    retrieveAssociationByIdTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Content item association read denied. Content item association " +
                        $"{associationId} is soft-deleted; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.IsDeleted = false;
            storageAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageAssociation.IsPublished = false;
            Guid associationId = storageAssociation.Id;

            var notFoundAssociationException = new NotFoundAssociationException(
                message: $"Content item association not found with id: {associationId}.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: notFoundAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            ValueTask<Association> retrieveAssociationByIdTask =
                this.associationService.RetrieveAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    retrieveAssociationByIdTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content item association read denied. Content item association " +
                        $"{associationId} is not publicly visible and the caller is not " +
                        "authenticated; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.IsDeleted = false;
            storageAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageAssociation.IsPublished = false;
            Guid associationId = storageAssociation.Id;

            var notFoundAssociationException = new NotFoundAssociationException(
                message: $"Content item association not found with id: {associationId}.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: notFoundAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<Association> retrieveAssociationByIdTask =
                this.associationService.RetrieveAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    retrieveAssociationByIdTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    associationId,
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
                        $"{associationId} is not publicly visible and user \"{randomActorUserId}\" " +
                        "is neither the owner nor in a review role; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
