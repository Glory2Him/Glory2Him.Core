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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        // one row per handler, so the whole event surface is covered by the envelope and
        // integrity checks below rather than by four near-identical copies
        public static TheoryData<string> ProcessingEventNames() =>
            new TheoryData<string>
            {
                "LinkProcessingAdding",
                "LinkProcessingModifying",
                "LinkProcessingRemovingById",
                "LinkProcessingRetrievingById"
            };

        private ValueTask<EventEnvelope<Link>?> InvokeHandlerAsync(
            string eventName,
            EventEnvelope<Link> envelope) =>
            eventName switch
            {
                "LinkProcessingAdding" =>
                    this.linkProcessingService.OnAddingLinkAsync(
                        envelope, TestContext.Current.CancellationToken),

                "LinkProcessingModifying" =>
                    this.linkProcessingService.OnModifyingLinkAsync(
                        envelope, TestContext.Current.CancellationToken),

                "LinkProcessingRemovingById" =>
                    this.linkProcessingService.OnRemovingLinkByIdAsync(
                        envelope, TestContext.Current.CancellationToken),

                _ =>
                    this.linkProcessingService.OnRetrievingLinkByIdAsync(
                        envelope, TestContext.Current.CancellationToken)
            };

        public static TheoryData<EventEnvelope<Link>?> MalformedEnvelopes() =>
            new TheoryData<EventEnvelope<Link>?>
            {
                null,

                new EventEnvelope<Link>
                {
                    Content = null!,
                    Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                },

                new EventEnvelope<Link>
                {
                    Content = new Link(),
                    Metadata = null!
                }
            };

        [Theory]
        [MemberData(nameof(MalformedEnvelopes))]
        public async Task ShouldThrowValidationExceptionOnAddingLinkIfEnvelopeIsMalformedAndLogItAsync(
            EventEnvelope<Link>? malformedEnvelope)
        {
            // given: a malformed event never reaches the do-work — content and metadata are
            // both required before the signature is even checked
            var invalidLinkProcessingEventException =
                new InvalidLinkProcessingEventException(
                    message: "Invalid link processing event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkProcessingEventException);

            // when
            ValueTask<EventEnvelope<Link>?> onAddingLinkTask =
                this.linkProcessingService.OnAddingLinkAsync(
                    malformedEnvelope!,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    onAddingLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ProcessingEventNames))]
        public async Task ShouldThrowValidationExceptionOnHandlerIfIntegrityVerificationFailsAndLogItAsync(
            string eventName)
        {
            // given: the processing service front-loads the contribution and permission
            // decisions against the inbound envelope's SecurityContext, so without this
            // check a caller who can put a message on a LinkProcessing address states their
            // own roles and is believed (design §14.6 rule 4)
            Link inputLink = CreateRandomLink();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext());

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    eventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidLinkProcessingEventException =
                new InvalidLinkProcessingEventException(
                    message: "Invalid link processing event. " +
                        "Integrity verification failed.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkProcessingEventException);

            // when
            ValueTask<EventEnvelope<Link>?> handlerTask =
                InvokeHandlerAsync(eventName, inboundEnvelope);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    handlerTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    eventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            // a forged envelope never reaches the foundation
            this.linkServiceMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnAddingLinkIfCallerIsNotAuthenticatedAndLogItAsync(
            SecurityContext? unauthenticatedSecurityContext)
        {
            // given: the event path runs the same contribution gate as the direct call —
            // the envelope's SecurityContext is the original caller, not the transport's
            Link inputLink = CreateRandomLink();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: unauthenticatedSecurityContext!);

            var unauthorizedLinkProcessingException =
                new UnauthorizedLinkProcessingException(
                    message: "The current user is not authenticated.");

            var expectedLinkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedLinkProcessingException);

            // when
            ValueTask<EventEnvelope<Link>?> onAddingLinkTask =
                this.linkProcessingService.OnAddingLinkAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            LinkProcessingValidationException actualLinkProcessingValidationException =
                await Assert.ThrowsAsync<LinkProcessingValidationException>(
                    onAddingLinkTask.AsTask);

            // then
            actualLinkProcessingValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingValidationException))),
                Times.Once);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ProcessingEventNames))]
        public async Task ShouldThrowOperationCanceledExceptionOnHandlerIfCancellationRequestedAsync(
            string eventName)
        {
            // given
            Link inputLink = CreateRandomLink();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext());

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            ValueTask<EventEnvelope<Link>?> handlerTask = eventName switch
            {
                "LinkProcessingAdding" =>
                    this.linkProcessingService.OnAddingLinkAsync(
                        inboundEnvelope, cancellationTokenSource.Token),

                "LinkProcessingModifying" =>
                    this.linkProcessingService.OnModifyingLinkAsync(
                        inboundEnvelope, cancellationTokenSource.Token),

                "LinkProcessingRemovingById" =>
                    this.linkProcessingService.OnRemovingLinkByIdAsync(
                        inboundEnvelope, cancellationTokenSource.Token),

                _ =>
                    this.linkProcessingService.OnRetrievingLinkByIdAsync(
                        inboundEnvelope, cancellationTokenSource.Token)
            };

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(handlerTask.AsTask);

            this.linkServiceMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
