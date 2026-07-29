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
using Moq;

namespace G2H.EventEnvelope.Client.Tests.Unit.Clients
{
    public partial class EventEnvelopeClientTests
    {
        [Fact]
        public async Task ShouldCreateNextEventEnvelopeAsync()
        {
            // given
            EventEnvelope<string> randomSourceEnvelope = CreateRandomEventEnvelope();
            string randomContent = GetRandomString();
            EventEnvelope<string> randomNextEventEnvelope = CreateRandomEventEnvelope();
            EventEnvelope<string> expectedEventEnvelope = randomNextEventEnvelope;

            this.eventEnvelopeServiceMock.Setup(service =>
                service.CreateNextAsync(randomSourceEnvelope, randomContent))
                    .ReturnsAsync(randomNextEventEnvelope);

            // when
            EventEnvelope<string> actualEventEnvelope =
                await this.eventEnvelopeClient.CreateNextAsync(randomSourceEnvelope, randomContent);

            // then
            actualEventEnvelope.Should().BeEquivalentTo(expectedEventEnvelope);

            this.eventEnvelopeServiceMock.Verify(service =>
                service.CreateNextAsync(randomSourceEnvelope, randomContent),
                    Times.Once);

            this.eventEnvelopeServiceMock.VerifyNoOtherCalls();
        }
    }
}
