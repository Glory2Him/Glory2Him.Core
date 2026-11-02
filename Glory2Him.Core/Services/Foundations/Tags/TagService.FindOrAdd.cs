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
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    internal partial class TagService
    {
        public async ValueTask<TagNameResult> FindOrAddTagAsync(
            Tag tag,
            CancellationToken cancellationToken = default)
        {
            await this.eventEnvelopeBroker.CreateAsync(content: tag);

            TagNameMatch? tagNameMatch = await SelectTagNameMatchAsync(
                name: tag.Name,
                cancellationToken: cancellationToken);

            return new TagNameResult
            {
                Id = tagNameMatch!.Id,
                Outcome = TagNameOutcome.Found
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
