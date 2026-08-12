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
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Tests.Unit.Brokers.Integrities
{
    public class EnvelopeIntegrityBrokerTests
    {
        private const string EventName = "ContentItemAdding";

        // A stable envelope, so a signature over one instance verifies against a fresh instance
        // carrying the same fields plus the integrity — the way a real receiver rebuilds it from
        // deserialized JSON.
        private static readonly Guid FixedEventId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Fact]
        public async Task ShouldSignThenVerifyTheSameEnvelopeAsync()
        {
            // given
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));
            EventEnvelope<string> unsigned = Envelope();

            // when
            EnvelopeIntegrity integrity =
                await broker.SignAsync(unsigned, EventName, EnvelopeDirection.Request);

            EventEnvelope<string> signed = Envelope(integrity: integrity);

            bool isValid =
                await broker.VerifyAsync(signed, EventName, EnvelopeDirection.Request);

            // then
            integrity.KeyId.Should().Be("key-a");
            integrity.Algorithm.Should().Be("HMACSHA256");
            integrity.Signature.Should().NotBeNullOrWhiteSpace();
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldRejectWhenTheContentWasTamperedAsync()
        {
            // given
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));

            EnvelopeIntegrity integrity = await broker.SignAsync(
                Envelope(content: "the-original-payload"),
                EventName,
                EnvelopeDirection.Request);

            EventEnvelope<string> tampered =
                Envelope(content: "a-swapped-payload", integrity: integrity);

            // when
            bool isValid =
                await broker.VerifyAsync(tampered, EventName, EnvelopeDirection.Request);

            // then
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldRejectWhenARoleWasAddedAsync()
        {
            // given: the headline attack — a forged Admin. Signing the security context is what
            // makes an added role detectable.
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));

            EnvelopeIntegrity integrity = await broker.SignAsync(
                Envelope(roles: new[] { "Reviewer" }),
                EventName,
                EnvelopeDirection.Request);

            EventEnvelope<string> tampered =
                Envelope(roles: new[] { "Reviewer", "Admin" }, integrity: integrity);

            // when
            bool isValid =
                await broker.VerifyAsync(tampered, EventName, EnvelopeDirection.Request);

            // then
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldRejectWhenTheEventNameDiffersAsync()
        {
            // given: a signed soft-delete must not verify against the hard-delete handler — they
            // share an address, so only the composed name tells them apart.
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));

            EnvelopeIntegrity integrity = await broker.SignAsync(
                Envelope(), "ContentItemRemoved", EnvelopeDirection.Request);

            EventEnvelope<string> signed = Envelope(integrity: integrity);

            // when
            bool isValid = await broker.VerifyAsync(
                signed, "ContentItemHardRemoved", EnvelopeDirection.Request);

            // then
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldRejectWhenTheDirectionDiffersAsync()
        {
            // given: a signed reply must not verify as a request
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));

            EnvelopeIntegrity integrity = await broker.SignAsync(
                Envelope(), EventName, EnvelopeDirection.Reply);

            EventEnvelope<string> signed = Envelope(integrity: integrity);

            // when
            bool isValid =
                await broker.VerifyAsync(signed, EventName, EnvelopeDirection.Request);

            // then
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldSignThenVerifyAReplyAsync()
        {
            // given: the fire-and-observe path — a handler's reply is signed with the Reply
            // direction, and the publisher reading it back verifies it under the same direction.
            // It must verify as a reply and never as a request, so a reply can never be lifted
            // back onto a request address and believed as an inbound command.
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));

            EnvelopeIntegrity integrity = await broker.SignAsync(
                Envelope(), EventName, EnvelopeDirection.Reply);

            EventEnvelope<string> signedReply = Envelope(integrity: integrity);

            // when
            bool verifiesAsReply =
                await broker.VerifyAsync(signedReply, EventName, EnvelopeDirection.Reply);

            bool verifiesAsRequest =
                await broker.VerifyAsync(signedReply, EventName, EnvelopeDirection.Request);

            // then
            verifiesAsReply.Should().BeTrue();
            verifiesAsRequest.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldRejectWhenTheKeyIdIsUnknownAsync()
        {
            // given
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));

            EnvelopeIntegrity integrity =
                await broker.SignAsync(Envelope(), EventName, EnvelopeDirection.Request);

            var reKeyed = new EnvelopeIntegrity
            {
                Algorithm = integrity.Algorithm,
                KeyId = "a-key-that-is-not-configured",
                Signature = integrity.Signature,
                SignedDate = integrity.SignedDate
            };

            EventEnvelope<string> signed = Envelope(integrity: reKeyed);

            // when
            bool isValid =
                await broker.VerifyAsync(signed, EventName, EnvelopeDirection.Request);

            // then
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldRejectWhenIntegrityIsNullAsync()
        {
            // given: the tripwire — an authenticated identity arriving over the event path with no
            // signature at all is refused
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));
            EventEnvelope<string> unsigned = Envelope(integrity: null);

            // when
            bool isValid =
                await broker.VerifyAsync(unsigned, EventName, EnvelopeDirection.Request);

            // then
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldNotConsultTheAlgorithmFieldWhenVerifyingAsync()
        {
            // given: the alg=none lesson — the verifier uses its own algorithm, so rewriting the
            // envelope's Algorithm to "none" cannot downgrade the check
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));

            EnvelopeIntegrity integrity =
                await broker.SignAsync(Envelope(), EventName, EnvelopeDirection.Request);

            var forgedAlgorithm = new EnvelopeIntegrity
            {
                Algorithm = "none",
                KeyId = integrity.KeyId,
                Signature = integrity.Signature,
                SignedDate = integrity.SignedDate
            };

            EventEnvelope<string> signed = Envelope(integrity: forgedAlgorithm);

            // when
            bool isValid =
                await broker.VerifyAsync(signed, EventName, EnvelopeDirection.Request);

            // then: still valid, because Algorithm was never read
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldStillVerifyAnEnvelopeSignedByANowRetiredKeyAsync()
        {
            // given: the whole point of KeyId — sign under key-a while it is the active one, then
            // rotate so key-a is expired and key-b signs new events. The historic event must still
            // verify, or replay breaks.
            DateTimeOffset now = DateTimeOffset.UtcNow;

            IEnvelopeIntegrityBroker atSigningTime =
                BrokerWith(Key("key-a", now.AddDays(-2), now.AddDays(2), secret: "secret-a"));

            EnvelopeIntegrity integrity = await atSigningTime.SignAsync(
                Envelope(), EventName, EnvelopeDirection.Request);

            IEnvelopeIntegrityBroker afterRotation = BrokerWith(
                Key("key-a", now.AddDays(-2), now.AddDays(-1), secret: "secret-a"),
                Key("key-b", now.AddDays(-1), null, secret: "secret-b"));

            EventEnvelope<string> signed = Envelope(integrity: integrity);

            // when
            bool isValid =
                await afterRotation.VerifyAsync(signed, EventName, EnvelopeDirection.Request);

            // then
            integrity.KeyId.Should().Be("key-a");
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldSignWithTheFirstActiveKeyAsync()
        {
            // given: two keys whose windows both contain now; the first wins
            DateTimeOffset now = DateTimeOffset.UtcNow;

            IEnvelopeIntegrityBroker broker = BrokerWith(
                Key("key-a", now.AddDays(-1), null, secret: "secret-a"),
                Key("key-b", now.AddDays(-1), null, secret: "secret-b"));

            // when
            EnvelopeIntegrity integrity =
                await broker.SignAsync(Envelope(), EventName, EnvelopeDirection.Request);

            // then
            integrity.KeyId.Should().Be("key-a");
        }

        [Fact]
        public async Task ShouldThrowWhenNoKeyIsActiveForSigningAsync()
        {
            // given: every configured key has already expired — signing must fail closed rather
            // than emit an unsigned envelope
            DateTimeOffset now = DateTimeOffset.UtcNow;

            IEnvelopeIntegrityBroker broker =
                BrokerWith(Key("key-a", now.AddDays(-10), now.AddDays(-1), secret: "secret-a"));

            // when
            Func<Task> signTask = async () =>
                await broker.SignAsync(Envelope(), EventName, EnvelopeDirection.Request);

            // then
            await signTask.Should().ThrowAsync<InvalidOperationException>();
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private static EventEnvelope<string> Envelope(
            string content = "payload",
            string[] roles = null,
            EnvelopeIntegrity integrity = null) =>
            new EventEnvelope<string>
            {
                Content = content,
                SecurityContext = new SecurityContext
                {
                    SubjectId = "subject-1",
                    IsAuthenticated = true,
                    Roles = roles ?? new[] { "Reviewer" }
                },
                Metadata = new EventMetadata { EventId = FixedEventId },
                Integrity = integrity
            };

        private static EventEnvelopeSigningKey ActiveKey(string keyId) =>
            Key(keyId, DateTimeOffset.UtcNow.AddYears(-1), DateTimeOffset.UtcNow.AddYears(1));

        private static EventEnvelopeSigningKey Key(
            string keyId,
            DateTimeOffset activeFrom,
            DateTimeOffset? activeTo,
            string secret = "a-development-secret") =>
            new EventEnvelopeSigningKey
            {
                KeyId = keyId,
                Key = secret,
                ActiveFrom = activeFrom,
                ActiveTo = activeTo
            };

        private static IEnvelopeIntegrityBroker BrokerWith(params EventEnvelopeSigningKey[] keys) =>
            new EnvelopeIntegrityBroker(BuildConfiguration(keys));

        private static IConfiguration BuildConfiguration(EventEnvelopeSigningKey[] keys)
        {
            var settings = new Dictionary<string, string>();

            for (int index = 0; index < keys.Length; index++)
            {
                string prefix = $"EventEnvelopeSigning:{index}:";
                settings[prefix + "KeyId"] = keys[index].KeyId;
                settings[prefix + "Key"] = keys[index].Key;
                settings[prefix + "ActiveFrom"] = keys[index].ActiveFrom.ToString("O");

                if (keys[index].ActiveTo is not null)
                {
                    settings[prefix + "ActiveTo"] = keys[index].ActiveTo.Value.ToString("O");
                }
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }
    }
}
