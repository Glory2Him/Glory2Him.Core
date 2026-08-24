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
using Glory2Him.Core.Models.Enums;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Glory2Him.WebApp.Tests.Acceptance.Models.Reactions;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Reactions
{
    [Collection(nameof(ApiTestCollection))]
    public partial class ReactionApiTests
    {
        private readonly ApiBroker apiBroker;

        public ReactionApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last.
            this.apiBroker.ActAsSeededAdministrator();
        }

        private int GetRandomNumber() =>
            new IntRange(min: 2, max: 5).GetValue();

        private static DateTimeOffset GetRandomDateTime() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static string GetRandomStringWithLengthOf(int length)
        {
            string result = new MnemonicString(wordCount: 1, wordMinLength: length, wordMaxLength: length).GetValue();

            return result.Length > length ? result.Substring(0, length) : result;
        }

        private static Reaction UpdateReactionWithRandomValues(Reaction inputReaction)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var updatedReaction = CreateRandomReaction();
            updatedReaction.Id = inputReaction.Id;
            updatedReaction.CreatedWhen = inputReaction.CreatedWhen;
            updatedReaction.CreatedBy = inputReaction.CreatedBy;
            updatedReaction.UpdatedWhen = now;
            updatedReaction.IsDeleted = inputReaction.IsDeleted;
            updatedReaction.DeletionReason = inputReaction.DeletionReason;
            updatedReaction.IsPublished = inputReaction.IsPublished;
            updatedReaction.PublishDate = inputReaction.PublishDate;
            updatedReaction.ApprovalStatus = inputReaction.ApprovalStatus;
            updatedReaction.IsApprovedByBypass = inputReaction.IsApprovedByBypass;
            updatedReaction.ApprovedByBypassReason = inputReaction.ApprovedByBypassReason;

            return updatedReaction;
        }

        private async ValueTask<Reaction> PostRandomReactionAsync()
        {
            Reaction randomReaction = CreateRandomReaction();
            Reaction createdReaction = await this.apiBroker.PostReactionAsync(randomReaction);

            return createdReaction;
        }

        private async ValueTask<List<Reaction>> PostRandomReactionsAsync()
        {
            int randomNumber = GetRandomNumber();
            var randomReactions = new List<Reaction>();

            for (int i = 0; i < randomNumber; i++)
            {
                randomReactions.Add(await PostRandomReactionAsync());
            }

            return randomReactions;
        }

        private static Reaction CreateRandomReaction() =>
            CreateRandomReactionFiller().Create();

        private static Filler<Reaction> CreateRandomReactionFiller()
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<Reaction>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                // Name is unique-indexed and capped at 30 characters, and a new reaction must arrive
                // unpublished and in Draft with no bypass — the foundation rejects anything else.
                .OnProperty(reaction => reaction.Name).Use(new Func<string>(GetRandomReactionName))

                // UnicodeEmoji is required and capped at 16 (ReactionService.Validations). Left
                // to the filler it arrives as a sentence and every write is refused, which is how
                // this suite first noticed that a Reaction is not a renamed Tag.
                .OnProperty(reaction => reaction.UnicodeEmoji)
                    .Use(new Func<string>(GetRandomReactionEmoji))
                .OnProperty(reaction => reaction.IsPublished).Use(false)
                .OnProperty(reaction => reaction.PublishDate).Use((DateTimeOffset?)null)
                .OnProperty(reaction => reaction.ApprovalStatus).Use(ApprovalStatus.Draft)
                .OnProperty(reaction => reaction.IsApprovedByBypass).Use(false)
                .OnProperty(reaction => reaction.ApprovedByBypassReason).Use((string)null)
                .OnProperty(reaction => reaction.IsDeleted).Use(false)
                .OnProperty(reaction => reaction.DeletionReason).Use((string)null)
                .OnProperty(reaction => reaction.DeletedBy).Use((string)null)
                .OnProperty(reaction => reaction.DeletedWhen).Use((DateTimeOffset?)null)

                .OnProperty(reaction => reaction.CreatedWhen).Use(now)
                .OnProperty(reaction => reaction.CreatedBy).Use(user)
                .OnProperty(reaction => reaction.UpdatedWhen).Use(now)
                .OnProperty(reaction => reaction.UpdatedBy).Use(user);

            return filler;
        }

        private static string GetRandomReactionName() =>
            Guid.NewGuid().ToString("N").Substring(0, 30);

        /// <summary>
        /// An ASCII stand-in rather than a literal emoji. The field's rules are "present and at
        /// most 16 characters" — nothing validates that the value is really an emoji — so a real
        /// one would put a surrogate pair into every fixture and buy no coverage. A test that
        /// means to exercise the character range should say so and use one.
        /// </summary>
        private static string GetRandomReactionEmoji() =>
            Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}
