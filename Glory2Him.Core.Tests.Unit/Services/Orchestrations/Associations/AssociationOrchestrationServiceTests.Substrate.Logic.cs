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
    }
}
