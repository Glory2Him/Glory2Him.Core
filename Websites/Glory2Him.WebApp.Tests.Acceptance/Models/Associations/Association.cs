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
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.WebApp.Tests.Acceptance.Models.Associations
{
    /// <summary>
    /// What a caller sends to write an association: the two endpoints and nothing else
    /// (§ARC16.8.1). Every other member of the stored row — scope, group ids, content types,
    /// <c>UserId</c> — is derived by the orchestration, so this model does not carry them.
    /// </summary>
    public class Association
    {
        public EntityType EntityAType { get; set; }
        public Guid EntityAKeyId { get; set; }
        public EntityType EntityBType { get; set; }
        public Guid EntityBKeyId { get; set; }
    }
}
