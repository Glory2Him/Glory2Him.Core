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

using Xeptions;

namespace Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions
{
    /// <summary>
    /// An override arriving over the event substrate claimed a <c>ContentType</c> that the content
    /// item it names contradicts. The derivation of §12.5.2 business rule 6 ran and disagreed with
    /// the request, so the request is refused.
    ///
    /// <para><b>Why the event path REFUSES where the method path OVERWRITES.</b> Both paths derive
    /// — the same read of the same item decides the answer on both — and on the method path the
    /// derived value simply replaces whatever the caller sent, because the caller's object is a
    /// loose payload nobody has attested to. An event request is not: it arrives inside a signed
    /// envelope whose HMAC covers the content (§14.6 rule 4), and the whole point of that
    /// signature is that no receiver silently edits a part the rules read. Rewriting the field
    /// here would leave the delivery's own record — and the reply built from it — saying something
    /// the publisher never signed. So the derivation still governs; what changes is that a request
    /// contradicting it is rejected rather than quietly corrected.</para>
    ///
    /// <para>The message names neither type. A caller who guessed at an item's id learns only that
    /// their claim was refused, never what the item actually is — the same posture §16.6 takes on
    /// the resolution itself.</para>
    /// </summary>
    public class ContentTypeMismatchContentItemSettingOrchestrationException : Xeption
    {
        public ContentTypeMismatchContentItemSettingOrchestrationException(string message)
            : base(message)
        { }
    }
}
