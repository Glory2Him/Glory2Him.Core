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
using G2H.EventEnvelope.Client.Brokers.DateTimes;
using G2H.EventEnvelope.Client.Brokers.Identifiers;
using G2H.EventEnvelope.Client.Brokers.Securities;
using G2H.EventEnvelope.Client.Models.Foundations;
using G2H.EventEnvelope.Client.Services.Foundations.Events;
using Moq;
using Tynamix.ObjectFiller;

namespace G2H.EventEnvelope.Client.Tests.Unit.Services.Foundations.Events
{
    public partial class EventEnvelopeServiceTests
    {
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<ISecurityBroker> securityBrokerMock;
        private readonly IEventEnvelopeService eventEnvelopeService;

        public EventEnvelopeServiceTests()
        {
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.securityBrokerMock = new Mock<ISecurityBroker>();

            this.eventEnvelopeService = new EventEnvelopeService(
                identifierBroker: this.identifierBrokerMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                securityBroker: this.securityBrokerMock.Object);
        }

        private static EventSecurityContext CreateRandomEventSecurityContext() =>
            new EventSecurityContext
            {
                SubjectId = GetRandomString(),
                Username = GetRandomString(),
                IsAuthenticated = true,
                Roles = new[] { GetRandomString() }
            };

        private static EventEnvelope<string> CreateRandomSourceEnvelope() =>
            new EventEnvelope<string>
            {
                Content = GetRandomString(),
                SecurityContext = CreateRandomEventSecurityContext(),

                RequestContext = new EventRequestContext
                {
                    CorrelationId = Guid.NewGuid(),
                    RequestedDate = GetRandomDateTimeOffset(),
                    SourceSystem = "Glory2Him.Core"
                },

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    EventType = nameof(String),
                    Version = 1,
                    RetryCount = 0,
                    ParentCorrelationId = Guid.NewGuid()
                }
            };

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();
    }
}
