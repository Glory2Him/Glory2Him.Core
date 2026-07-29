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
using G2H.EventEnvelope.Client.Models.Foundations;
using Moq;

namespace G2H.EventEnvelope.Client.Tests.Unit.Clients
{
    public partial class EventEnvelopeClientTests
    {
        [Fact]
        public async Task ShouldCreateEventEnvelopeAsync()
        {
            // given
            string randomContent = GetRandomString();
            EventEnvelope<string> randomEventEnvelope = CreateRandomEventEnvelope();
            EventEnvelope<string> expectedEventEnvelope = randomEventEnvelope;

            this.eventEnvelopeServiceMock.Setup(service =>
                service.CreateAsync(randomContent, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(randomEventEnvelope);

            // when
            EventEnvelope<string> actualEventEnvelope =
                await this.eventEnvelopeClient.CreateAsync(randomContent);

            // then
            actualEventEnvelope.Should().BeEquivalentTo(expectedEventEnvelope);

            this.eventEnvelopeServiceMock.Verify(service =>
                service.CreateAsync(randomContent, It.IsAny<CancellationToken>()),
                    Times.Once);

            this.eventEnvelopeServiceMock.VerifyNoOtherCalls();
        }
    }
}
