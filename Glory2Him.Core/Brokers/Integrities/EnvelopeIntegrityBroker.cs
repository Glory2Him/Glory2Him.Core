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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Brokers.Integrities
{
    internal class EnvelopeIntegrityBroker : IEnvelopeIntegrityBroker
    {
        private const string Algorithm = "HMACSHA256";
        private const string SigningSection = "EventEnvelopeSigning";

        private readonly IReadOnlyList<EventEnvelopeSigningKey> signingKeys;

        public EnvelopeIntegrityBroker(IConfiguration configuration) =>
            this.signingKeys =
                configuration
                    .GetSection(SigningSection)
                    .Get<List<EventEnvelopeSigningKey>>()
                        ?? new List<EventEnvelopeSigningKey>();

        public ValueTask<EnvelopeIntegrity> SignAsync<T>(
            EventEnvelope<T> envelope,
            string eventName,
            EnvelopeDirection direction)
        {
            DateTimeOffset signedDate = DateTimeOffset.UtcNow;
            EventEnvelopeSigningKey signingKey = SelectActiveSigningKey(signedDate);

            string signature =
                ComputeSignature(envelope, eventName, direction, signingKey.Key);

            var integrity = new EnvelopeIntegrity
            {
                Algorithm = Algorithm,
                KeyId = signingKey.KeyId,
                Signature = signature,
                SignedDate = signedDate
            };

            return new ValueTask<EnvelopeIntegrity>(integrity);
        }

        public ValueTask<bool> VerifyAsync<T>(
            EventEnvelope<T> envelope,
            string expectedEventName,
            EnvelopeDirection expectedDirection)
        {
            EnvelopeIntegrity integrity = envelope?.Integrity;

            // An envelope with no signature, or one naming no key, is unverifiable — and an
            // unverifiable envelope on the event path is exactly what this exists to refuse.
            if (integrity is null
                || string.IsNullOrWhiteSpace(integrity.Signature)
                || string.IsNullOrWhiteSpace(integrity.KeyId))
            {
                return new ValueTask<bool>(false);
            }

            // Resolve the key the envelope names — ignoring its active window, because a historic
            // event must still verify after its key retired. An unknown id fails closed.
            EventEnvelopeSigningKey signingKey =
                this.signingKeys.FirstOrDefault(key => key.KeyId == integrity.KeyId);

            if (signingKey is null)
            {
                return new ValueTask<bool>(false);
            }

            string expectedSignature =
                ComputeSignature(envelope, expectedEventName, expectedDirection, signingKey.Key);

            // Constant-time comparison so a caller cannot learn the correct signature byte by byte
            // from response timing. A length mismatch (a malformed signature) simply returns false.
            bool isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(integrity.Signature));

            return new ValueTask<bool>(isValid);
        }

        // The first key whose window contains "now" signs. A gap between windows, or a set of
        // wholly-expired keys, leaves nothing to sign with — which must throw rather than emit an
        // unsigned envelope, because an unsigned envelope is one this system will later refuse.
        private EventEnvelopeSigningKey SelectActiveSigningKey(DateTimeOffset now)
        {
            EventEnvelopeSigningKey activeKey =
                this.signingKeys.FirstOrDefault(key =>
                    key.ActiveFrom <= now
                        && (key.ActiveTo is null || now < key.ActiveTo));

            if (activeKey is null)
            {
                throw new InvalidOperationException(
                    $"No active event envelope signing key is configured as of {now:O}. " +
                    "Publishing cannot proceed without one.");
            }

            return activeKey;
        }

        // The signed portion, and only it: event name, direction, and the three carried sections
        // plus content. Integrity itself is excluded — it holds the signature, so it cannot be an
        // input to it. Serialized through System.Text.Json in declaration order, which is stable for
        // a fixed type; the verifier rebuilds the identical payload from the deserialized envelope.
        private static string ComputeSignature<T>(
            EventEnvelope<T> envelope,
            string eventName,
            EnvelopeDirection direction,
            string key)
        {
            var signedPayload = new SignedPayload<T>
            {
                EventName = eventName,
                Direction = direction.ToString(),
                Content = envelope.Content,
                SecurityContext = envelope.SecurityContext,
                RequestContext = envelope.RequestContext,
                Metadata = envelope.Metadata
            };

            byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(signedPayload);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] signatureBytes = HMACSHA256.HashData(keyBytes, payloadBytes);

            return Convert.ToHexStringLower(signatureBytes);
        }

        private sealed class SignedPayload<T>
        {
            public string EventName { get; init; }
            public string Direction { get; init; }
            public T Content { get; init; }
            public SecurityContext SecurityContext { get; init; }
            public RequestContext RequestContext { get; init; }
            public EventMetadata Metadata { get; init; }
        }
    }
}
