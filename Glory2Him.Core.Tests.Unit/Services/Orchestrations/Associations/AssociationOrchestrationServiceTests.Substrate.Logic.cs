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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    // THE ASSOCIATION-ADDING EVENT PATH (#631). The address binds this orchestration rather than
    // the foundation, because its handler derives Entity{A,B}ContentType — an authorization input
    // — from the endpoints it resolves. The foundation keeps everything else: these pin that the
    // envelope reaches it unchanged, and that nothing the method path gates is walked past.
    public partial class AssociationOrchestrationServiceTests
    {
        // THE SAME ENVELOPE, NOT A REBUILT ONE. The foundation's deduplication, audit stamping,
        // write, fact and reply all key on the identity and causation this envelope carries, and
        // its signature covers the content — so the reference handed down must be the one that
        // arrived, and its content must be exactly what the publisher signed.
        [Fact]
        public async Task ShouldDelegateTheUnchangedEnvelopeToTheFoundationOnTheEventPathAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            Association expectedContent = addRequest.DeepClone();
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            EventEnvelope<Association> expectedReplyEnvelope =
                CreateRequestEnvelope(addRequest.DeepClone());

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedReplyEnvelope);

            // when
            EventEnvelope<Association> actualReplyEnvelope =
                await this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeSameAs(expectedReplyEnvelope);
            inputEnvelope.Content.Should().BeEquivalentTo(expectedContent);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Association>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A REPLAY DOES NO WORK. Deduplication belongs to the foundation, and this handler now
        // runs AHEAD of it — so without the early question a re-delivered envelope would read
        // both endpoint rows before anything noticed the event was already applied. If an
        // endpoint has since been soft-deleted, or stopped being visible to the signed caller,
        // that read fails and a settled write is recorded as a failed delivery and retried.
        //
        // Asserted as "no endpoint was read and the foundation handler was never called",
        // because a short-circuit that still pays for the reads is the bug half-fixed.
        [Fact]
        public async Task ShouldShortCircuitADuplicateBeforeResolvingEndpointsOnTheEventPathAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            this.associationServiceMock.Setup(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Association> actualReplyEnvelope =
                await this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeNull();

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inputEnvelope,
                    "AssociationAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.HasAlreadyAddedAssociationAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // the derivation never ran, so a since-deleted endpoint cannot fail a settled replay,
            // and the foundation handler was never reached
            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
            this.commentServiceMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
