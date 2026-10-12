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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Orchestrations.LiveUpdates.Exceptions;

namespace Glory2Him.Core.Services.Orchestrations.LiveUpdates
{
    internal partial class LiveUpdateOrchestrationService
    {
        // Asked before anything is read (§SEC14.8 rule 3, §EVN15 item 10). The name is the one
        // the publisher signed, and it is inside the HMAC, so a receiver verifying under another
        // name refuses every genuine fact it is delivered — and the direction is the request
        // leg, because a fact is published, never replied.
        private async ValueTask ValidateAssociationFactEnvelopeAsync(
            EventEnvelope<Association> envelope,
            string eventName)
        {
            // a signature proves provenance, never that a section is present (§SEC14.6 rule 4)
            if (envelope is null || envelope.Content is null)
            {
                throw new InvalidLiveUpdateOrchestrationException(
                    message: "Invalid live update orchestration fact. " +
                        "The fact envelope and its content are required.");
            }

            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidLiveUpdateOrchestrationException(
                    message: "Invalid live update orchestration fact. " +
                        "Integrity verification failed.");
            }
        }

        // The setting facts' own check, kept apart from the association facts' so that one can
        // change without the other.
        private async ValueTask ValidateContentItemSettingFactEnvelopeAsync(
            EventEnvelope<ContentItemSetting> envelope,
            string eventName)
        {
            bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
                envelope, eventName, EnvelopeDirection.Request);

            if (isSignatureValid is false)
            {
                throw new InvalidLiveUpdateOrchestrationException(
                    message: "Invalid live update orchestration fact. " +
                        "Integrity verification failed.");
            }
        }
    }
}
