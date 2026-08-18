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
        public async Task ShouldRejectAReplyLiftedOntoADifferentAddressAsync()
        {
            // given: the foundation and the processing service both reply over the same content
            // type, so a reply must bind the address name ("ContentItem..." vs
            // "ContentItemProcessing...") and not merely the type — otherwise a foundation reply
            // could be lifted into a processing delivery slot and verify.
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-a"));

            EnvelopeIntegrity integrity = await broker.SignAsync(
                Envelope(), "ContentItemRetrievingById", EnvelopeDirection.Reply);

            EventEnvelope<string> foundationReply = Envelope(integrity: integrity);

            // when
            bool verifiesAsProcessingReply = await broker.VerifyAsync(
                foundationReply, "ContentItemProcessingRetrievingById", EnvelopeDirection.Reply);

            // then
            verifiesAsProcessingReply.Should().BeFalse();
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

        // ── the workflow key: provenance the payload cannot assert (design §16.7.1) ──

        [Fact]
        public async Task ShouldSignASystemIdentityEnvelopeWithTheWorkflowKeyAsync()
        {
            // given
            IEnvelopeIntegrityBroker broker =
                BrokerWith(ActiveKey("key-general"), ActiveWorkflowKey());

            EventEnvelope<string> systemEnvelope = Envelope(isSystemIdentity: true);

            // when
            EnvelopeIntegrity integrity =
                await broker.SignAsync(systemEnvelope, EventName, EnvelopeDirection.Request);

            // then: the general key is active and listed FIRST, so a purpose-blind selection
            // would have picked it.
            integrity.KeyId.Should().Be("key-workflow");
        }

        [Fact]
        public async Task ShouldSignAnOrdinaryEnvelopeWithTheGeneralKeyDespiteAWorkflowKeyAsync()
        {
            // given
            IEnvelopeIntegrityBroker broker =
                BrokerWith(ActiveWorkflowKey(), ActiveKey("key-general"));

            // when: the workflow key is listed FIRST here, so a purpose-blind selection picks it
            EnvelopeIntegrity integrity = await broker.SignAsync(
                Envelope(isSystemIdentity: false), EventName, EnvelopeDirection.Request);

            // then
            integrity.KeyId.Should().Be("key-general");
        }

        [Fact]
        public async Task ShouldRefuseToSignASystemIdentityEnvelopeWithNoWorkflowKeyAsync()
        {
            // given: fails CLOSED. Falling back to the general key would mint an envelope that
            // every receiver then refuses, turning a configuration mistake into a silent outage.
            IEnvelopeIntegrityBroker broker = BrokerWith(ActiveKey("key-general"));

            // when
            Func<Task> signTask = async () =>
                await broker.SignAsync(
                    Envelope(isSystemIdentity: true), EventName, EnvelopeDirection.Request);

            // then
            await signTask.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ShouldVerifyASystemIdentityEnvelopeSignedWithTheWorkflowKeyAsync()
        {
            // given
            IEnvelopeIntegrityBroker broker =
                BrokerWith(ActiveKey("key-general"), ActiveWorkflowKey());

            EnvelopeIntegrity integrity = await broker.SignAsync(
                Envelope(isSystemIdentity: true), EventName, EnvelopeDirection.Request);

            EventEnvelope<string> signed =
                Envelope(isSystemIdentity: true, integrity: integrity);

            // when
            bool isValid =
                await broker.VerifyAsync(signed, EventName, EnvelopeDirection.Request);

            // then
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldRejectASystemIdentityClaimCarriedByANonWorkflowKeyAsync()
        {
            // given: the gate in isolation. The SAME key id and the SAME secret on both sides,
            // so the HMAC matches perfectly and the ONLY difference is the key's declared
            // purpose — this is the one arrangement where a passing signature must still be
            // refused. It models the insider who holds the ordinary publishing key and mints an
            // envelope claiming to be the workflow.
            const string sharedSecret = "a-shared-secret";

            IEnvelopeIntegrityBroker signingBroker = BrokerWith(
                ActiveKey("key-a", EnvelopeSigningPurpose.Workflow, sharedSecret));

            EnvelopeIntegrity integrity = await signingBroker.SignAsync(
                Envelope(isSystemIdentity: true), EventName, EnvelopeDirection.Request);

            IEnvelopeIntegrityBroker verifyingBroker = BrokerWith(
                ActiveKey("key-a", EnvelopeSigningPurpose.General, sharedSecret));

            EventEnvelope<string> signed =
                Envelope(isSystemIdentity: true, integrity: integrity);

            // when
            bool isValid = await verifyingBroker.VerifyAsync(
                signed, EventName, EnvelopeDirection.Request);

            // then
            isValid.Should().BeFalse();

            // and the same envelope IS accepted where that key is the workflow's, which proves
            // the refusal came from the purpose and not from a broken signature.
            bool isValidUnderWorkflowPurpose = await signingBroker.VerifyAsync(
                signed, EventName, EnvelopeDirection.Request);

            isValidUnderWorkflowPurpose.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldRejectWhenTheSystemIdentityFlagWasAddedAsync()
        {
            // given: the headline forgery — an ordinary caller promoting themselves to the
            // workflow by setting one JSON property on an envelope that was signed without it.
            IEnvelopeIntegrityBroker broker =
                BrokerWith(ActiveKey("key-general"), ActiveWorkflowKey());

            EnvelopeIntegrity integrity = await broker.SignAsync(
                Envelope(isSystemIdentity: false), EventName, EnvelopeDirection.Request);

            EventEnvelope<string> tampered =
                Envelope(isSystemIdentity: true, integrity: integrity);

            // when
            bool isValid =
                await broker.VerifyAsync(tampered, EventName, EnvelopeDirection.Request);

            // then
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldAcceptAWorkflowSignedEnvelopeThatMakesNoSystemClaimAsync()
        {
            // given: the rule is one-directional. A system claim demands the workflow key; the
            // workflow key does not demand a system claim. Exercised by verifying under a
            // configuration where the signing key has since been re-declared as the workflow's
            // — same id, same secret — which is the only way an ordinary envelope comes to be
            // workflow-signed. Were the gate written as an equivalence rather than an
            // implication, this ordinary envelope would be refused.
            const string sharedSecret = "a-shared-secret";

            IEnvelopeIntegrityBroker signingBroker = BrokerWith(
                ActiveKey("key-a", EnvelopeSigningPurpose.General, sharedSecret));

            EnvelopeIntegrity integrity = await signingBroker.SignAsync(
                Envelope(isSystemIdentity: false), EventName, EnvelopeDirection.Request);

            IEnvelopeIntegrityBroker verifyingBroker = BrokerWith(
                ActiveKey("key-a", EnvelopeSigningPurpose.Workflow, sharedSecret));

            EventEnvelope<string> signed =
                Envelope(isSystemIdentity: false, integrity: integrity);

            // when
            bool isValid = await verifyingBroker.VerifyAsync(
                signed, EventName, EnvelopeDirection.Request);

            // then
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldTreatAKeyWithNoStatedPurposeAsGeneralAsync()
        {
            // given: the deployed shape. An existing host configures KeyId, Key and ActiveFrom
            // and says nothing about Purpose, so the default is the whole of its behaviour — and
            // a default that landed on Workflow would hand every ordinary publisher the identity
            // the gate exists to protect.
            IEnvelopeIntegrityBroker broker = BrokerWithRawKey(
                keyId: "key-no-purpose",
                secret: "a-development-secret",
                purpose: null);

            EventEnvelope<string> ordinary = Envelope(isSystemIdentity: false);

            // when
            EnvelopeIntegrity integrity =
                await broker.SignAsync(ordinary, EventName, EnvelopeDirection.Request);

            bool isValid = await broker.VerifyAsync(
                Envelope(isSystemIdentity: false, integrity: integrity),
                EventName,
                EnvelopeDirection.Request);

            // then: it signs and verifies ordinary traffic exactly as before
            integrity.KeyId.Should().Be("key-no-purpose");
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldRefuseToSignAsTheWorkflowWhenOnlyAPurposelessKeyExistsAsync()
        {
            // given: the same deployed shape, asked for the one thing it cannot do. This is the
            // failure a host sees when the Workflow key has not been provisioned yet, and it
            // must be a throw at the point of signing rather than an envelope signed with the
            // ordinary key — which every receiver would then refuse, turning a missing setting
            // into a silent, far-away outage.
            IEnvelopeIntegrityBroker broker = BrokerWithRawKey(
                keyId: "key-no-purpose",
                secret: "a-development-secret",
                purpose: null);

            // when
            Func<Task> signTask = async () =>
                await broker.SignAsync(
                    Envelope(isSystemIdentity: true), EventName, EnvelopeDirection.Request);

            // then
            await signTask.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ShouldRejectAForgedClaimThatSimplyNamesTheWorkflowKeyIdAsync()
        {
            // given: why the two secrets MUST differ, demonstrated rather than asserted.
            //
            // KeyId is attacker-controlled — it rides on the envelope and the verifier resolves
            // the key by it. So the forgery is not subtle: mint a system-identity envelope, sign
            // it with the ordinary key you hold, and simply LABEL it with the workflow's key id.
            // The verifier then checks the signature against the workflow's secret.
            //
            // That attempt fails only because the workflow's secret is a different one. Were the
            // two configured with the same value, this envelope would verify and the gate would
            // be a no-op for anyone holding the ordinary key.
            var generalKey = ActiveKey("key-general", EnvelopeSigningPurpose.General, "general-secret");
            var workflowKey = ActiveKey("key-workflow", EnvelopeSigningPurpose.Workflow, "workflow-secret");

            // the attacker's own broker: it holds ONLY the ordinary secret, but declares it as
            // the workflow's so that it will sign a system-identity envelope at all
            IEnvelopeIntegrityBroker attackerBroker = BrokerWith(
                ActiveKey("key-workflow", EnvelopeSigningPurpose.Workflow, "general-secret"));

            EnvelopeIntegrity forged = await attackerBroker.SignAsync(
                Envelope(isSystemIdentity: true), EventName, EnvelopeDirection.Request);

            // it names the workflow's key id, exactly as a genuine one would
            forged.KeyId.Should().Be("key-workflow");

            IEnvelopeIntegrityBroker realBroker = BrokerWith(generalKey, workflowKey);

            // when
            bool isValid = await realBroker.VerifyAsync(
                Envelope(isSystemIdentity: true, integrity: forged),
                EventName,
                EnvelopeDirection.Request);

            // then
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldShowTheGateIsVoidedWhenBothKeysShareASecretAsync()
        {
            // given: the misconfiguration itself, pinned so that its consequence is on record
            // rather than folklore. Same secret on both purposes — the arrangement the settings
            // comments warn against.
            const string sharedSecret = "the-same-secret-for-both";

            IEnvelopeIntegrityBroker attackerBroker = BrokerWith(
                ActiveKey("key-workflow", EnvelopeSigningPurpose.Workflow, sharedSecret));

            EnvelopeIntegrity forged = await attackerBroker.SignAsync(
                Envelope(isSystemIdentity: true), EventName, EnvelopeDirection.Request);

            IEnvelopeIntegrityBroker realBroker = BrokerWith(
                ActiveKey("key-general", EnvelopeSigningPurpose.General, sharedSecret),
                ActiveKey("key-workflow", EnvelopeSigningPurpose.Workflow, sharedSecret));

            // when
            bool isValid = await realBroker.VerifyAsync(
                Envelope(isSystemIdentity: true, integrity: forged),
                EventName,
                EnvelopeDirection.Request);

            // then: it PASSES — holding the ordinary secret was enough to assert the workflow
            // identity. Nothing in the code can detect this; only distinct secrets prevent it.
            isValid.Should().BeTrue();
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private static EventEnvelope<string> Envelope(
            string content = "payload",
            string[] roles = null,
            EnvelopeIntegrity integrity = null,
            bool isSystemIdentity = false) =>
            new EventEnvelope<string>
            {
                Content = content,
                SecurityContext = new SecurityContext
                {
                    SubjectId = "subject-1",
                    IsAuthenticated = true,
                    IsSystemIdentity = isSystemIdentity,
                    Roles = roles ?? new[] { "Reviewer" }
                },
                Metadata = new EventMetadata { EventId = FixedEventId },
                Integrity = integrity
            };

        private static EventEnvelopeSigningKey ActiveKey(
            string keyId,
            EnvelopeSigningPurpose purpose = EnvelopeSigningPurpose.General,
            string secret = "a-development-secret") =>
            Key(
                keyId,
                DateTimeOffset.UtcNow.AddYears(-1),
                DateTimeOffset.UtcNow.AddYears(1),
                secret,
                purpose);

        // The workflow key carries its OWN secret. Sharing one secret across both purposes would
        // make a general-signed and a workflow-signed envelope byte-identical, and every test
        // below would pass without the two keys ever being distinguishable.
        private static EventEnvelopeSigningKey ActiveWorkflowKey(string keyId = "key-workflow") =>
            ActiveKey(keyId, EnvelopeSigningPurpose.Workflow, "a-different-workflow-secret");

        private static EventEnvelopeSigningKey Key(
            string keyId,
            DateTimeOffset activeFrom,
            DateTimeOffset? activeTo,
            string secret = "a-development-secret",
            EnvelopeSigningPurpose purpose = EnvelopeSigningPurpose.General) =>
            new EventEnvelopeSigningKey
            {
                KeyId = keyId,
                Key = secret,
                ActiveFrom = activeFrom,
                ActiveTo = activeTo,
                Purpose = purpose
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
                settings[prefix + "Purpose"] = keys[index].Purpose.ToString();

                if (keys[index].ActiveTo is not null)
                {
                    settings[prefix + "ActiveTo"] = keys[index].ActiveTo.Value.ToString("O");
                }
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }
        // Builds configuration the way a host file or an environment variable does, so that a
        // key with NO Purpose entry at all can be exercised. The typed helpers above always
        // carry one, which is exactly the case a deployed host does not.
        private static IEnvelopeIntegrityBroker BrokerWithRawKey(
            string keyId,
            string secret,
            string purpose)
        {
            var settings = new Dictionary<string, string>
            {
                ["EventEnvelopeSigning:0:KeyId"] = keyId,
                ["EventEnvelopeSigning:0:Key"] = secret,
                ["EventEnvelopeSigning:0:ActiveFrom"] =
                    DateTimeOffset.UtcNow.AddYears(-1).ToString("O"),
            };

            if (purpose is not null)
            {
                settings["EventEnvelopeSigning:0:Purpose"] = purpose;
            }

            return new EnvelopeIntegrityBroker(
                new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        }

    }
}
