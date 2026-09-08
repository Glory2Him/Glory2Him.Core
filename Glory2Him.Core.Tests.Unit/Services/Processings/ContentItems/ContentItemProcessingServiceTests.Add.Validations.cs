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
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsNotAuthenticatedAndLogItAsync(
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
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemProcessingService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    addContentItemTask.AsTask);

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
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldThrowValidationExceptionOnAddIfUserHasBlockRoleAndLogItAsync(
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
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemProcessingService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    addContentItemTask.AsTask);

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
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfContentItemIsNullAndLogItAsync()
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
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemProcessingService.AddContentItemAsync(
                    nullContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    addContentItemTask.AsTask);

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
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnAddIfContentItemIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            var invalidContentItem = new ContentItem
            {
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
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemProcessingService.AddContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    addContentItemTask.AsTask);

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
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // §9.7.1 rule 1: the add surface may carry an ApprovalStatus of Draft or Submitted and
        // nothing else. The status IS the caller's on this path — that is what lets a
        // contribution land in review rather than as work in progress — so the pair it admits
        // has to be stated where the copy is made, or a caller arrives already decided.
        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalStatusIsDecidedAndLogItAsync(
            ApprovalStatus decidedApprovalStatus)
        {
            // given
            ContentItem inputContentItem = CreateRandomContentItem();
            inputContentItem.ApprovalStatus = decidedApprovalStatus;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemProcessingException.AddData(
                key: nameof(ContentItem.ApprovalStatus),
                values: "Value must be Draft or Submitted on add");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemProcessingService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    addContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputContentItem),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // There is no duplicate-content test here any more: §3.4.2 rule 6 has the add
        // ACKNOWLEDGE a duplicate rather than refuse it, so the case is a logic test rather
        // than a validation one and lives in Add.Logic.cs. The refusal is the modify arm's,
        // and is tested in Modify.Validations.cs.

        // The two rules below are the foundation's as well, and are asked here because the
        // quiet arm never reaches the foundation (§3.4.2 rule 6, #412). Without them a caller
        // sending a bad ShareabilityBasis learns whether the content already exists from
        // whether they get an acknowledgement or a validation error — the probe the rule
        // exists to close, reopened by a rule asked in only one of the two places.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCallerSuppliedFieldsAreInvalidAndLogItAsync()
        {
            // given
            ContentItem invalidContentItem = CreateRandomContentItem();
            invalidContentItem.ShareabilityBasis = (ShareabilityBasis)int.MaxValue;
            invalidContentItem.SharePermission = new string('x', 501);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: invalidContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var invalidContentItemProcessingException =
                new InvalidContentItemProcessingException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemProcessingException.AddData(
                key: nameof(ContentItem.ShareabilityBasis),
                values: "Value is not a supported shareability basis");

            invalidContentItemProcessingException.AddData(
                key: nameof(ContentItem.SharePermission),
                values: "Text exceed max length of 500 characters");

            var expectedContentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemProcessingException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(invalidContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // when
            ValueTask<ContentItem> addContentItemTask =
                this.contentItemProcessingService.AddContentItemAsync(
                    invalidContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemProcessingValidationException actualContentItemProcessingValidationException =
                await Assert.ThrowsAsync<ContentItemProcessingValidationException>(
                    addContentItemTask.AsTask);

            // then
            actualContentItemProcessingValidationException.Should().BeEquivalentTo(
                expectedContentItemProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemProcessingValidationException))),
                Times.Once);

            // ONE envelope and nothing else from this broker. Carried over from the
            // duplicate-refusal test this replaced, because it is the guard that catches a
            // refusal path quietly minting a second envelope or publishing a fact.
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(invalidContentItem),
                Times.Once);

            // the probe never runs: the request is refused before the duplicate question is
            // asked, so an invalid submission cannot be used to ask it either
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
