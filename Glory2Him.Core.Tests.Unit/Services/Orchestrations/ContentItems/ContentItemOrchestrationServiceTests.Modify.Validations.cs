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
        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: unauthenticatedSecurityContext!);

            var unauthorizedContentItemOrchestrationException =
                new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is not authenticated.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputContentItem),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUserHasBlockRoleAndLogItAsync(
            string blockRole)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext(blockRole));

            var unauthorizedContentItemOrchestrationException =
                new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is blocked from contributing content items.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIsNullAndLogItAsync()
        {
            // given
            ContentItem nullContentItem = null!;

            var nullContentItemOrchestrationException =
                new NullContentItemOrchestrationException(message: "Content item is null.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: nullContentItemOrchestrationException);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemOrchestrationService.ModifyContentItemAsync(
                    nullContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given: on modify the Id is required too — it selects the row being modified
            var invalidContentItem = new ContentItem
            {
                Id = Guid.Empty,
                ContentType = (ContentType)int.MaxValue,
                Content = invalidText!
            };

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: invalidContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidContentItemOrchestrationException =
                new InvalidContentItemOrchestrationException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemOrchestrationException.AddData(
                key: nameof(ContentItem.Id),
                values: "Id is required");

            invalidContentItemOrchestrationException.AddData(
                key: nameof(ContentItem.ContentType),
                values: "Value is not a supported content type");

            invalidContentItemOrchestrationException.AddData(
                key: nameof(ContentItem.Content),
                values: "Text is required");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(invalidContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemOrchestrationService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIsSoftDeletedAndLogItAsync()
        {
            // given: a soft-deleted row is treated as not found — it must not be revived
            // through the modify path
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: GetRandomString());

            storageContentItem.IsDeleted = true;
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            var notFoundContentItemOrchestrationException =
                new NotFoundContentItemOrchestrationException(
                    message: "The content item was not found.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIsNotLatestVersionAndLogItAsync()
        {
            // given: edits go to the tip of the version chain only (§3.4.1) — modifying a
            // superseded row would end up creating a second IsLatestVersion = true on fork
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: ApprovalStatus.Approved,
                createdBy: GetRandomString());

            storageContentItem.IsLatestVersion = false;
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            var invalidContentItemOrchestrationException =
                new InvalidContentItemOrchestrationException(
                    message: "Only the latest version of a content item may be modified.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.hashBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft, null)]
        [InlineData(ApprovalStatus.Submitted, null)]
        [InlineData(ApprovalStatus.Rejected, null)]
        [InlineData(ApprovalStatus.Dismissed, null)]
        [InlineData(ApprovalStatus.Approved, null)]
        [InlineData(ApprovalStatus.Approved, Roles.Reviewer)]
        [InlineData(ApprovalStatus.Approved, Roles.ContentItemReviewer)]
        [InlineData(ApprovalStatus.Approved, Roles.Publisher)]
        [InlineData(ApprovalStatus.Approved, Roles.ContentItemPublisher)]
        [InlineData(ApprovalStatus.Approved, Roles.Admin)]
        public async Task ShouldThrowValidationExceptionOnModifyIfActorIsNotPermittedAndLogItAsync(
            ApprovalStatus approvalStatus,
            string? actorRole)
        {
            // given: a plain authenticated user never touches someone else's item, and an
            // approved item belongs to its owner alone — no role (Reviewer, Publisher or
            // Admin) may modify it on the owner's behalf
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: approvalStatus,
                createdBy: GetRandomString());

            string[] actorRoles = actorRole is null
                ? Array.Empty<string>()
                : new[] { actorRole };

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(actorRoles);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            var unauthorizedContentItemOrchestrationException =
                new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is not allowed to modify this content item.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.hashBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfDuplicateContentExistsInAnotherGroupAndLogItAsync()
        {
            // given: matching normalized content of the same content type in ANOTHER group
            // is a validation error on modify (§3.4.2 rule 6)
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string contentHash = ComputeContentHash(inputContentItem.Content);
            string actorUserId = GetRandomString();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            var alreadyExistsContentItemOrchestrationException =
                new AlreadyExistsContentItemOrchestrationException(
                    message: "A content item already exists with the same content.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: alreadyExistsContentItemOrchestrationException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(contentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
