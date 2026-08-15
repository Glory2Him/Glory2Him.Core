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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentItemIdIsInvalidAndLogItAsync()
        {
            // given: the id is the whole instruction on this path — nothing selects a row
            // without it
            Guid invalidContentItemId = Guid.Empty;
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemProcessingException.AddData(
                key: nameof(ContentItem.Id),
                values: "Id is required");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(invalidContentItemId))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> retrieveContentItemByIdTask =
                this.contentItemProcessingService.RetrieveContentItemByIdAsync(
                    invalidContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveContentItemByIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfNonPublicAndCallerIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given: a non-public version is reported as not found — never as
            // unauthorized — so an anonymous probe cannot tell it from a missing row,
            // and the caller is never identified
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItemId,
                approvalStatus: ApprovalStatus.Submitted,
                createdBy: GetRandomString());

            storageContentItem.IsPublished = false;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: unauthenticatedSecurityContext!);

            var notFoundContentItemProcessingException =
                new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputContentItemId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ContentItem> retrieveContentItemByIdTask =
                this.contentItemProcessingService.RetrieveContentItemByIdAsync(
                    inputContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveContentItemByIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            // the outward answer is reason-free, so the true denial reason must land in
            // the server-side log — and only there
            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content item read denied. Content item {inputContentItemId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft, false, false)]
        [InlineData(ApprovalStatus.Submitted, false, false)]
        [InlineData(ApprovalStatus.Rejected, false, false)]
        [InlineData(ApprovalStatus.Approved, false, false)]
        [InlineData(ApprovalStatus.Approved, true, true)]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfNonPublicAndActorIsNotPermittedAndLogItAsync(
            ApprovalStatus approvalStatus,
            bool isPublished,
            bool hasFuturePublishDate)
        {
            // given: every way a version can miss canonical visibility (§14.1) — not
            // approved, not published, or scheduled in the future — reads as not found
            // for an authenticated caller who is neither the owner nor in a review role
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItemId,
                approvalStatus: approvalStatus,
                createdBy: GetRandomString());

            storageContentItem.IsPublished = isPublished;

            if (hasFuturePublishDate)
            {
                storageContentItem.PublishDate = currentDateTime.AddDays(1);
            }

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();
            string actorUserId = GetRandomString();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: securityContext);

            var notFoundContentItemProcessingException =
                new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputContentItemId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<ContentItem> retrieveContentItemByIdTask =
                this.contentItemProcessingService.RetrieveContentItemByIdAsync(
                    inputContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveContentItemByIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            // the outward answer is reason-free, so the true denial reason — including
            // who was denied — must land in the server-side log, and only there
            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content item read denied. Content item {inputContentItemId} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentItemIsRemovedAndLogItAsync()
        {
            // given: a soft-deleted row is gone for every caller — even an Admin reads
            // it as not found, before the clock or the caller's identity is ever
            // consulted
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItemId,
                approvalStatus: ApprovalStatus.Approved,
                createdBy: GetRandomString());

            storageContentItem.IsDeleted = true;
            SecurityContext securityContext = CreateAuthenticatedSecurityContext(Roles.Admin);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: securityContext);

            var notFoundContentItemProcessingException =
                new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputContentItemId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            // when
            ValueTask<ContentItem> retrieveContentItemByIdTask =
                this.contentItemProcessingService.RetrieveContentItemByIdAsync(
                    inputContentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveContentItemByIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            // the outward answer is reason-free, so the true denial reason must land in
            // the server-side log — and only there
            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Content item read denied. Content item {inputContentItemId} is " +
                        "soft-deleted; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
