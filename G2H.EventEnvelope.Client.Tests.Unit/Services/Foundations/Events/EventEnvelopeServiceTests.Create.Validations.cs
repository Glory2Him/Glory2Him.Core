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

using System.Threading.Tasks;
using FluentAssertions;
using G2H.EventEnvelope.Client.Models.Foundations;
using G2H.EventEnvelope.Client.Models.Foundations.Exceptions;

namespace G2H.EventEnvelope.Client.Tests.Unit.Services.Foundations.Events
{
    public partial class EventEnvelopeServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnCreateIfContentIsNullAndLogItAsync()
        {
            // given
            string? nullContent = null;

            var invalidArgumentEventEnvelopeException =
                new InvalidArgumentEventEnvelopeException(
                    message: "Invalid event envelope argument(s), correct the errors and try again.");

            invalidArgumentEventEnvelopeException.AddData(
                key: "Content",
                values: "Value is required");

            var expectedEventEnvelopeValidationException =
                new EventEnvelopeValidationException(
                    message: "Event envelope validation errors occurred, please try again.",
                    innerException: invalidArgumentEventEnvelopeException);

            // when
            ValueTask<EventEnvelope<string>> createTask =
                this.eventEnvelopeService.CreateAsync(nullContent!, TestContext.Current.CancellationToken);

            EventEnvelopeValidationException actualEventEnvelopeValidationException =
                await Assert.ThrowsAsync<EventEnvelopeValidationException>(createTask.AsTask);

            // then
            actualEventEnvelopeValidationException.Should()
                .BeEquivalentTo(expectedEventEnvelopeValidationException);

            this.securityBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
        }
    }
}
