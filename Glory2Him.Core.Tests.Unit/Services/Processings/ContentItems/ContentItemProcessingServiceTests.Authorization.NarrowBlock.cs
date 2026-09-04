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
    /// <summary>
    /// The narrow block at the processing layer (design §18.6 rule 2). §14.6 rule 1 has every
    /// service gate its own callers, and §8.6.1 has the foundation re-decide beneath this one —
    /// so this is a deliberate duplicate rather than the enforcement point.
    ///
    /// <para><b>Twice on the modify path, and the second one is the load-bearing half.</b> The
    /// pre-load gate can only ask about the caller's copy; <c>ContentType</c> is create-only
    /// (§12.4.1 rule 7a), so the stored row is asked about again once it has been read, and that
    /// is what refuses a blocked contributor who relabelled their edit as a type they are free
    /// on.</para>
    /// </summary>
    public partial class ContentItemProcessingServiceTests
    {
        private const ContentType ProcessingBlockedContentType = ContentType.Quote;
        private const ContentType ProcessingUnblockedContentType = ContentType.Story;

        private static string ProcessingQuoteBlock =>
            Roles.ReadOnlyFor(EntityType.ContentItem, ProcessingBlockedContentType);

        private static ContentItemProcessingValidationException ExpectedProcessingBlockException() =>
            new ContentItemProcessingValidationException(
                message:
                    "Content item processing validation error occurred, fix the errors and try again.",
                innerException: new UnauthorizedContentItemProcessingException(
                    message: "The current user is blocked from contributing content items."));

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfTheNarrowBlockCoversTheContentTypeAsync()
        {
            // given
            ContentItem inputContentItem = CreateRandomContentItem();
            inputContentItem.ContentType = ProcessingBlockedContentType;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext(
                    ProcessingQuoteBlock,
                    Roles.Administrators));

            ContentItemProcessingValidationException expectedException =
                ExpectedProcessingBlockException();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemProcessingService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    addContentItemTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheNarrowBlockCoversTheStoredContentTypeAsync()
        {
            // given: the relabel. The caller presents their quote as a story, so the pre-load
            // gate — which can only see the copy in front of it — lets them through, and the
            // stored row's own type refuses them once it has been read.
            string ownerUserId = GetRandomString();
            Guid contentItemId = Guid.NewGuid();

            ContentItem inputContentItem = CreateRandomContentItem();
            inputContentItem.Id = contentItemId;
            inputContentItem.ContentType = ProcessingUnblockedContentType;

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: contentItemId,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: ownerUserId);

            storageContentItem.ContentType = ProcessingBlockedContentType;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext(ProcessingQuoteBlock));

            ContentItemProcessingValidationException expectedException =
                ExpectedProcessingBlockException();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    contentItemId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(ownerUserId);

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemProcessingService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveByIdIfTheNarrowBlockCoversTheStoredContentTypeAsync()
        {
            // given: the remove path is handed an id, so the stored row is the FIRST place the
            // narrow block can be composed at all — and it covers the holder's own rows.
            string ownerUserId = GetRandomString();
            Guid contentItemId = Guid.NewGuid();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: contentItemId,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: ownerUserId);

            storageContentItem.ContentType = ProcessingBlockedContentType;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: CreateAuthenticatedSecurityContext(ProcessingQuoteBlock));

            ContentItemProcessingValidationException expectedException =
                ExpectedProcessingBlockException();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(contentItemId, null))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    contentItemId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(ownerUserId);

            // when
            ValueTask<ContentItem> removeContentItemByIdTask =
                this.contentItemProcessingService.RemoveContentItemByIdAsync(
                    contentItemId,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    removeContentItemByIdTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.contentItemServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldNotThrowOnAddWhenTheNarrowBlockNamesADifferentContentTypeAsync()
        {
            // given: the silent half. A Quote block says nothing about a Story, so the add is
            // expected to get PAST the gate — proved by the hash the gate would otherwise have
            // prevented, not by a successful add, because everything downstream is another rule.
            ContentItem inputContentItem = CreateRandomContentItem();
            inputContentItem.ContentType = ProcessingUnblockedContentType;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext(ProcessingQuoteBlock));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()))
                    .ReturnsAsync(GetRandomString());

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemProcessingService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            await Record.ExceptionAsync(addContentItemTask.AsTask);

            // then
            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()),
                Times.Once);
        }
    }
}
