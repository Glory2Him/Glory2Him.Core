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

using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class ServiceRegistrationTests
    {
        private static IServiceCollection CreateServicesWithBrokerStubs()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new Mock<IStorageBroker>().Object);
            services.AddSingleton(new Mock<IDateTimeBroker>().Object);
            services.AddSingleton(new Mock<IIdentifierBroker>().Object);
            services.AddSingleton(new Mock<IEventBroker>().Object);
            services.AddSingleton(new Mock<IEventEnvelopeBroker>().Object);
            services.AddSingleton(new Mock<ISecurityAuditBroker>().Object);
            services.AddSingleton(new Mock<IAccessBroker>().Object);
            services.AddSingleton(new Mock<IEnvelopeIntegrityBroker>().Object);
            services.AddSingleton(new Mock<ILoggingBroker>().Object);

            return services;
        }
    }
}
