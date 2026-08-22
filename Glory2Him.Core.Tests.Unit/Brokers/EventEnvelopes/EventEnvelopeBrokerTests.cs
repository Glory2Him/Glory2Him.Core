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
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Tests.Unit.Brokers.EventEnvelopes
{
    public class EventEnvelopeBrokerTests
    {
        // EventEnvelope declares defaults for SecurityContext, RequestContext and Metadata,
        // but a property initializer does not survive an explicit null in JSON — and
        // EventBroker.DeserializeEnvelope is a bare JsonSerializer.Deserialize, so a stored
        // event carrying "SecurityContext": null really does produce a null.
        //
        // The security gates null-test it themselves, so a write is refused before any
        // conversion happens. The read handlers are where it bites: they short-circuit for a
        // publicly-visible row BEFORE the gate runs, then build a reply envelope through
        // CreateNextAsync — which converts the source envelope and dereferences it.

        [Fact]
        public async Task ShouldTreatANullSecurityContextAsUnauthenticatedOnCreateNextAsync()
        {
            // given
            var eventEnvelopeBroker = new EventEnvelopeBroker();

            var sourceEnvelope = new EventEnvelope<string>
            {
                Content = "source",
                SecurityContext = null,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            // when
            EventEnvelope<string> actualEnvelope =
                await eventEnvelopeBroker.CreateNextAsync(sourceEnvelope, "next");

            // then: fail closed. An envelope carrying no security context is an
            // unauthenticated one — which is exactly what every gate already refuses — not a
            // crash, and not a silently permissive default.
            actualEnvelope.SecurityContext.Should().NotBeNull();
            actualEnvelope.SecurityContext.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldTreatANullRequestContextAsEmptyOnCreateNextAsync()
        {
            // given
            var eventEnvelopeBroker = new EventEnvelopeBroker();

            var sourceEnvelope = new EventEnvelope<string>
            {
                Content = "source",
                RequestContext = null,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            // when
            EventEnvelope<string> actualEnvelope =
                await eventEnvelopeBroker.CreateNextAsync(sourceEnvelope, "next");

            // then
            actualEnvelope.Should().NotBeNull();
        }

        [Fact]
        public async Task ShouldTreatANullMetadataAsEmptyOnCreateNextAsync()
        {
            // given: Metadata carries the causation chain CreateNextAsync reads from the
            // source, so a null here reaches the converter on the same path
            var eventEnvelopeBroker = new EventEnvelopeBroker();

            var sourceEnvelope = new EventEnvelope<string>
            {
                Content = "source",
                Metadata = null
            };

            // when
            EventEnvelope<string> actualEnvelope =
                await eventEnvelopeBroker.CreateNextAsync(sourceEnvelope, "next");

            // then
            actualEnvelope.Should().NotBeNull();
            actualEnvelope.Metadata.Should().NotBeNull();
        }

        // The system identity mint, tested against the REAL broker rather than a stub.
        //
        // This is the authorization decision for the workflow's own dismissal: the foundation
        // admits the system identity in place of the publisher tier precisely where this method
        // minted the context. Every service test mocks IEventEnvelopeBroker, so they pin that the
        // service CALLS this — not that this does anything. Measured: with no test here, setting
        // IsSystemIdentity to false leaves all 4123 unit tests green while the stale-review reset
        // silently reverts to throwing for every editor.
        //
        // Not an exception to "brokers have no logic, so brokers get no unit tests" — this broker
        // has some, and it is the security property itself.
        [Fact]
        public async Task ShouldMintTheSystemIdentityOnCreateSystemAsync()
        {
            // given
            var eventEnvelopeBroker = new EventEnvelopeBroker();

            // when
            EventEnvelope<string> actualEnvelope =
                await eventEnvelopeBroker.CreateSystemAsync(content: "content");

            // then
            actualEnvelope.SecurityContext.IsSystemIdentity.Should().BeTrue(
                because: "the flag IS the authority — the dismissal gate admits the system " +
                    "identity in place of the publisher tier only where this method set it");

            actualEnvelope.SecurityContext.IsAuthenticated.Should().BeTrue(
                because: "a system write is not an anonymous one, and the contribute gate " +
                    "refuses an unauthenticated context before the tier check runs");
        }

        // The other half of the mint, and the reason it is not simply "the caller plus a flag":
        // roles are DROPPED. The flag stands in for the publisher tier by itself, and a context
        // carrying both would look like it was authorised two different ways.
        [Fact]
        public async Task ShouldDropTheCallersRolesOnCreateSystemAsync()
        {
            // given
            var eventEnvelopeBroker = new EventEnvelopeBroker();

            // when
            EventEnvelope<string> actualEnvelope =
                await eventEnvelopeBroker.CreateSystemAsync(content: "content");

            // then
            actualEnvelope.SecurityContext.Roles.Should().BeEmpty(
                because: "the system identity is a claim about provenance, not a role grant");
        }

        // The mint is built ON CreateAsync rather than beside it, so everything that is not the
        // identity is minted exactly as any other envelope's is. A rewrite that hand-rolled the
        // system envelope would lose the causation chain silently.
        [Fact]
        public async Task ShouldCarryTheOrdinaryEnvelopeSectionsOnCreateSystemAsync()
        {
            // given
            var eventEnvelopeBroker = new EventEnvelopeBroker();

            // when
            EventEnvelope<string> actualEnvelope =
                await eventEnvelopeBroker.CreateSystemAsync(content: "content");

            // then
            actualEnvelope.Content.Should().Be("content");
            actualEnvelope.Metadata.Should().NotBeNull();
            actualEnvelope.Metadata.EventId.Should().NotBe(Guid.Empty);
            actualEnvelope.RequestContext.Should().NotBeNull();
        }
    }
}
