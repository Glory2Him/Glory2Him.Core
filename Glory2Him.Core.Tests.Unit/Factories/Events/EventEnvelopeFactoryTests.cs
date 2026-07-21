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
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Factories.Events;
using Glory2Him.Core.Models.Events;
using Moq;
using Tynamix.ObjectFiller;

namespace Glory2Him.Core.Tests.Unit.Factories.Events
{
    public partial class EventEnvelopeFactoryTests
    {
        private readonly Mock<IIdentifierBroker> identifierBrokerMock;
        private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
        private readonly Mock<ISecurityBroker> securityBrokerMock;
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;

        public EventEnvelopeFactoryTests()
        {
            this.identifierBrokerMock = new Mock<IIdentifierBroker>();
            this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
            this.securityBrokerMock = new Mock<ISecurityBroker>();

            this.eventEnvelopeFactory = new EventEnvelopeFactory(
                identifierBroker: this.identifierBrokerMock.Object,
                dateTimeBroker: this.dateTimeBrokerMock.Object,
                securityBroker: this.securityBrokerMock.Object);
        }

        private static DateTimeOffset GetRandomDateTimeOffset() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static SecurityContext CreateRandomSecurityContext() =>
            new SecurityContext
            {
                SubjectId = GetRandomString(),
                Username = GetRandomString(),
                Roles = new[] { GetRandomString() },
                IsAuthenticated = true,
                AuthenticationType = AuthenticationType.User
            };
    }
}
