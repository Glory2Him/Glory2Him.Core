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

using System.Text.RegularExpressions;
using Glory2Him.Core.Brokers.Hashes;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.WebApp.Models.Foundations.Users;
using Microsoft.AspNetCore.Identity;

namespace Glory2Him.WebApp.Data
{
    // DEMO CONTENT, not configuration — which is what separates this from its two siblings.
    // SeedData mints the roles the authorization vocabulary is spelled in, and
    // ContentItemSettingSeedData mints the per-type default every render resolves against; a
    // site missing either is broken. A site missing these 32 rows is simply empty, so this seed
    // is GATED (see Program.cs) and runs only where fabricated contributions belong.
    //
    // It exists because every surface that LISTS content items — the search panel, a moderation
    // queue, "my contributions" — otherwise has to be developed against an empty table or against
    // hand-typed rows that differ per machine, and none of them can be shown to differ by
    // approval status without rows in each one.
    //
    // THE MATRIX. Every (ContentType × ApprovalStatus) pair except Dismissed: 8 types × 4
    // statuses = 32 rows. Dismissed is excluded because it is a state the workflow MOVES a row
    // into once its approval stops counting — nothing is ever born in it, and a seeded one would
    // be a row no code path could have produced.
    //
    // WRITTEN DIRECTLY THROUGH IStorageBroker, for the reason ContentItemSettingSeedData states
    // at length: the audited add path builds its EventEnvelope's SecurityContext from the ambient
    // HttpContext, and there is no HttpContext during host startup. These rows are
    // system-authored, so bypassing the audit trail is correct here rather than a workaround.
    public static class ContentItemSeedData
    {
        // The account the seeded rows are attributed to. RetrieveAllContentItemsAsync widens for
        // `CreatedBy == actorUserId`, so attributing them to the demo contributor is what lets
        // somebody sign in as `user` and see their own Draft, Submitted and Rejected rows — the
        // whole point of seeding the non-approved half of the matrix.
        private const string DemoContributorUserName = "user";

        // The fallback when that account is absent, and it is deliberately an id no account can
        // hold: the rows still land and still render, they simply belong to nobody, so the
        // owner-widened half of the read shows the public set and nothing more.
        private const string SeededBy = "system-seed";

        // Dismissed is excluded — see the matrix note above. Stated as a list rather than derived
        // by excluding one member, so adding a status to the enum does not silently add 8 rows.
        internal static readonly ApprovalStatus[] SeededApprovalStatuses =
        [
            ApprovalStatus.Draft,
            ApprovalStatus.Submitted,
            ApprovalStatus.Approved,
            ApprovalStatus.Rejected
        ];

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IServiceProvider services = scope.ServiceProvider;
            var storageBroker = services.GetRequiredService<IStorageBroker>();
            var hashBroker = services.GetRequiredService<IHashBroker>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            string contributorId = await ResolveContributorIdAsync(userManager);

            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedContentItemsAsync(
                hashBroker,
                contributorId,
                seededWhen: DateTimeOffset.UtcNow);

            foreach (ContentItem contentItem in seedContentItems)
            {
                // Existence by ID, and that is the whole idempotence story — unlike its sibling,
                // which re-inserts a default whose row has been soft-deleted because §12.5.2 rule
                // 5 says a content type must always HAVE a live default. There is no such
                // invariant here: demo content somebody removed is content somebody removed, and
                // putting it back on the next restart would make the delete button look broken.
                bool alreadySeeded = await storageBroker.ExistsContentItemAsync(contentItem.Id);

                if (alreadySeeded is false)
                {
                    await storageBroker.InsertContentItemAsync(contentItem);
                }
            }
        }

        // THE MATRIX ITSELF, separated from the writing of it. INTERNAL rather than private so
        // ContentItemSeedTests can pin the 32 slots, their identifiers and their published state
        // without a database — the same argument SeedData.CoreRoles is internal on. A seed that
        // silently stops covering a pair is invisible until somebody asks why a status never
        // appears on a surface.
        internal static async ValueTask<IReadOnlyList<ContentItem>> BuildSeedContentItemsAsync(
            IHashBroker hashBroker,
            string contributorId,
            DateTimeOffset seededWhen)
        {
            var seedContentItems = new List<ContentItem>();
            int slotIndex = 0;

            foreach (ContentType contentType in Enum.GetValues<ContentType>())
            {
                foreach (ApprovalStatus approvalStatus in SeededApprovalStatuses)
                {
                    seedContentItems.Add(
                        await BuildContentItemAsync(
                            hashBroker,
                            contentType,
                            approvalStatus,
                            contributorId,
                            seededWhen,
                            slotIndex));

                    slotIndex++;
                }
            }

            return seedContentItems;
        }

        private static async Task<string> ResolveContributorIdAsync(UserManager<AppUser> userManager)
        {
            AppUser? demoContributor = await userManager.FindByNameAsync(DemoContributorUserName);

            // ToString() rather than the Guid, and the format matters: CreatedBy is compared as a
            // STRING against what /api/accounts/me returns as userId and what the audit broker
            // reads off the nameidentifier claim, both of which are AppUser.Id.ToString(). A row
            // stamped in any other shape belongs to nobody the read can match.
            return demoContributor?.Id.ToString() ?? SeededBy;
        }

        private static async ValueTask<ContentItem> BuildContentItemAsync(
            IHashBroker hashBroker,
            ContentType contentType,
            ApprovalStatus approvalStatus,
            string contributorId,
            DateTimeOffset seededWhen,
            int slotIndex)
        {
            ContentSpecimen specimen = SpecimenFor(contentType, approvalStatus);

            // Staggered so the list has an order worth looking at: the first slot is the most
            // recent and each one after it is three days older, which gives a $orderby on
            // CreatedWhen something to sort and a feed something that reads like a timeline.
            DateTimeOffset authoredWhen = seededWhen.AddDays(-3 * slotIndex);

            // §14.1 canonical visibility is approved AND published AND past its publish date. An
            // Approved row that is not published is invisible to the public read, so the eight
            // approved rows would prove nothing about the surfaces they exist to exercise.
            bool isPublished = approvalStatus == ApprovalStatus.Approved;

            return new ContentItem
            {
                Id = ContentItemIdFor(contentType, approvalStatus),
                GroupId = GroupIdFor(contentType, approvalStatus),
                Version = 1,
                ContentType = contentType,
                Title = specimen.Title,
                Author = specimen.Author,
                Content = specimen.Content,
                ShareabilityBasis = specimen.ShareabilityBasis,
                SharePermission = specimen.SharePermission,
                ContentHash = await ComputeContentHashAsync(hashBroker, specimen.Content),
                ApprovalStatus = approvalStatus,
                IsApprovedByBypass = false,
                ApprovedByBypassReason = null,
                IsPublished = isPublished,
                PublishDate = isPublished ? authoredWhen : null,
                CreatedBy = contributorId,
                CreatedWhen = authoredWhen,
                UpdatedBy = contributorId,
                UpdatedWhen = authoredWhen,
                IsDeleted = false
            };
        }

        // The duplicate rule (§3.4.2) is scoped per (ContentType, ContentHash), so a seeded row
        // carrying a hash the rule cannot match is a row the rule silently ignores. The hashing
        // itself goes through the same IHashBroker the processing service uses; the NORMALIZATION
        // is restated because ContentItemProcessingService.NormalizeContent is private to it. Two
        // lines is the price of a hash that matches; a copied SHA256 would not have been.
        private static async ValueTask<string> ComputeContentHashAsync(
            IHashBroker hashBroker,
            string content) =>
            await hashBroker.ComputeSha256HashAsync(NormalizeContent(content));

        private static string NormalizeContent(string content) =>
            Regex.Replace(content.Trim(), pattern: @"\s+", replacement: " ")
                .ToLowerInvariant();

        // DETERMINISTIC identifiers, so the seed is idempotent by identity rather than by
        // content: a restart finds the row it wrote last time, and editing a specimen's prose
        // amends nothing rather than inserting a thirty-third row.
        //
        // The fixed prefix marks a seeded row at a glance in a table dump, and the last four hex
        // digits are the (ContentType, ApprovalStatus) pair it stands for — every ContentType
        // member fits in a byte (Topic, the largest, is 200) and every ApprovalStatus in a nibble.
        private static Guid ContentItemIdFor(ContentType contentType, ApprovalStatus approvalStatus) =>
            new($"2f9e6a10-7c41-4d55-9b8e-00000000{(int)contentType:x2}{(int)approvalStatus:x2}");

        // A DIFFERENT base, because a group is a different identity from the item in it. Every
        // seeded row is its own lineage at Version 1: nothing here is a fork, so no two rows may
        // share a GroupId.
        private static Guid GroupIdFor(ContentType contentType, ApprovalStatus approvalStatus) =>
            new($"5d1c4e80-9a23-4f67-8c1d-00000000{(int)contentType:x2}{(int)approvalStatus:x2}");

        private static ContentSpecimen SpecimenFor(
            ContentType contentType,
            ApprovalStatus approvalStatus)
        {
            ContentSpecimen[] specimens = Specimens[contentType];

            return specimens[Array.IndexOf(SeededApprovalStatuses, approvalStatus)];
        }

        // One content item's caller-supplied members — the six a contributor actually sends. The
        // control fields are derived above, exactly as the processing service derives them.
        //
        // TITLE AND AUTHOR FOLLOW THE SEEDED SETTING'S FIELD SHAPING. A Quote's default carries
        // HasTitle false, a Testimony's carries HasAuthor false, and Series and Topic carry
        // neither author — a seeded row that filled a field its type does not render would be a
        // row no contribution form could have produced.
        private sealed class ContentSpecimen
        {
            public string? Title { get; init; }
            public string? Author { get; init; }
            public string Content { get; init; } = string.Empty;
            // The NARROWEST live member, matching the untouched contribute form's own default:
            // the retired Owned is what pre-split rows carry, and a freshly seeded row must not
            // be born under a member the picker no longer offers.
            public ShareabilityBasis ShareabilityBasis { get; init; } =
                ShareabilityBasis.OwnedPermissionGranted;
            public string? SharePermission { get; init; }
        }

        // Four specimens per content type, in the order SeededApprovalStatuses declares: Draft,
        // Submitted, Approved, Rejected.
        //
        // WRITTEN OUT RATHER THAN GENERATED. A card reading "Seeded Devotional 3" tells nobody
        // whether the excerpt truncates, whether a long title wraps, or whether a quote fits its
        // hero card — which is the only reason to have demo rows at all.
        private static readonly Dictionary<ContentType, ContentSpecimen[]> Specimens = new()
        {
            [ContentType.Quote] =
            [
                new ContentSpecimen
                {
                    Author = "Augustine of Hippo",
                    Content =
                        "You have made us for Yourself, and our heart is restless "
                            + "until it rests in You.",
                    ShareabilityBasis = ShareabilityBasis.PublicDomain
                },
                new ContentSpecimen
                {
                    Author = "George Müller",
                    Content =
                        "The beginning of anxiety is the end of faith, and the beginning "
                            + "of true faith is the end of anxiety.",
                    ShareabilityBasis = ShareabilityBasis.PublicDomain
                },
                new ContentSpecimen
                {
                    Author = "D. L. Moody",
                    Content = "Character is what you are in the dark.",
                    ShareabilityBasis = ShareabilityBasis.PublicDomain
                },
                new ContentSpecimen
                {
                    Author = "Hudson Taylor",
                    Content =
                        "God's work done in God's way will never lack God's supply.",
                    ShareabilityBasis = ShareabilityBasis.PublicDomain
                }
            ],

            [ContentType.Story] =
            [
                new ContentSpecimen
                {
                    Title = "The Bread That Was Already On The Table",
                    Author = "Ruth Alderman",
                    Content =
                        "She had prayed for a week about the shopping money and got nothing "
                            + "back but silence. On the Friday a neighbour knocked with a box "
                            + "she had packed by mistake — too much bread, too much of "
                            + "everything. The answer had been in the oven two doors down the "
                            + "whole time."
                },
                new ContentSpecimen
                {
                    Title = "A Locked Door On A Tuesday",
                    Author = "Ruth Alderman",
                    Content =
                        "The interview was at ten and the office was shut. He sat on the step "
                            + "for forty minutes deciding what he thought of God. The man who "
                            + "eventually opened it offered him a different job entirely, and a "
                            + "better one."
                },
                new ContentSpecimen
                {
                    Title = "The Neighbour Who Kept Knocking",
                    Author = "Peter Nkemdirim",
                    Content =
                        "For two years she asked him to come to the carol service, and for two "
                            + "years he said no in the politest possible way. The third year his "
                            + "wife was ill and he could not think of anywhere else to be. He "
                            + "has not missed one since."
                },
                new ContentSpecimen
                {
                    Title = "Twelve Miles Of Rain",
                    Author = "Peter Nkemdirim",
                    Content =
                        "The car died outside Kendal and the phone died with it. What he "
                            + "remembers is not the walk but the lorry driver who turned around "
                            + "after passing him, and would not take a penny for it."
                }
            ],

            [ContentType.Testimony] =
            [
                new ContentSpecimen
                {
                    Title = "I Stopped Running In A Hospital Car Park",
                    Content =
                        "I had been busy for eleven years, and busy is a very good place to "
                            + "hide. It took a waiting room and a diagnosis that turned out to "
                            + "be nothing to make me sit still long enough to be found."
                },
                new ContentSpecimen
                {
                    Title = "Twenty Years Of Sundays, And Then One Wednesday",
                    Content =
                        "I could recite more of the Bible than most people in the room. What I "
                            + "could not do was tell you what any of it had cost me. That "
                            + "changed on an ordinary Wednesday, in an ordinary kitchen, over an "
                            + "ordinary apology I did not want to make."
                },
                new ContentSpecimen
                {
                    Title = "The Year I Could Not Pray",
                    Content =
                        "Nothing dramatic happened. The words simply stopped coming, and I "
                            + "assumed that meant I had stopped belonging. Someone sat with me "
                            + "every week for a year without once telling me to try harder, and "
                            + "somewhere in there the words came back."
                },
                new ContentSpecimen
                {
                    Title = "Found In A Second-Hand Bookshop",
                    Content =
                        "I bought a Bible for forty pence because I liked the cover, and read "
                            + "it for eight months as an argument I intended to win. I am still "
                            + "not sure at what point I stopped arguing."
                }
            ],

            [ContentType.Devotional] =
            [
                new ContentSpecimen
                {
                    Title = "Grace For The Ordinary Tuesday",
                    Author = "Miriam Vale",
                    Content =
                        "Grace is not a one-time event but the daily air the believer breathes. "
                            + "It is given for the Tuesday you will not remember, as freely as "
                            + "for the day everything changed."
                },
                new ContentSpecimen
                {
                    Title = "When The Answer Is Wait",
                    Author = "Miriam Vale",
                    Content =
                        "Waiting is not the absence of an answer; it is one of the three "
                            + "answers there are. The hard part is that it looks exactly like "
                            + "being ignored right up until it does not."
                },
                new ContentSpecimen
                {
                    Title = "Small Obediences",
                    Author = "Samuel Okonkwo",
                    Content =
                        "Almost nobody is asked for the dramatic obedience they rehearse in "
                            + "their head. Almost everybody is asked for the small one in front "
                            + "of them this morning, and that is the one that forms you."
                },
                new ContentSpecimen
                {
                    Title = "The Lamp At Your Feet",
                    Author = "Samuel Okonkwo",
                    Content =
                        "A lamp to my feet is a promise about the next step, not the next "
                            + "decade. It is enough light to walk by and never enough to plan "
                            + "the whole route, and that is deliberate."
                }
            ],

            [ContentType.BibleStudy] =
            [
                new ContentSpecimen
                {
                    Title = "Ephesians 6 — The Armour, Piece By Piece",
                    Author = "Rev. Daniel Hartley",
                    Content =
                        "A six-part walk through Paul's picture of the believer's equipment for "
                            + "the fight. Each piece is examined for what it is, what it is not, "
                            + "and what the first readers would have pictured when they heard it."
                },
                new ContentSpecimen
                {
                    Title = "Psalm 23 — Five Verbs Of A Shepherd",
                    Author = "Rev. Daniel Hartley",
                    Content =
                        "The psalm is usually read for its comfort and rarely for its grammar. "
                            + "Following the five things the shepherd actually does turns a "
                            + "familiar poem into a job description."
                },
                new ContentSpecimen
                {
                    Title = "Romans 8 — No Condemnation, And Why",
                    Author = "Hannah Reid",
                    Content =
                        "Chapter eight opens with a verdict and spends the rest of its length "
                            + "explaining the grounds. This study works backwards from the "
                            + "verdict to the argument that earns it."
                },
                new ContentSpecimen
                {
                    Title = "James 1 — Trials, Wisdom And The Double Mind",
                    Author = "Hannah Reid",
                    Content =
                        "James moves from trials to wisdom to doubt in nine verses, and the "
                            + "joins are easy to miss. Reading them as one argument rather than "
                            + "three topics changes what the passage is asking for."
                }
            ],

            [ContentType.BlogPost] =
            [
                new ContentSpecimen
                {
                    Title = "Why We Read The Hard Passages Out Loud",
                    Author = "Editorial",
                    Content =
                        "A congregation that only ever hears the comfortable verses read aloud "
                            + "learns, without being told, that the rest are an embarrassment. "
                            + "Reading them is the cheapest way to say otherwise."
                },
                new ContentSpecimen
                {
                    Title = "On Keeping A Prayer List You Actually Use",
                    Author = "Editorial",
                    Content =
                        "Most prayer lists fail for the same two reasons: they are too long and "
                            + "they are somewhere you never look. Both are fixable in an "
                            + "afternoon."
                },
                new ContentSpecimen
                {
                    Title = "The Case For Reading Whole Books Of The Bible",
                    Author = "Editorial",
                    Content =
                        "A verse a day is better than nothing, and it is also how a letter "
                            + "written to be read in one sitting becomes a collection of "
                            + "fortune cookies. Sixty uninterrupted minutes with one book is a "
                            + "different experience entirely.",
                    ShareabilityBasis = ShareabilityBasis.PermissionGranted,
                    SharePermission =
                        "Reprinted from the parish magazine with the author's written "
                            + "permission, 4 March 2026."
                },
                new ContentSpecimen
                {
                    Title = "What A Church Notice Board Says About Us",
                    Author = "Editorial",
                    Content =
                        "It is the one page of ours that strangers read most often, and it is "
                            + "usually the page we think about least. Here is what a visitor "
                            + "actually takes from it."
                }
            ],

            // A verse image carries the verse whole — quotation marks, reference and translation
            // in the content, the way the card renders it — and the author is the source. All
            // public domain: scripture is nobody's to own here.
            [ContentType.VerseImage] =
            [
                new ContentSpecimen
                {
                    Author = "The Bible",
                    Content =
                        "“For God so loved the world, that he gave his only Son, that whoever "
                            + "believes in him should not perish but have eternal life.” "
                            + "— John 3:16 ESV",
                    ShareabilityBasis = ShareabilityBasis.PublicDomain
                },
                new ContentSpecimen
                {
                    Author = "The Bible",
                    Content =
                        "“I can do all things through him who strengthens me.” "
                            + "— Philippians 4:13 ESV",
                    ShareabilityBasis = ShareabilityBasis.PublicDomain
                },
                new ContentSpecimen
                {
                    Author = "The Bible",
                    Content =
                        "“Trust in the LORD with all your heart, and do not lean on your own "
                            + "understanding.” — Proverbs 3:5 ESV",
                    ShareabilityBasis = ShareabilityBasis.PublicDomain
                },
                new ContentSpecimen
                {
                    Author = "The Bible",
                    Content =
                        "“Be strong and courageous. Do not be frightened, and do not be "
                            + "dismayed, for the LORD your God is with you wherever you go.” "
                            + "— Joshua 1:9 ESV",
                    ShareabilityBasis = ShareabilityBasis.PublicDomain
                }
            ],

            [ContentType.Series] =
            [
                new ContentSpecimen
                {
                    Title = "Walking Through Ephesians",
                    Content =
                        "Six studies covering the letter in the order it was written to be read."
                },
                new ContentSpecimen
                {
                    Title = "The Parables, One At A Time",
                    Content =
                        "One parable per entry, read for what it says before what it is taken "
                            + "to mean."
                },
                new ContentSpecimen
                {
                    Title = "Advent In Four Weeks",
                    Content =
                        "Four weeks of short readings, one for each candle, gathered for use at "
                            + "home rather than from a lectern."
                },
                new ContentSpecimen
                {
                    Title = "Names Of God",
                    Content =
                        "A name at a time, with the passage it comes from and what the people "
                            + "who first used it were facing."
                }
            ],

            [ContentType.Topic] =
            [
                new ContentSpecimen
                {
                    Title = "Prayer",
                    Content =
                        "Everything gathered here on speaking to God — the habit, the silence, "
                            + "and the arguments about both."
                },
                new ContentSpecimen
                {
                    Title = "Grace",
                    Content =
                        "The unearned favour that the whole story turns on, gathered from every "
                            + "kind of contribution."
                },
                new ContentSpecimen
                {
                    Title = "Suffering",
                    Content =
                        "The contributions that do not tidy it up: lament, endurance, and the "
                            + "questions nobody answers well."
                },
                new ContentSpecimen
                {
                    Title = "Discipleship",
                    Content =
                        "Following, and what it costs on an ordinary week — gathered for people "
                            + "who are somewhere in the middle of it."
                }
            ]
        };
    }
}
