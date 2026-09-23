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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        // THE DELEGATED CALL FAILING. The foundation's own dependency and service failures keep
        // the category they have on the method path, carrying the foundation's inner exception
        // rather than the foundation's type (§ARC12.2).
        [Theory]
        [MemberData(nameof(AssociationDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddingIfTheFoundationHandlerFailsAndLogItAsync(
            Xeption foundationException)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            this.associationServiceMock.Setup(service =>
                service.OnAddingAssociationAsync(
                    inputEnvelope,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<EventEnvelope<Association>> onAddingTask =
                this.associationOrchestrationService.OnAddingAssociationAsync(
                    inputEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
