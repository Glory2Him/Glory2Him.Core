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
using G2H.EventEnvelope.Client.Clients;
using Glory2Him.Core.Models.Events;
using ExternalEventEnvelopes = G2H.EventEnvelope.Client.Models.Foundations;

namespace Glory2Him.Core.Brokers.EventEnvelopes
{
    internal class EventEnvelopeBroker : IEventEnvelopeBroker
    {
        private readonly IEventEnvelopeClient eventEnvelopeClient;

        public EventEnvelopeBroker() =>
            this.eventEnvelopeClient = new EventEnvelopeClient();

        public async ValueTask<EventEnvelope<T>> CreateAsync<T>(T content)
        {
            ExternalEventEnvelopes.EventEnvelope<T> externalEventEnvelope =
                await this.eventEnvelopeClient.CreateAsync(content);

            return ConvertToEventEnvelope(externalEventEnvelope);
        }

        public async ValueTask<EventEnvelope<T>> CreateNextAsync<TSource, T>(
            EventEnvelope<TSource> sourceEnvelope,
            T content)
        {
            ExternalEventEnvelopes.EventEnvelope<TSource> externalSourceEnvelope =
                ConvertToExternalEventEnvelope(sourceEnvelope);

            ExternalEventEnvelopes.EventEnvelope<T> externalEventEnvelope =
                await this.eventEnvelopeClient.CreateNextAsync(externalSourceEnvelope, content);

            return ConvertToEventEnvelope(externalEventEnvelope);
        }

        private static ExternalEventEnvelopes.EventEnvelope<T> ConvertToExternalEventEnvelope<T>(
            EventEnvelope<T> eventEnvelope) =>
            new ExternalEventEnvelopes.EventEnvelope<T>
            {
                Content = eventEnvelope.Content,

                SecurityContext = new ExternalEventEnvelopes.EventSecurityContext
                {
                    SubjectId = eventEnvelope.SecurityContext.SubjectId,
                    Username = eventEnvelope.SecurityContext.Username,
                    TenantId = eventEnvelope.SecurityContext.TenantId,
                    Roles = eventEnvelope.SecurityContext.Roles,
                    Scopes = eventEnvelope.SecurityContext.Scopes,
                    Permissions = eventEnvelope.SecurityContext.Permissions,
                    IsAuthenticated = eventEnvelope.SecurityContext.IsAuthenticated,

                    AuthenticationType =
                        (G2H.EventEnvelope.Client.Models.Securities.AuthenticationType)
                            eventEnvelope.SecurityContext.AuthenticationType,

                    ClientId = eventEnvelope.SecurityContext.ClientId,
                    ClientApplicationName = eventEnvelope.SecurityContext.ClientApplicationName,
                    IsSystemIdentity = eventEnvelope.SecurityContext.IsSystemIdentity,
                    DelegatedBySubjectId = eventEnvelope.SecurityContext.DelegatedBySubjectId
                },

                RequestContext = new ExternalEventEnvelopes.EventRequestContext
                {
                    CorrelationId = eventEnvelope.RequestContext.CorrelationId,
                    RequestedDate = eventEnvelope.RequestContext.RequestedDate,
                    RequestId = eventEnvelope.RequestContext.RequestId,
                    SourceSystem = eventEnvelope.RequestContext.SourceSystem,
                    ClientApplicationId = eventEnvelope.RequestContext.ClientApplicationId
                },

                Metadata = new ExternalEventEnvelopes.EventMetadata
                {
                    EventId = eventEnvelope.Metadata.EventId,
                    EventType = eventEnvelope.Metadata.EventType,
                    Version = eventEnvelope.Metadata.Version,
                    RetryCount = eventEnvelope.Metadata.RetryCount,
                    CausationId = eventEnvelope.Metadata.CausationId,
                    ParentCorrelationId = eventEnvelope.Metadata.ParentCorrelationId
                },

                Integrity = eventEnvelope.Integrity is null
                    ? null
                    : new ExternalEventEnvelopes.EventEnvelopeIntegrity
                    {
                        Algorithm = eventEnvelope.Integrity.Algorithm,
                        Signature = eventEnvelope.Integrity.Signature,
                        SignedDate = eventEnvelope.Integrity.SignedDate
                    }
            };

        private static EventEnvelope<T> ConvertToEventEnvelope<T>(
            ExternalEventEnvelopes.EventEnvelope<T> externalEventEnvelope) =>
            new EventEnvelope<T>
            {
                Content = externalEventEnvelope.Content,

                SecurityContext = new SecurityContext
                {
                    SubjectId = externalEventEnvelope.SecurityContext.SubjectId,
                    Username = externalEventEnvelope.SecurityContext.Username,
                    TenantId = externalEventEnvelope.SecurityContext.TenantId,
                    Roles = externalEventEnvelope.SecurityContext.Roles,
                    Scopes = externalEventEnvelope.SecurityContext.Scopes,
                    Permissions = externalEventEnvelope.SecurityContext.Permissions,
                    IsAuthenticated = externalEventEnvelope.SecurityContext.IsAuthenticated,
                    AuthenticationType =
                        (Glory2Him.Core.Models.Events.AuthenticationType)
                            externalEventEnvelope.SecurityContext.AuthenticationType,
                    ClientId = externalEventEnvelope.SecurityContext.ClientId,
                    ClientApplicationName = externalEventEnvelope.SecurityContext.ClientApplicationName,
                    IsSystemIdentity = externalEventEnvelope.SecurityContext.IsSystemIdentity,
                    DelegatedBySubjectId = externalEventEnvelope.SecurityContext.DelegatedBySubjectId
                },

                RequestContext = new RequestContext
                {
                    CorrelationId = externalEventEnvelope.RequestContext.CorrelationId,
                    RequestedDate = externalEventEnvelope.RequestContext.RequestedDate,
                    RequestId = externalEventEnvelope.RequestContext.RequestId,
                    SourceSystem = externalEventEnvelope.RequestContext.SourceSystem,
                    ClientApplicationId = externalEventEnvelope.RequestContext.ClientApplicationId
                },

                Metadata = new EventMetadata
                {
                    EventId = externalEventEnvelope.Metadata.EventId,
                    EventType = externalEventEnvelope.Metadata.EventType,
                    Version = externalEventEnvelope.Metadata.Version,
                    RetryCount = externalEventEnvelope.Metadata.RetryCount,
                    CausationId = externalEventEnvelope.Metadata.CausationId,
                    ParentCorrelationId = externalEventEnvelope.Metadata.ParentCorrelationId
                },

                Integrity = externalEventEnvelope.Integrity is null
                    ? null
                    : new EnvelopeIntegrity
                    {
                        Algorithm = externalEventEnvelope.Integrity.Algorithm,
                        Signature = externalEventEnvelope.Integrity.Signature,
                        SignedDate = externalEventEnvelope.Integrity.SignedDate
                    }
            };
    }
}
