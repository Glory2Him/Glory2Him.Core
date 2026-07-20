// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Factories.Events
{
    public class EventEnvelopeFactory : IEventEnvelopeFactory
    {
        private readonly IIdentifierBroker identifierBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly ISecurityBroker securityBroker;

        public EventEnvelopeFactory(
            IIdentifierBroker identifierBroker,
            IDateTimeBroker dateTimeBroker,
            ISecurityBroker securityBroker)
        {
            this.identifierBroker = identifierBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.securityBroker = securityBroker;
        }

        public async ValueTask<EventEnvelope<T>> CreateAsync<T>(T content)
        {
            SecurityContext securityContext =
                await this.securityBroker.GetCurrentSecurityContextAsync();

            Guid eventId = await this.identifierBroker.GetIdentifierAsync();
            Guid correlationId = await this.identifierBroker.GetIdentifierAsync();
            DateTimeOffset now = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            return new EventEnvelope<T>
            {
                Content = content,
                SecurityContext = securityContext,

                RequestContext = new RequestContext
                {
                    CorrelationId = correlationId,
                    RequestedDate = now,
                    SourceSystem = "Glory2Him.Core"
                },

                Metadata = new EventMetadata
                {
                    EventId = eventId,
                    EventType = typeof(T).Name,
                    Version = 1,
                    RetryCount = 0
                }
            };
        }

        public async ValueTask<EventEnvelope<T>> CreateNextAsync<TSource, T>(
            EventEnvelope<TSource> sourceEnvelope,
            T content)
        {
            Guid eventId = await this.identifierBroker.GetIdentifierAsync();

            return new EventEnvelope<T>
            {
                Content = content,
                SecurityContext = sourceEnvelope.SecurityContext,
                RequestContext = sourceEnvelope.RequestContext,

                Metadata = new EventMetadata
                {
                    EventId = eventId,
                    EventType = typeof(T).Name,
                    Version = 1,
                    RetryCount = 0,
                    CausationId = sourceEnvelope.Metadata.EventId.ToString(),
                    ParentCorrelationId = sourceEnvelope.Metadata.ParentCorrelationId
                }
            };
        }
    }
}
