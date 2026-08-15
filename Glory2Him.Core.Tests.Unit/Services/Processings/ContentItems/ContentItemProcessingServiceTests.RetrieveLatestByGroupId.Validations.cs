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
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveLatestByGroupIdIfGroupIdIsInvalidAndLogItAsync()
        {
            // given
            Guid invalidGroupId = Guid.Empty;
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemProcessingException.AddData(
                key: nameof(ContentItem.GroupId),
                values: "Id is required");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(invalidGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> retrieveLatestContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrieveLatestContentItemByGroupIdAsync(
                    invalidGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveLatestContentItemByGroupIdTask.AsTask);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveLatestByGroupIdIfNoLatestVersionExistsAndLogItAsync()
        {
            // given: a group whose only tip candidate is soft-deleted has no readable
            // latest version — like a missing group, it answers not found before the
            // clock or the caller's identity is ever consulted
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem olderContentItem = CreateRandomStorageContentItem(
                contentItemId: Guid.NewGuid(),
                approvalStatus: ApprovalStatus.Approved,
                createdBy: GetRandomString());

            olderContentItem.GroupId = inputGroupId;
            olderContentItem.IsLatestVersion = false;
            ContentItem deletedLatestContentItem = CreateRandomDeletedContentItem(currentDateTime);
            deletedLatestContentItem.GroupId = inputGroupId;
            deletedLatestContentItem.IsLatestVersion = true;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                olderContentItem,
                deletedLatestContentItem
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(Roles.Admin);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem { GroupId = inputGroupId },
                securityContext: securityContext);

            var notFoundContentItemProcessingException =
                new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            ValueTask<ContentItem> retrieveLatestContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrieveLatestContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveLatestContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            // the outward answer is reason-free, so the true denial reason must land in
            // the server-side log — and only there
            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Content item read denied. Group {inputGroupId} has no " +
                        "non-deleted latest version; reported to the caller as not found."),
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

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveLatestByGroupIdIfNonPublicAndCallerIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given: a non-public edit tip is reported as not found — never as
            // unauthorized — so an anonymous probe cannot tell an in-review group from a
            // missing one, and the caller is never identified
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem latestContentItem = CreateRandomStorageContentItem(
                contentItemId: Guid.NewGuid(),
                approvalStatus: ApprovalStatus.Submitted,
                createdBy: GetRandomString());

            latestContentItem.GroupId = inputGroupId;
            latestContentItem.IsPublished = false;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                latestContentItem
            }.AsQueryable();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem { GroupId = inputGroupId },
                securityContext: unauthenticatedSecurityContext!);

            var notFoundContentItemProcessingException =
                new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ContentItem> retrieveLatestContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrieveLatestContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveLatestContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

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
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
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
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem latestContentItem = CreateRandomStorageContentItem(
                contentItemId: Guid.NewGuid(),
                approvalStatus: ApprovalStatus.Submitted,
                createdBy: GetRandomString());

            latestContentItem.GroupId = inputGroupId;
            latestContentItem.IsPublished = false;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                latestContentItem
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();
            string actorUserId = GetRandomString();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem { GroupId = inputGroupId },
                securityContext: securityContext);

            var notFoundContentItemProcessingException =
                new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
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
                this.contentItemProcessingService.RetrieveLatestContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrieveLatestContentItemByGroupIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

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
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
