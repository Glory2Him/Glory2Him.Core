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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidContentTypeId = Guid.Empty;

            var invalidContentTypeException = new InvalidContentTypeException(
                message: "Content type is invalid, fix the errors and try again.");

            invalidContentTypeException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedContentTypeValidationException = new ContentTypeValidationException(
                message: "Content type validation error occurred, fix the errors and try again.",
                innerException: invalidContentTypeException);

            // when
            ValueTask<Glory2Him.Core.Models.Foundations.ContentTypes.ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    invalidContentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    retrieveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentTypeNotFoundAndLogItAsync()
        {
            // given
            Guid someContentTypeId = Guid.NewGuid();
            ContentType nullContentType = null;

            var notFoundContentTypeException =
                new NotFoundContentTypeException(
                    message: $"Content type not found with id: {someContentTypeId}.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullContentType);

            // when
            ValueTask<ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    retrieveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    someContentTypeId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentTypeIsSoftDeletedAndLogItAsync()
        {
            // given: even an Admin caller gets not-found for a soft-deleted row —
            // deleted beats privilege
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentType storageContentType = CreateRandomContentType();
            storageContentType.IsDeleted = true;
            Guid contentTypeId = storageContentType.Id;

            var notFoundContentTypeException =
                new NotFoundContentTypeException(
                    message: $"Content type not found with id: {contentTypeId}.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    contentTypeId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentType);

            // when
            ValueTask<ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    contentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    retrieveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    contentTypeId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Content type read denied. Content type {contentTypeId} is " +
                        "soft-deleted; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
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
            ContentType storageContentType = CreateRandomContentType();
            storageContentType.IsDeleted = false;
            storageContentType.ApprovalStatus = ApprovalStatus.Draft;
            storageContentType.IsPublished = false;
            Guid contentTypeId = storageContentType.Id;

            var notFoundContentTypeException =
                new NotFoundContentTypeException(
                    message: $"Content type not found with id: {contentTypeId}.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    contentTypeId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentType);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            ValueTask<ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    contentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    retrieveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    contentTypeId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content type read denied. Content type {contentTypeId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfNotVisibleAndUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            string randomActorUserId = GetRandomString();
            ContentType storageContentType = CreateRandomContentType();
            storageContentType.IsDeleted = false;
            storageContentType.ApprovalStatus = ApprovalStatus.Draft;
            storageContentType.IsPublished = false;
            Guid contentTypeId = storageContentType.Id;

            var notFoundContentTypeException =
                new NotFoundContentTypeException(
                    message: $"Content type not found with id: {contentTypeId}.");

            var expectedContentTypeValidationException =
                new ContentTypeValidationException(
                    message: "Content type validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentTypeException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    contentTypeId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentType);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<ContentType> retrieveContentTypeByIdTask =
                this.contentTypeService.RetrieveContentTypeByIdAsync(
                    contentTypeId,
                    TestContext.Current.CancellationToken);

            ContentTypeValidationException actualContentTypeValidationException =
                await Assert.ThrowsAsync<ContentTypeValidationException>(
                    retrieveContentTypeByIdTask.AsTask);

            // then
            actualContentTypeValidationException.Should().BeEquivalentTo(
                expectedContentTypeValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    contentTypeId,
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
                    $"Content type read denied. Content type {contentTypeId} " +
                        $"is not publicly visible and user \"{randomActorUserId}\" is not an " +
                        "Admin; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentTypeValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
