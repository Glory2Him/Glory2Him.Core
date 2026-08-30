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
        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: unauthenticatedSecurityContext!);

            var unauthorizedContentItemProcessingException =
                new UnauthorizedContentItemProcessingException(
                    message: "The current user is not authenticated.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputContentItemId, null))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> removeContentItemByIdTask =
                this.contentItemProcessingService.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    removeContentItemByIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputContentItemId, null))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfUserHasBlockRoleAndLogItAsync(
            string blockRole)
        {
            // given
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            ContentItem randomContentItem = CreateRandomContentItem();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: randomContentItem,
                securityContext: CreateAuthenticatedSecurityContext(blockRole));

            var unauthorizedContentItemProcessingException =
                new UnauthorizedContentItemProcessingException(
                    message: "The current user is blocked from contributing content items.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputContentItemId, null))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> removeContentItemByIdTask =
                this.contentItemProcessingService.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    removeContentItemByIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

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
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfContentItemIdIsInvalidAndLogItAsync()
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
                broker.CreateAsync(It.Is(SameRemoveRequestAs(invalidContentItemId, null))))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> removeContentItemByIdTask =
                this.contentItemProcessingService.RemoveContentItemByIdAsync(
                    invalidContentItemId,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    removeContentItemByIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

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
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfContentItemIsAlreadyRemovedAndLogItAsync()
        {
            // given: removing an already soft-deleted row must not report a second
            // removal — from the caller's point of view the item is simply gone
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            string actorUserId = GetRandomString();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItemId,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            storageContentItem.IsDeleted = true;
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

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
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputContentItemId, null))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ValueTask<ContentItem> removeContentItemByIdTask =
                this.contentItemProcessingService.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    removeContentItemByIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

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

        [Theory]
        [InlineData(null)]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.ContentItemReviewers)]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.ContentItemPublishers)]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfActorIsNotPermittedAndLogItAsync(
            string? actorRole)
        {
            // given: removing someone else's content is a takedown, reserved for the
            // owner and an administrator — a reviewer or Publishers moderates through the approval
            // workflow instead and never removes the row
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItemId,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: GetRandomString());

            string[] actorRoles = actorRole is null
                ? Array.Empty<string>()
                : new[] { actorRole };

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(actorRoles);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: securityContext);

            var unauthorizedContentItemProcessingException =
                new UnauthorizedContentItemProcessingException(
                    message: "The current user is not allowed to remove this content item.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputContentItemId, null))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<ContentItem> removeContentItemByIdTask =
                this.contentItemProcessingService.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    removeContentItemByIdTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.hashBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
