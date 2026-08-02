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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveLatestByGroupIdIfGroupIdIsInvalidAndLogItAsync()
        {
            // given
            Guid invalidContentItemGroupId = Guid.Empty;
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidContentItemOrchestrationException =
                new InvalidContentItemOrchestrationException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemOrchestrationException.AddData(
                key: nameof(ContentItem.ContentItemGroupId),
                values: "Id is required");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(invalidContentItemGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> retrieveLatestContentItemByGroupIdTask =
                this.contentItemOrchestrationService.RetrieveLatestContentItemByGroupIdAsync(
                    invalidContentItemGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    retrieveLatestContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveLatestByGroupIdIfNoLatestVersionExistsAndLogItAsync()
        {
            // given: a group whose only tip candidate is soft-deleted has no readable
            // latest version — like a missing group, it answers not found before the
            // clock or the caller's identity is ever consulted
            Guid randomContentItemGroupId = Guid.NewGuid();
            Guid inputContentItemGroupId = randomContentItemGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem olderContentItem = CreateRandomStorageContentItem(
                contentItemId: Guid.NewGuid(),
                approvalStatus: ApprovalStatus.Approved,
                createdBy: GetRandomString());

            olderContentItem.ContentItemGroupId = inputContentItemGroupId;
            olderContentItem.IsLatestVersion = false;
            ContentItem deletedLatestContentItem = CreateRandomDeletedContentItem(currentDateTime);
            deletedLatestContentItem.ContentItemGroupId = inputContentItemGroupId;
            deletedLatestContentItem.IsLatestVersion = true;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                olderContentItem,
                deletedLatestContentItem
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(Roles.Admin);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem { ContentItemGroupId = inputContentItemGroupId },
                securityContext: securityContext);

            var notFoundContentItemOrchestrationException =
                new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputContentItemGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            ValueTask<ContentItem> retrieveLatestContentItemByGroupIdTask =
                this.contentItemOrchestrationService.RetrieveLatestContentItemByGroupIdAsync(
                    inputContentItemGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    retrieveLatestContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            // the outward answer is reason-free, so the true denial reason must land in
            // the server-side log — and only there
            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Content item read denied. Group {inputContentItemGroupId} has no " +
                        "non-deleted latest version; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveLatestByGroupIdIfNonPublicAndCallerIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given: a non-public edit tip is reported as not found — never as
            // unauthorized — so an anonymous probe cannot tell an in-review group from a
            // missing one, and the caller is never identified
            Guid randomContentItemGroupId = Guid.NewGuid();
            Guid inputContentItemGroupId = randomContentItemGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem latestContentItem = CreateRandomStorageContentItem(
                contentItemId: Guid.NewGuid(),
                approvalStatus: ApprovalStatus.Submitted,
                createdBy: GetRandomString());

            latestContentItem.ContentItemGroupId = inputContentItemGroupId;
            latestContentItem.IsPublished = false;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                latestContentItem
            }.AsQueryable();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem { ContentItemGroupId = inputContentItemGroupId },
                securityContext: unauthenticatedSecurityContext!);

            var notFoundContentItemOrchestrationException =
                new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputContentItemGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ContentItem> retrieveLatestContentItemByGroupIdTask =
                this.contentItemOrchestrationService.RetrieveLatestContentItemByGroupIdAsync(
                    inputContentItemGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    retrieveLatestContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            // the outward answer is reason-free, so the true denial reason must land in
            // the server-side log — and only there
            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content item read denied. Content item {latestContentItem.Id} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveLatestByGroupIdIfNonPublicAndActorIsNotPermittedAndLogItAsync()
        {
            // given: an authenticated caller who is neither the owner nor in a review
            // role reads a non-public edit tip as not found
            Guid randomContentItemGroupId = Guid.NewGuid();
            Guid inputContentItemGroupId = randomContentItemGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem latestContentItem = CreateRandomStorageContentItem(
                contentItemId: Guid.NewGuid(),
                approvalStatus: ApprovalStatus.Submitted,
                createdBy: GetRandomString());

            latestContentItem.ContentItemGroupId = inputContentItemGroupId;
            latestContentItem.IsPublished = false;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                latestContentItem
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();
            string actorUserId = GetRandomString();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem { ContentItemGroupId = inputContentItemGroupId },
                securityContext: securityContext);

            var notFoundContentItemOrchestrationException =
                new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputContentItemGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<ContentItem> retrieveLatestContentItemByGroupIdTask =
                this.contentItemOrchestrationService.RetrieveLatestContentItemByGroupIdAsync(
                    inputContentItemGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    retrieveLatestContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            // the outward answer is reason-free, so the true denial reason — including
            // who was denied — must land in the server-side log, and only there
            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content item read denied. Content item {latestContentItem.Id} " +
                        $"is not publicly visible and user \"{actorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
