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
        public async Task ShouldThrowValidationExceptionOnSubmitIfUserIsNotAuthenticatedAndLogItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: new SecurityContext { IsAuthenticated = false });

            var unauthorizedContentItemOrchestrationException =
                new UnauthorizedContentItemOrchestrationException(
                    message: "The current user is not authenticated.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemOrchestrationException);

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> submitContentItemTask =
                this.contentItemOrchestrationService.SubmitContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    submitContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.eventEnvelopeFactoryMock.Verify(factory =>
                factory.CreateAsync(inputContentItem),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeFactoryMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldThrowValidationExceptionOnSubmitIfUserHasBlockRoleAndLogItAsync(
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

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> submitContentItemTask =
                this.contentItemOrchestrationService.SubmitContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    submitContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.eventEnvelopeFactoryMock.Verify(factory =>
                factory.CreateAsync(inputContentItem),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeFactoryMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmitIfContentItemIsNullAndLogItAsync()
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
            ValueTask<ContentItem> submitContentItemTask =
                this.contentItemOrchestrationService.SubmitContentItemAsync(
                    nullContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    submitContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeFactoryMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnSubmitIfContentItemIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            var invalidContentItem = new ContentItem
            {
                ContentTypeId = Guid.Empty,
                Content = invalidText!
            };

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: invalidContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidContentItemOrchestrationException =
                new InvalidContentItemOrchestrationException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemOrchestrationException.AddData(
                key: nameof(ContentItem.ContentTypeId),
                values: "Id is required");

            invalidContentItemOrchestrationException.AddData(
                key: nameof(ContentItem.Content),
                values: "Text is required");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemOrchestrationException);

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateAsync(invalidContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> submitContentItemTask =
                this.contentItemOrchestrationService.SubmitContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    submitContentItemTask.AsTask);

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
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmitIfDuplicateContentExistsAndLogItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string contentHash = ComputeContentHash(inputContentItem.Content);
            ContentItem duplicateContentItem = CreateRandomContentItem();
            duplicateContentItem.ContentTypeId = inputContentItem.ContentTypeId;
            duplicateContentItem.ContentHash = contentHash;
            duplicateContentItem.IsDeleted = false;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var alreadyExistsContentItemOrchestrationException =
                new AlreadyExistsContentItemOrchestrationException(
                    message: "Content item already exists with the same content.");

            var expectedContentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: alreadyExistsContentItemOrchestrationException);

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(contentHash);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { duplicateContentItem }.AsQueryable());

            // when
            ValueTask<ContentItem> submitContentItemTask =
                this.contentItemOrchestrationService.SubmitContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemOrchestrationValidationException actualContentItemOrchestrationValidationException =
                await Assert.ThrowsAsync<ContentItemOrchestrationValidationException>(
                    submitContentItemTask.AsTask);

            // then
            actualContentItemOrchestrationValidationException.Should().BeEquivalentTo(
                expectedContentItemOrchestrationValidationException);

            this.eventEnvelopeFactoryMock.Verify(factory =>
                factory.CreateAsync(inputContentItem),
                Times.Once);

            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(normalizedContent),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemOrchestrationValidationException))),
                Times.Once);

            this.eventEnvelopeFactoryMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
