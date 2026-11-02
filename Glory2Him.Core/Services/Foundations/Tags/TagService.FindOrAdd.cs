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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    internal partial class TagService
    {
        public async ValueTask<TagNameResult> FindOrAddTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default)
        {
            EventEnvelope<Tag> envelope =
                await this.eventEnvelopeBroker.CreateAsync(content: tag);

            TagNameMatch? tagNameMatch = await SelectTagNameMatchAsync(
                name: tag.Name,
                cancellationToken: cancellationToken);

            if (tagNameMatch is not null)
            {
                return await AnswerTagNameMatchAsync(tagNameMatch);
            }

            // no row holds the name: the handed tag is added as the add adds one, through its
            // own write body, so its gate, mint, validations, audit, write and Tag-Added fact are
            // written once (§3 rule 6)
            Tag addedTag = await DoAddTagAsync(
                tag: tag,
                inboundEnvelope: envelope,
                cancellationToken: cancellationToken);

            return new TagNameResult
            {
                Id = addedTag.Id,
                Outcome = TagNameOutcome.Added
            };
        }

        // A rejected name is never paired, and a soft-deleted tag is withheld until #1362's
        // undelete replaces that: either is answered Withheld, the true reason logged and never
        // answered, so the caller learns nothing of another reader's row (§3 rule 5; §SEC14.5
        // rules 6 and 7)
        private async ValueTask<TagNameResult> AnswerTagNameMatchAsync(TagNameMatch tagNameMatch)
        {
            bool isRejected = tagNameMatch.ApprovalStatus == ApprovalStatus.Rejected;

            if (isRejected is false && tagNameMatch.IsDeleted is false)
            {
                return new TagNameResult
                {
                    Id = tagNameMatch.Id,
                    Outcome = TagNameOutcome.Found
                };
            }

            string withheldReason = (isRejected, tagNameMatch.IsDeleted) switch
            {
                (true, true) => "rejected and soft-deleted",
                (true, false) => "rejected",
                _ => "soft-deleted"
            };

            await this.loggingBroker.LogInformationAsync(
                message: $"Tag name match withheld. Tag {tagNameMatch.Id} is {withheldReason}; " +
                    "it was withheld from the suggestion, which pairs nothing with it, and the " +
                    "caller is not told so.");

            return new TagNameResult
            {
                Id = tagNameMatch.Id,
                Outcome = TagNameOutcome.Withheld
            };
        }

        private async ValueTask<TagNameMatch?> SelectTagNameMatchAsync(
            string name,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<TagNameMatch> tagNameMatches =
                await this.storageBroker.SelectTagsAsync(
                    query: tags => tags
                        .Where(tag => tag.Name == name)
                        .Select(tag => new TagNameMatch
                        {
                            Id = tag.Id,
                            ApprovalStatus = tag.ApprovalStatus,
                            IsDeleted = tag.IsDeleted
                        }),
                    cancellationToken: cancellationToken);

            return tagNameMatches.FirstOrDefault();
        }
    }
}
