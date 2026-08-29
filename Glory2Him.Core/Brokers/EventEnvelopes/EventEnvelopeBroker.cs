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
using Glory2Him.Core.Models.Securities;
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

        // Both system mints are built on CreateAsync rather than beside it, so metadata,
        // correlation and every other carried section are minted exactly as any other
        // envelope's are — only the identity differs, and it differs in one visible place.
        public async ValueTask<EventEnvelope<T>> CreateSystemAsync<T>(T content) =>
            await CreateSystemContextAsync(
                content: content,
                isCarryingOutACallersDecision: false);

        public async ValueTask<EventEnvelope<T>> CreateElevatedAsync<T>(T content) =>
            await CreateSystemContextAsync(
                content: content,
                isCarryingOutACallersDecision: true);

        // ONE writer for both, because the difference between them is a single field and two
        // near-identical copies would drift. The caller picks the ACT through the two public
        // methods above; it never supplies an identity, so it can only ever elect to be recorded
        // as itself and the flag stays unforgeable by construction (§16.7.1).
        private async ValueTask<EventEnvelope<T>> CreateSystemContextAsync<T>(
            T content,
            bool isCarryingOutACallersDecision)
        {
            EventEnvelope<T> callerEnvelope = await CreateAsync(content);
            string? callerSubjectId = callerEnvelope.SecurityContext?.SubjectId;

            return new EventEnvelope<T>
            {
                Content = callerEnvelope.Content,
                RequestContext = callerEnvelope.RequestContext,
                Metadata = callerEnvelope.Metadata,
                Integrity = callerEnvelope.Integrity,

                SecurityContext = new SecurityContext
                {
                    // Whose act it is decides whose name the audit columns carry. Carrying out a
                    // person's decision keeps the person — the audit answer to "who approved
                    // this" is a human. Acting on its own account records the system, because
                    // stamping whichever request happened to be on the stack would name somebody
                    // who did not act. Either way the triggering person survives on
                    // DelegatedBySubjectId, so the causal trail is not lost to make the audit
                    // truthful. Roles are dropped in both: the flag stands in for the publisher
                    // tier by itself, and carrying them would leave a context that looks like it
                    // was authorised two different ways.
                    SubjectId = isCarryingOutACallersDecision
                        ? callerSubjectId
                        : SystemIdentity.UserId,

                    Username = isCarryingOutACallersDecision
                        ? callerEnvelope.SecurityContext?.Username
                        : SystemIdentity.Username,

                    DelegatedBySubjectId = callerSubjectId,
                    TenantId = callerEnvelope.SecurityContext?.TenantId,
                    IsAuthenticated = true,
                    IsSystemIdentity = true,

                    // The two enum members that existed for this and had never been minted.
                    AuthenticationType = isCarryingOutACallersDecision
                        ? AuthenticationType.Delegated
                        : AuthenticationType.System,
                }
            };
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

        // EventEnvelope declares defaults for these three, but a property initializer does not
        // survive an explicit null in JSON — and the inbound path is a bare
        // JsonSerializer.Deserialize (EventBroker.DeserializeEnvelope), so a stored event
        // carrying "SecurityContext": null really does produce one. The read handlers reach
        // here with it: they short-circuit for a publicly-visible row before the security gate
        // runs, then build a reply through CreateNextAsync.
        //
        // Restoring the declared default is fail-closed by construction: IsAuthenticated
        // defaults to false, so a null context becomes an unauthenticated one and every gate
        // refuses it exactly as it refuses an envelope that omitted the field.
        private static ExternalEventEnvelopes.EventEnvelope<T> ConvertToExternalEventEnvelope<T>(
            EventEnvelope<T> eventEnvelope)
        {
            SecurityContext securityContext =
                eventEnvelope.SecurityContext ?? new SecurityContext();

            RequestContext requestContext =
                eventEnvelope.RequestContext ?? new RequestContext();

            EventMetadata metadata =
                eventEnvelope.Metadata ?? new EventMetadata();

            return new ExternalEventEnvelopes.EventEnvelope<T>
            {
                Content = eventEnvelope.Content,

                SecurityContext = new ExternalEventEnvelopes.EventSecurityContext
                {
                    SubjectId = securityContext.SubjectId,
                    Username = securityContext.Username,
                    TenantId = securityContext.TenantId,
                    Roles = securityContext.Roles,
                    Scopes = securityContext.Scopes,
                    Permissions = securityContext.Permissions,
                    IsAuthenticated = securityContext.IsAuthenticated,

                    AuthenticationType =
                        (G2H.EventEnvelope.Client.Models.Securities.AuthenticationType)
                            securityContext.AuthenticationType,

                    ClientId = securityContext.ClientId,
                    ClientApplicationName = securityContext.ClientApplicationName,
                    IsSystemIdentity = securityContext.IsSystemIdentity,
                    DelegatedBySubjectId = securityContext.DelegatedBySubjectId
                },

                RequestContext = new ExternalEventEnvelopes.EventRequestContext
                {
                    CorrelationId = requestContext.CorrelationId,
                    RequestedDate = requestContext.RequestedDate,
                    RequestId = requestContext.RequestId,
                    SourceSystem = requestContext.SourceSystem,
                    ClientApplicationId = requestContext.ClientApplicationId
                },

                Metadata = new ExternalEventEnvelopes.EventMetadata
                {
                    EventId = metadata.EventId,
                    EventType = metadata.EventType,
                    Version = metadata.Version,
                    RetryCount = metadata.RetryCount,
                    CausationId = metadata.CausationId,
                    ParentCorrelationId = metadata.ParentCorrelationId
                },

                Integrity = eventEnvelope.Integrity is null
                    ? null
                    : new ExternalEventEnvelopes.EventEnvelopeIntegrity
                    {
                        Algorithm = eventEnvelope.Integrity.Algorithm,
                        KeyId = eventEnvelope.Integrity.KeyId,
                        Signature = eventEnvelope.Integrity.Signature,
                        SignedDate = eventEnvelope.Integrity.SignedDate
                    }
            };
        }

        // The inbound direction, and the one that matters most: this is where an envelope
        // built elsewhere becomes a Core envelope. Same reasoning as above — restore the
        // declared defaults rather than dereference whatever arrived.
        private static EventEnvelope<T> ConvertToEventEnvelope<T>(
            ExternalEventEnvelopes.EventEnvelope<T> externalEventEnvelope)
        {
            ExternalEventEnvelopes.EventSecurityContext securityContext =
                externalEventEnvelope.SecurityContext
                    ?? new ExternalEventEnvelopes.EventSecurityContext();

            ExternalEventEnvelopes.EventRequestContext requestContext =
                externalEventEnvelope.RequestContext
                    ?? new ExternalEventEnvelopes.EventRequestContext();

            ExternalEventEnvelopes.EventMetadata metadata =
                externalEventEnvelope.Metadata
                    ?? new ExternalEventEnvelopes.EventMetadata();

            return new EventEnvelope<T>
            {
                Content = externalEventEnvelope.Content,

                SecurityContext = new SecurityContext
                {
                    SubjectId = securityContext.SubjectId,
                    Username = securityContext.Username,
                    TenantId = securityContext.TenantId,
                    Roles = securityContext.Roles,
                    Scopes = securityContext.Scopes,
                    Permissions = securityContext.Permissions,
                    IsAuthenticated = securityContext.IsAuthenticated,
                    AuthenticationType =
                        (Glory2Him.Core.Models.Events.AuthenticationType)
                            securityContext.AuthenticationType,
                    ClientId = securityContext.ClientId,
                    ClientApplicationName = securityContext.ClientApplicationName,
                    IsSystemIdentity = securityContext.IsSystemIdentity,
                    DelegatedBySubjectId = securityContext.DelegatedBySubjectId
                },

                RequestContext = new RequestContext
                {
                    CorrelationId = requestContext.CorrelationId,
                    RequestedDate = requestContext.RequestedDate,
                    RequestId = requestContext.RequestId,
                    SourceSystem = requestContext.SourceSystem,
                    ClientApplicationId = requestContext.ClientApplicationId
                },

                Metadata = new EventMetadata
                {
                    EventId = metadata.EventId,
                    EventType = metadata.EventType,
                    Version = metadata.Version,
                    RetryCount = metadata.RetryCount,
                    CausationId = metadata.CausationId,
                    ParentCorrelationId = metadata.ParentCorrelationId
                },

                Integrity = externalEventEnvelope.Integrity is null
                    ? null
                    : new EnvelopeIntegrity
                    {
                        Algorithm = externalEventEnvelope.Integrity.Algorithm,
                        KeyId = externalEventEnvelope.Integrity.KeyId,
                        Signature = externalEventEnvelope.Integrity.Signature,
                        SignedDate = externalEventEnvelope.Integrity.SignedDate
                    }
            };
        }
    }
}
