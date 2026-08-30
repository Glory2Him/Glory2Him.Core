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
        public async Task ShouldThrowValidationExceptionOnRetrievePublishedByGroupIdIfGroupIdIsInvalidAndLogItAsync()
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
            ValueTask<ContentItem> retrievePublishedContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrievePublishedContentItemByGroupIdAsync(
                    invalidGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrievePublishedContentItemByGroupIdTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnRetrievePublishedByGroupIdIfNoPublishedVersionExistsAndLogItAsync()
        {
            // given: a group whose only published row is soft-deleted has no readable
            // published version — like a missing group, it answers not found before the
            // clock or the caller's identity is ever consulted
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem unpublishedContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            unpublishedContentItem.GroupId = inputGroupId;
            ContentItem deletedPublishedContentItem = CreateRandomDeletedContentItem(currentDateTime);
            deletedPublishedContentItem.GroupId = inputGroupId;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                unpublishedContentItem,
                deletedPublishedContentItem
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);

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
            ValueTask<ContentItem> retrievePublishedContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrievePublishedContentItemByGroupIdTask.AsTask);

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
                        "non-deleted published version; reported to the caller as not found."),
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
        public async Task ShouldThrowValidationExceptionOnRetrievePublishedByGroupIdIfFutureScheduledAndCallerIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given: a future-scheduled published row is reported as not found — never as
            // unauthorized — so an anonymous probe cannot tell a scheduled group from a
            // missing one, and the caller is never identified
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publishedContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publishedContentItem.GroupId = inputGroupId;
            publishedContentItem.PublishDate = currentDateTime.AddDays(1);

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publishedContentItem
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
            ValueTask<ContentItem> retrievePublishedContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrievePublishedContentItemByGroupIdTask.AsTask);

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
                    $"Content item read denied. Content item {publishedContentItem.Id} is not " +
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
        public async Task ShouldThrowValidationExceptionOnRetrievePublishedByGroupIdIfFutureScheduledAndActorIsNotPermittedAndLogItAsync()
        {
            // given: an authenticated caller who is neither the owner nor in a review
            // role reads a future-scheduled published row as not found until the
            // schedule passes
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publishedContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publishedContentItem.GroupId = inputGroupId;
            publishedContentItem.PublishDate = currentDateTime.AddDays(1);

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publishedContentItem
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
            ValueTask<ContentItem> retrievePublishedContentItemByGroupIdTask =
                this.contentItemProcessingService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    retrievePublishedContentItemByGroupIdTask.AsTask);

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
                    $"Content item read denied. Content item {publishedContentItem.Id} " +
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
