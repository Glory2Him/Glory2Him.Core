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

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    // THE REST OF THE REFUSAL SET (#631 criterion 3, Architecture.md "RULE — on the event path,
    // every value the write flow derives"). The foundation takes UserId and a versioned endpoint's
    // group id from its caller as handed, so on the event path a claim that differs from the
    // derivation is refused rather than edited out of signed content.
    public partial class AssociationOrchestrationServiceTests
    {
        public static TheoryData<string> ClaimedUserIds() =>
            new TheoryData<string>
            {
                "a-claimed-user-id",
                "",
                "   ",
            };

        // 3b. The derived UserId is null on every path until the personal-reaction derivation
        // exists, so every non-null claim differs — an empty or blank string included. A non-null
        // UserId makes the row personal, and that tier is seeded to auto-approve.
        [Theory]
        [MemberData(nameof(ClaimedUserIds))]
        public async Task ShouldRefuseAClaimedUserIdOnTheEventPathAsync(string claimedUserId)
        {
            // given
            Association addRequest = CreateHonestAddRequest();
            addRequest.UserId = claimedUserId;
            EventEnvelope<Association> inputEnvelope = CreateRequestEnvelope(addRequest);
            SetupEventPathEndpointReads(addRequest, inputEnvelope);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: nameof(Association.UserId),
                values: "Value is derived and must not be supplied");

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
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
