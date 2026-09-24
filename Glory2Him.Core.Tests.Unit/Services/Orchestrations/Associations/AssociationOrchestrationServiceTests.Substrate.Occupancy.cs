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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    // THE OCCUPANCY CHECK ON THE EVENT PATH (#631 criteria 1b, 2a, 4b-4d; Architecture.md "Rule 2
    // — the occupancy check runs on both doors"). The unique index covers undeleted rows only, so
    // without the probes a publisher could re-insert over a moderator's takedown, and an overlap
    // would never be seen.
    public partial class AssociationOrchestrationServiceTests
    {
        // 1b. The pair probe, then the overlap probe, then the foundation — each call recorded as
        // it happens, so a delegation made before either probe cannot pass.
        [Fact]
        public async Task ShouldDelegateOnlyAfterBothOccupancyProbesFindThePairUnoccupiedOnTheEventPathAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);
            var calls = new List<string>();

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .Callback(() => calls.Add("pair probe"))
                        .ReturnsAsync((AssociationPairMatch)null);

            this.associationServiceMock.Setup(service =>
                service.FindOverlappingAssociationAsync(
                    It.IsAny<Association>(),
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .Callback(() => calls.Add("overlap probe"))
                        .ReturnsAsync((AssociationPairMatch)null);

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .Callback(() => calls.Add("foundation handler"))
                        .ReturnsAsync(inputEnvelope);

            // when
            EventEnvelope<Association> actualReplyEnvelope =
                await this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeSameAs(inputEnvelope);

            calls.Should().Equal("pair probe", "overlap probe", "foundation handler");

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // 2a. The probes' contribution gate is identity, so on this path it is asked of the
        // SIGNED caller: through the inbound-envelope overloads, handed the inbound envelope
        // itself, and never through the minting members, which would read the ambient caller.
        [Fact]
        public async Task ShouldProbeThroughTheInboundEnvelopeOverloadsOnTheEventPathAsync()
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(inputEnvelope);

            // when
            await this.associationOrchestrationService.OnAddingAssociationAsync(
                inputEnvelope,
                TestContext.Current.CancellationToken);

            // then
            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindOverlappingAssociationAsync(
                    It.IsAny<Association>(),
                    inputEnvelope,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.Verify(service =>
                service.FindOverlappingAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.Is<EventEnvelope<Association>>(envelope => envelope != inputEnvelope),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.Verify(service =>
                service.FindOverlappingAssociationAsync(
                    It.IsAny<Association>(),
                    It.Is<EventEnvelope<Association>>(envelope => envelope != inputEnvelope),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Association>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // The five ways a pair can be occupied: a live row on the exact pair in each approval
        // state that can hold it, a soft-deleted row there, and a live row that overlaps it.
        public static TheoryData<string> Occupants() =>
            new TheoryData<string>
            {
                "a live approved row on the pair",
                "a live pending row on the pair",
                "a live rejected row on the pair",
                "a soft-deleted row on the pair",
                "a live row that overlaps the pair",
            };

        // Stubs the two probes as the named occupant would answer them, and hands back the row's
        // projection so a test can check none of it leaks.
        private AssociationPairMatch SetupOccupant(
            string occupant,
            EventEnvelope<Association> inboundEnvelope)
        {
            AssociationPairMatch pairMatch = occupant switch
            {
                "a live approved row on the pair" =>
                    CreatePairMatch(ApprovalStatus.Approved, isDeleted: false),

                "a live pending row on the pair" =>
                    CreatePairMatch(ApprovalStatus.Submitted, isDeleted: false),

                "a live rejected row on the pair" =>
                    CreatePairMatch(ApprovalStatus.Rejected, isDeleted: false),

                "a soft-deleted row on the pair" =>
                    CreatePairMatch(ApprovalStatus.Approved, isDeleted: true),

                _ => null,
            };

            AssociationPairMatch overlapMatch =
                pairMatch is null ? CreatePairMatch(ApprovalStatus.Approved, isDeleted: false) : null;

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    inboundEnvelope,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(pairMatch);

            this.associationServiceMock.Setup(service =>
                service.FindOverlappingAssociationAsync(
                    It.IsAny<Association>(),
                    inboundEnvelope,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(overlapMatch);

            return pairMatch ?? overlapMatch;
        }

        // 4c. The method path answers an occupant with a status; this door cannot. Its reply is
        // an EventEnvelope<Association>: returning the occupant leaks what the status projection
        // exists to hide, null claims the event was already applied, and a new shape is a
        // contract change. So every occupant is refused, and the foundation — which alone could
        // record, write or publish — is never reached.
        [Theory]
        [MemberData(nameof(Occupants))]
        public async Task ShouldRefuseAnOccupiedPairOnTheEventPathAsync(string occupant)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);
            SetupOccupant(occupant, inputEnvelope);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "The content item association's pair is already occupied.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedValidationException,
                because: $"{occupant} occupies the pair");

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
