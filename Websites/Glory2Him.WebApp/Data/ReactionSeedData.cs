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

using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Reactions;

namespace Glory2Him.WebApp.Data
{
    // The REACTION VOCABULARY — the choices every Like control offers. CONFIGURATION, not demo
    // content, which is why this seed stands beside ContentItemSettingSeedData rather than the
    // gated ContentItemSeedData: a site whose vocabulary table is empty has a Like control that
    // can offer nothing, in production exactly as on a laptop.
    //
    // Seeded APPROVED AND PUBLISHED, because the choices surface reads only the approved rows —
    // a reaction the moderators have not accepted must not be offerable, and these five are
    // accepted by being shipped. A new reaction contributed at runtime still walks the workflow.
    //
    // WRITTEN DIRECTLY THROUGH IStorageBroker for the reason its siblings state at length: there
    // is no HttpContext at host startup, so the audited add path is unreachable. Idempotent by
    // DETERMINISTIC IDs, the ContentItemSeedData posture: a restart amends nothing, and a
    // vocabulary row somebody deliberately removed stays removed rather than resurrecting to
    // make the delete look broken. Name is unique in storage, so the id check doubles as the
    // collision guard.
    public static class ReactionSeedData
    {
        private const string SeededBy = "system-seed";

        // The five the design's picker carries. Love is the member LimitReactionsToLoveOnly
        // narrows to (§6.5) — renaming it is a decision with a consequence, and the projection
        // that marks it says so at the match site.
        private static readonly (Guid Id, string Name, string UnicodeEmoji)[] Vocabulary =
        [
            (new Guid("7b2d90c1-4e6a-4f3b-8d21-000000000001"), "Amen", "👍"),
            (new Guid("7b2d90c1-4e6a-4f3b-8d21-000000000002"), "Love", "❤️"),
            (new Guid("7b2d90c1-4e6a-4f3b-8d21-000000000003"), "Joy", "😄"),
            (new Guid("7b2d90c1-4e6a-4f3b-8d21-000000000004"), "Moved", "😢"),
            (new Guid("7b2d90c1-4e6a-4f3b-8d21-000000000005"), "Praying", "🙏")
        ];

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IServiceProvider services = scope.ServiceProvider;
            var storageBroker = services.GetRequiredService<IStorageBroker>();
            DateTimeOffset seededWhen = DateTimeOffset.UtcNow;

            foreach ((Guid id, string name, string unicodeEmoji) in Vocabulary)
            {
                bool alreadySeeded = await storageBroker.ExistsReactionAsync(id);

                if (alreadySeeded is false)
                {
                    await storageBroker.InsertReactionAsync(new Reaction
                    {
                        Id = id,
                        Name = name,
                        UnicodeEmoji = unicodeEmoji,
                        ApprovalStatus = ApprovalStatus.Approved,
                        IsApprovedByBypass = false,
                        ApprovedByBypassReason = null,
                        IsPublished = true,
                        PublishDate = seededWhen,
                        IsDeleted = false,
                        CreatedBy = SeededBy,
                        CreatedWhen = seededWhen,
                        UpdatedBy = SeededBy,
                        UpdatedWhen = seededWhen
                    });
                }
            }
        }
    }
}
