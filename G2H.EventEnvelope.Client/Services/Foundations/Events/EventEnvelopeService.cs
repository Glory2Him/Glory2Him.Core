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
using System.Threading;
using System.Threading.Tasks;
using G2H.EventEnvelope.Client.Brokers.DateTimes;
using G2H.EventEnvelope.Client.Brokers.Identifiers;
using G2H.EventEnvelope.Client.Brokers.Securities;
using G2H.EventEnvelope.Client.Models.Foundations;

namespace G2H.EventEnvelope.Client.Services.Foundations.Events
{
    internal partial class EventEnvelopeService : IEventEnvelopeService
    {
        private readonly IIdentifierBroker identifierBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly ISecurityBroker securityBroker;

        public EventEnvelopeService(
            IIdentifierBroker identifierBroker,
            IDateTimeBroker dateTimeBroker,
            ISecurityBroker securityBroker)
        {
            this.identifierBroker = identifierBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.securityBroker = securityBroker;
        }

        public ValueTask<EventEnvelope<T>> CreateAsync<T>(
            T content,
            CancellationToken cancellationToken = default) =>
        TryCatch(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateOnCreate(content);

            EventSecurityContext securityContext =
                await this.securityBroker.GetCurrentSecurityContextAsync();

            Guid eventId = await this.identifierBroker.GetIdentifierAsync();
            Guid correlationId = await this.identifierBroker.GetIdentifierAsync();
            DateTimeOffset now = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            return new EventEnvelope<T>
            {
                Content = content,
                SecurityContext = securityContext,

                RequestContext = new EventRequestContext
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
        });

        public ValueTask<EventEnvelope<T>> CreateNextAsync<TSource, T>(
            EventEnvelope<TSource> sourceEnvelope,
            T content,
            CancellationToken cancellationToken = default) =>
        TryCatch(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateOnCreateNext(sourceEnvelope, content);
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
        });
    }
}
