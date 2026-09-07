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
    /// The content item an override names could not be resolved — it does not exist, or it is not
    /// visible to this caller. Raised in place of the endpoint service's own not-found, which has
    /// already been logged there, so the caller learns that the endpoint failed and nothing about
    /// which of the two reasons applied.
    /// </summary>
    public class NotFoundContentItemSettingOrchestrationException : Xeption
    {
        public NotFoundContentItemSettingOrchestrationException(string message)
            : base(message)
        { }
    }
}
