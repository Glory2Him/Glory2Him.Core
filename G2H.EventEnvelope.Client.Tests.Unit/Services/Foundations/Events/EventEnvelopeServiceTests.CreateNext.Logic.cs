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
using System.Threading.Tasks;
using FluentAssertions;
using G2H.EventEnvelope.Client.Models.Foundations;
using Moq;

namespace G2H.EventEnvelope.Client.Tests.Unit.Services.Foundations.Events
{
    public partial class EventEnvelopeServiceTests
    {
        [Fact]
        public async Task ShouldCreateNextEventEnvelopeAsync()
        {
            // given
            EventEnvelope<string> randomSourceEnvelope = CreateRandomSourceEnvelope();
            EventEnvelope<string> inputSourceEnvelope = randomSourceEnvelope;
            string randomContent = GetRandomString();
            string inputContent = randomContent;
            Guid nextEventId = Guid.NewGuid();

            var expectedEventEnvelope = new EventEnvelope<string>
            {
                Content = inputContent,
                SecurityContext = inputSourceEnvelope.SecurityContext,
                RequestContext = inputSourceEnvelope.RequestContext,

                Metadata = new EventMetadata
                {
                    EventId = nextEventId,
                    EventType = nameof(String),
                    Version = 1,
                    RetryCount = 0,
                    CausationId = inputSourceEnvelope.Metadata.EventId.ToString(),
                    ParentCorrelationId = inputSourceEnvelope.Metadata.ParentCorrelationId
                }
            };

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(nextEventId);

            // when
            EventEnvelope<string> actualEventEnvelope =
                await this.eventEnvelopeService.CreateNextAsync(inputSourceEnvelope, inputContent);

            // then
            actualEventEnvelope.Should().BeEquivalentTo(expectedEventEnvelope);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                    Times.Once);

            this.securityBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
        }
    }
}
