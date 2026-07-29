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
        public async Task ShouldCreateEventEnvelopeAsync()
        {
            // given
            string randomContent = GetRandomString();
            string inputContent = randomContent;
            EventSecurityContext randomSecurityContext = CreateRandomEventSecurityContext();
            Guid eventId = Guid.NewGuid();
            Guid correlationId = Guid.NewGuid();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            var expectedEventEnvelope = new EventEnvelope<string>
            {
                Content = inputContent,
                SecurityContext = randomSecurityContext,

                RequestContext = new EventRequestContext
                {
                    CorrelationId = correlationId,
                    RequestedDate = randomDateTimeOffset,
                    SourceSystem = "Glory2Him.Core"
                },

                Metadata = new EventMetadata
                {
                    EventId = eventId,
                    EventType = nameof(String),
                    Version = 1,
                    RetryCount = 0
                }
            };

            this.securityBrokerMock.Setup(broker =>
                broker.GetCurrentSecurityContextAsync())
                    .ReturnsAsync(randomSecurityContext);

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(eventId)
                    .ReturnsAsync(correlationId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            EventEnvelope<string> actualEventEnvelope =
                await this.eventEnvelopeService.CreateAsync(inputContent);

            // then
            actualEventEnvelope.Should().BeEquivalentTo(expectedEventEnvelope);

            this.securityBrokerMock.Verify(broker =>
                broker.GetCurrentSecurityContextAsync(),
                    Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                    Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                    Times.Once);

            this.securityBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
        }
    }
}
