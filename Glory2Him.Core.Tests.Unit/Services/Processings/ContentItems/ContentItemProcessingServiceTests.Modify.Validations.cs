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

            var unauthorizedContentItemProcessingException =
                new UnauthorizedContentItemProcessingException(
                    message: "The current user is not authenticated.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemProcessingService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputContentItem),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUserHasBlockRoleAndLogItAsync(
            string blockRole)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext(blockRole));

            var unauthorizedContentItemProcessingException =
                new UnauthorizedContentItemProcessingException(
                    message: "The current user is blocked from contributing content items.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemProcessingService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    modifyContentItemTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIsNullAndLogItAsync()
        {
            // given
            ContentItem nullContentItem = null!;

            var nullContentItemProcessingException =
                new NullContentItemProcessingException(message: "Content item is null.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: nullContentItemProcessingException);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemProcessingService.ModifyContentItemAsync(
                    nullContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

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

            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemProcessingException.AddData(
                key: nameof(ContentItem.Id),
                values: "Id is required");

            invalidContentItemProcessingException.AddData(
                key: nameof(ContentItem.ContentType),
                values: "Value is not a supported content type");

            invalidContentItemProcessingException.AddData(
                key: nameof(ContentItem.Content),
                values: "Text is required");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(invalidContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemProcessingService.ModifyContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    modifyContentItemTask.AsTask);

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

            var notFoundContentItemProcessingException =
                new NotFoundContentItemProcessingException(
                    message: "The content item was not found.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemProcessingService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyGroupTipResolved();

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIsNotLatestVersionAndLogItAsync()
        {
            // given: edits go to the tip of the version chain only (§3.4.1) — forking off a
            // superseded row would put a second row at the same Version, which is exactly what
            // the (GroupId, Version) unique index refuses. The row is not made superseded by a
            // flag on itself: SetupGroupTip seeds a live sibling at a HIGHER Version, and the
            // derivation reads the answer off the group.
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: ApprovalStatus.Approved,
                createdBy: GetRandomString());

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(
                    message: "Only the latest version of a content item may be modified.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            SetupGroupTip(storageContentItem, isTheGroupTip: false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemProcessingService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            // the refusal came out of the GROUP, not off a field on the loaded row
            VerifyGroupTipResolved();

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
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
        [InlineData(ApprovalStatus.Approved, Roles.Reviewers)]
        [InlineData(ApprovalStatus.Approved, Roles.ContentItemReviewers)]
        [InlineData(ApprovalStatus.Approved, Roles.Publishers)]
        [InlineData(ApprovalStatus.Approved, Roles.ContentItemPublishers)]
        [InlineData(ApprovalStatus.Approved, Roles.Administrators)]
        [InlineData(ApprovalStatus.Rejected, Roles.Reviewers)]
        [InlineData(ApprovalStatus.Rejected, Roles.ContentItemReviewers)]
        [InlineData(ApprovalStatus.Rejected, Roles.Publishers)]
        [InlineData(ApprovalStatus.Rejected, Roles.ContentItemPublishers)]
        [InlineData(ApprovalStatus.Rejected, Roles.Administrators)]
        public async Task ShouldThrowValidationExceptionOnModifyIfActorIsNotPermittedAndLogItAsync(
            ApprovalStatus approvalStatus,
            string? actorRole)
        {
            // given: a plain authenticated user never touches someone else's item, and a
            // terminal item — Approved or Rejected — belongs to its owner alone: no role
            // (Reviewers, Publishers or Administrators) may modify it on the owner's behalf, because
            // the only edit a terminal row admits is a fork, and a moderator forking
            // someone else's decided row would author a version in their name
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

            var unauthorizedContentItemProcessingException =
                new UnauthorizedContentItemProcessingException(
                    message: "The current user is not allowed to modify this content item.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemProcessingService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
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

            var alreadyExistsContentItemProcessingException =
                new AlreadyExistsContentItemProcessingException(
                    message: "A content item already exists with the same content.");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: alreadyExistsContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

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
                this.contentItemProcessingService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

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
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
