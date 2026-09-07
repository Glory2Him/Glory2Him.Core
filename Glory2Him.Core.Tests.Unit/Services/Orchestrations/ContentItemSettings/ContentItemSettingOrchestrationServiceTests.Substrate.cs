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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItemSettings
{
    // THE EVENT PATH OF THE DERIVATION (#456). #455 fixed the HTTP path and left this one bound to
    // the foundation, so an add request published to ContentItemSetting-Adding reached the write
    // gate with the caller's own ContentType still deciding which publisher tier could write the
    // row. These pin that the address now arrives here first, that the item is read before the
    // foundation is, and that a request contradicting the item never reaches it at all.
    public partial class ContentItemSettingOrchestrationServiceTests
    {
        // THE INVARIANT, ON THE PATH THAT DID NOT HAVE IT. The item is read, its type is what the
        // row claims, and only then does the foundation see the envelope — with the envelope
        // itself untouched, because its signature covers the content and the delivery's
        // deduplication keys on its metadata.
        [Fact]
        public async Task ShouldDeriveContentTypeBeforeDelegatingOnAddingContentItemSettingAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomOverrideRequest();
            randomContentItemSetting.ContentType = ActualContentType;
            Guid contentItemId = randomContentItemSetting.ContentItemId.Value;

            EventEnvelope<ContentItemSetting> inputEnvelope =
                CreateRequestEnvelope(randomContentItemSetting);

            ContentItem storageContentItem =
                CreateContentItemOfType(contentItemId, ActualContentType);

            EventEnvelope<ContentItemSetting> expectedReplyEnvelope =
                CreateRequestEnvelope(randomContentItemSetting);

            // SNAPSHOTTED WHILE THE CALL IS HAPPENING, for the same reason the method-path test
            // does it: Moq evaluates It.Is<> at Verify time, so a resolve moved to AFTER the
            // delegation — which restores the whole vulnerability — would still satisfy a matcher.
            bool wasContentItemResolvedFirst = false;

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    contentItemId,
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.contentItemSettingServiceMock.Setup(service =>
                service.OnAddingContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<EventEnvelope<ContentItemSetting>, CancellationToken>(
                            (_, _) => wasContentItemResolvedFirst =
                                this.contentItemServiceMock.Invocations.Count > 0)
                        .ReturnsAsync(expectedReplyEnvelope);

            // when
            EventEnvelope<ContentItemSetting>? actualReplyEnvelope =
                await this.contentItemSettingOrchestrationService
                    .OnAddingContentItemSettingAsync(
                        inputEnvelope,
                        TestContext.Current.CancellationToken);

            // then
            wasContentItemResolvedFirst.Should().BeTrue();
            actualReplyEnvelope.Should().BeSameAs(expectedReplyEnvelope);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "ContentItemSettingAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            // THE ENVELOPE IS CARRIED INTO THE READ, which is the point rather than a detail. The
            // ambient-context overload would evaluate this read as whoever the delivery happens to
            // run under — nobody on a background delivery, the PUBLISHER on a synchronous one —
            // instead of as the subject the envelope was signed for (#469 review).
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    contentItemId,
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // THE SAME ENVELOPE, not a rebuilt one. Everything the foundation owns — the audit
            // stamping, the write, the past-tense fact, the ProcessedEvents rows keyed on
            // Metadata.EventId — depends on the identity and causation this envelope carries.
            this.contentItemSettingServiceMock.Verify(service =>
                service.OnAddingContentItemSettingAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // THE ATTACK #456 DESCRIBES, refused. A Devotional publisher aims an add labelled
        // Devotional at a Story's id; the derivation says Story, and the request never reaches the
        // gate that would have composed ContentItem-Devotional-Publishers out of the claim.
        //
        // Refused rather than silently corrected because the claim arrived inside a SIGNED
        // envelope: rewriting content the publisher signed is the one thing §14.6 rule 4's
        // signature exists to make impossible, and the derived value still governs either way.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingIfClaimedContentTypeIsNotTheItemsAndLogItAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomOverrideRequest();
            Guid contentItemId = randomContentItemSetting.ContentItemId.Value;

            EventEnvelope<ContentItemSetting> inputEnvelope =
                CreateRequestEnvelope(randomContentItemSetting);

            ContentItem storageContentItem =
                CreateContentItemOfType(contentItemId, ActualContentType);

            var contentTypeMismatchContentItemSettingOrchestrationException =
                new ContentTypeMismatchContentItemSettingOrchestrationException(
                    message: "Content item setting is invalid. An override's content type is " +
                        "derived from the content item it names and cannot be stated by the " +
                        "caller; fix the errors and try again.");

            var expectedContentItemSettingOrchestrationValidationException =
                new ContentItemSettingOrchestrationValidationException(
                    message: "Content item setting orchestration validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: contentTypeMismatchContentItemSettingOrchestrationException);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    contentItemId,
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onAddingTask =
                this.contentItemSettingOrchestrationService.OnAddingContentItemSettingAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedContentItemSettingOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingOrchestrationValidationException))),
                Times.Once);

            // THE ENVELOPE IS CARRIED INTO THE READ, which is the point rather than a detail. The
            // ambient-context overload would evaluate this read as whoever the delivery happens to
            // run under — nobody on a background delivery, the PUBLISHER on a synchronous one —
            // instead of as the subject the envelope was signed for (#469 review).
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    contentItemId,
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // the write gate is never reached, which is the whole of the fix
            this.contentItemSettingServiceMock.Verify(service =>
                service.OnAddingContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "ContentItemSettingAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A DEFAULT NAMES NO ITEM, so there is nothing to derive from and the caller's content
        // type is the whole of what the row declares — only an administrator may write one, and
        // the foundation's gate says so. Asserted as "the item service is never touched", because
        // a resolve attempt on a null id is the bug worth catching.
        [Fact]
        public async Task ShouldNotResolveAnyContentItemOnAddingAContentTypeDefaultAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomContentItemSetting();
            randomContentItemSetting.ContentItemId = null;

            EventEnvelope<ContentItemSetting> inputEnvelope =
                CreateRequestEnvelope(randomContentItemSetting);

            EventEnvelope<ContentItemSetting> expectedReplyEnvelope =
                CreateRequestEnvelope(randomContentItemSetting);

            this.contentItemSettingServiceMock.Setup(service =>
                service.OnAddingContentItemSettingAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedReplyEnvelope);

            // when
            EventEnvelope<ContentItemSetting>? actualReplyEnvelope =
                await this.contentItemSettingOrchestrationService
                    .OnAddingContentItemSettingAsync(
                        inputEnvelope,
                        TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeSameAs(expectedReplyEnvelope);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemSettingServiceMock.Verify(service =>
                service.OnAddingContentItemSettingAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "ContentItemSettingAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A DEDUPLICATED DELIVERY REPLIES null, and that has to survive the extra layer. The
        // foundation answers null when ProcessedEvents already holds this event id for this
        // receiver; a handler that turned that into an envelope would make a replay look like a
        // fresh write to whatever read the delivery.
        [Fact]
        public async Task ShouldReturnNullOnAddingWhenTheFoundationSkipsADuplicatedRequestAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomOverrideRequest();
            randomContentItemSetting.ContentType = ActualContentType;
            Guid contentItemId = randomContentItemSetting.ContentItemId.Value;

            EventEnvelope<ContentItemSetting> inputEnvelope =
                CreateRequestEnvelope(randomContentItemSetting);

            ContentItem storageContentItem =
                CreateContentItemOfType(contentItemId, ActualContentType);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    contentItemId,
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.contentItemSettingServiceMock.Setup(service =>
                service.OnAddingContentItemSettingAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((EventEnvelope<ContentItemSetting>)null);

            // when
            EventEnvelope<ContentItemSetting>? actualReplyEnvelope =
                await this.contentItemSettingOrchestrationService
                    .OnAddingContentItemSettingAsync(
                        inputEnvelope,
                        TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeNull();

            // THE ENVELOPE IS CARRIED INTO THE READ, which is the point rather than a detail. The
            // ambient-context overload would evaluate this read as whoever the delivery happens to
            // run under — nobody on a background delivery, the PUBLISHER on a synchronous one —
            // instead of as the subject the envelope was signed for (#469 review).
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    contentItemId,
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemSettingServiceMock.Verify(service =>
                service.OnAddingContentItemSettingAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "ContentItemSettingAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // VERIFICATION COMES FIRST, ahead of the item read. This handler resolves a second entity
        // before the foundation is reached, so acting on an envelope nothing has vouched for would
        // mean reading a row on the word of an unattested payload — which is why the guard is
        // repeated here rather than left to the foundation's identical one (§14.6 rule 2).
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingIfTheEnvelopeDoesNotVerifyAndLogItAsync()
        {
            // given
            ContentItemSetting randomContentItemSetting = CreateRandomOverrideRequest();

            EventEnvelope<ContentItemSetting> inputEnvelope =
                CreateRequestEnvelope(randomContentItemSetting);

            var invalidContentItemSettingEventOrchestrationException =
                new InvalidContentItemSettingEventOrchestrationException(
                    message: "Invalid content item setting event. Integrity verification failed.");

            var expectedContentItemSettingOrchestrationValidationException =
                new ContentItemSettingOrchestrationValidationException(
                    message: "Content item setting orchestration validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: invalidContentItemSettingEventOrchestrationException);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onAddingTask =
                this.contentItemSettingOrchestrationService.OnAddingContentItemSettingAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedContentItemSettingOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingOrchestrationValidationException))),
                Times.Once);

            // nothing was read and nothing was written on an envelope that did not verify
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "ContentItemSettingAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // The shape guard the verification cannot stand in for: a null envelope has nothing to
        // verify, and a null content has nothing to derive from.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingIfTheEnvelopeIsNotUsableAndLogItAsync()
        {
            // given
            EventEnvelope<ContentItemSetting> envelopeWithNoContent =
                CreateRequestEnvelope(contentItemSetting: null);

            var invalidContentItemSettingEventOrchestrationException =
                new InvalidContentItemSettingEventOrchestrationException(
                    message: "Invalid content item setting event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedContentItemSettingOrchestrationValidationException =
                new ContentItemSettingOrchestrationValidationException(
                    message: "Content item setting orchestration validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: invalidContentItemSettingEventOrchestrationException);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onAddingTask =
                this.contentItemSettingOrchestrationService.OnAddingContentItemSettingAsync(
                    envelopeWithNoContent,
                    TestContext.Current.CancellationToken);

            ContentItemSettingOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ContentItemSettingOrchestrationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedContentItemSettingOrchestrationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingOrchestrationValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
