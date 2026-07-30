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
using G2H.EventEnvelope.Client.Clients;
using G2H.EventEnvelope.Client.Models.Foundations;
using G2H.EventEnvelope.Client.Models.Foundations.Exceptions;
using G2H.EventEnvelope.Client.Services.Foundations.Events;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace G2H.EventEnvelope.Client.Tests.Unit.Clients
{
    public partial class EventEnvelopeClientTests
    {
        private readonly Mock<IEventEnvelopeService> eventEnvelopeServiceMock;
        private readonly IEventEnvelopeClient eventEnvelopeClient;

        public EventEnvelopeClientTests()
        {
            this.eventEnvelopeServiceMock = new Mock<IEventEnvelopeService>();

            this.eventEnvelopeClient = new EventEnvelopeClient(
                eventEnvelopeService: this.eventEnvelopeServiceMock.Object);
        }

        public static TheoryData<Xeption> ValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(randomMessage);

            return new TheoryData<Xeption>
            {
                new EventEnvelopeValidationException(
                    message: "Event envelope validation errors occurred, please try again.",
                    innerException),

                new EventEnvelopeDependencyValidationException(
                    message: "Event envelope dependency validation error occurred, please try again.",
                    innerException),
            };
        }

        public static TheoryData<Xeption> DependencyExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(randomMessage);

            return new TheoryData<Xeption>
            {
                new EventEnvelopeDependencyException(
                    message: "Event envelope dependency error occurred, please try again.",
                    innerException),

                new EventEnvelopeServiceException(
                    message: "Event envelope service error occurred, please contact support.",
                    innerException)
            };
        }

        private static EventEnvelope<string> CreateRandomEventEnvelope() =>
            new EventEnvelope<string>
            {
                Content = GetRandomString(),
                SecurityContext = new EventSecurityContext { IsAuthenticated = true },

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
                    Version = 1
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
