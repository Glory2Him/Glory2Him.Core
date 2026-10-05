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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldAnswerEachReactionsCountWithItsNameAndEmojiInTheVocabularysOrderAsync()
        {
            // given: the vocabulary answers Joy before Love, and the counts arrive Love first
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction joy = CreatePublicReaction(name: "Joy");
            Reaction love = CreatePublicReaction(name: "Love");
            var contentItemIds = new List<Guid> { host.ContentItemId };

            SetupPublicContentItemGroups(contentItemIds, host);
            SetupPublicReactions(joy, love);
            SetupWinningSettings(hosts: [host], CreateWinningSetting(host, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [host.GroupId],
                reactionIds: [joy.Id, love.Id],
                CreatePairCount(host, love, count: 3),
                CreatePairCount(host, joy, count: 1));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,

                    Reactions = new List<ContentItemReactionCount>
                    {
                        CreateReactionCount(joy, count: 1),
                        CreateReactionCount(love, count: 3),
                    },

                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: the vocabulary's order, kept rather than re-sorted (Likes.md rule 5a)
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        // A caller who is not signed in.
        private static SecurityContext CreateAnonymousSecurityContext() =>
            new SecurityContext { IsAuthenticated = false };

        // A version anybody may see, answered under its version group. The group is never the
        // version's own id — ContentItemProcessingService.cs:331 and :339 mint them apart — so a
        // summary keyed on the wrong one of the two cannot pass.
        private static PublicContentItemGroup CreatePublicContentItemGroup(
            ContentType contentType = ContentType.Story) =>
            new PublicContentItemGroup(
                ContentItemId: Guid.NewGuid(),
                GroupId: Guid.NewGuid(),
                ContentType: contentType);

        // A reaction of the public vocabulary under its own names.
        private static Reaction CreatePublicReaction(string name) =>
            new Reaction
            {
                Id = Guid.NewGuid(),
                Name = name,
                UnicodeEmoji = GetRandomString(),
            };

        // The host's winning setting. Every <Facet>Allowed switch is on, so a read that asked the
        // write gate's switch instead of the display switch could not tell the two apart only by
        // the switch being off.
        private static EffectiveContentItemSetting CreateWinningSetting(
            PublicContentItemGroup host,
            bool showReactions,
            bool limitReactionsToLoveOnly = false)
        {
            ContentItemSetting contentItemSetting =
                CreateAllowingContentItemSetting(host.ContentType);

            contentItemSetting.ShowReactions = showReactions;
            contentItemSetting.LimitReactionsToLoveOnly = limitReactionsToLoveOnly;

            return new EffectiveContentItemSetting
            {
                ContentItemId = host.ContentItemId,
                ContentItemSetting = contentItemSetting,
            };
        }

        // One row of the grouped count: the host at its group, the reaction at its id.
        private static AssociationPairCount CreatePairCount(
            PublicContentItemGroup host,
            Reaction reaction,
            int count) =>
            new AssociationPairCount
            {
                EntityAEffectiveId = host.GroupId,
                EntityBKeyId = reaction.Id,
                Count = count,
            };

        // The caller's own row on a host, as the caller's-reactions read answers it.
        private static AssociationPairKey CreatePairKey(
            PublicContentItemGroup host,
            Reaction reaction) =>
            new AssociationPairKey
            {
                EntityAEffectiveId = host.GroupId,
                EntityBKeyId = reaction.Id,
            };

        private static ContentItemReactionCount CreateReactionCount(Reaction reaction, int count) =>
            new ContentItemReactionCount
            {
                ReactionId = reaction.Id,
                Name = reaction.Name,
                UnicodeEmoji = reaction.UnicodeEmoji,
                Count = count,
            };

        // Each host's key: its own content type and the id it was supplied by.
        private static List<ContentItemSettingKey> CreateSettingKeysFor(
            IEnumerable<PublicContentItemGroup> hosts) =>
            hosts
                .Select(host => new ContentItemSettingKey
                {
                    ContentType = host.ContentType,
                    ContentItemId = host.ContentItemId,
                })
                .ToList();

        private void SetupPublicContentItemGroups(
            IReadOnlyList<Guid> contentItemIds,
            params PublicContentItemGroup[] hosts) =>
            this.contentItemServiceMock.Setup(service =>
                service.RetrievePublicContentItemGroupsAsync(
                    It.Is(SameIdsAs(contentItemIds)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(hosts.ToList());

        private void SetupPublicReactions(params Reaction[] vocabulary) =>
            this.reactionServiceMock.Setup(service =>
                service.RetrievePublicReactionsAsync(TestContext.Current.CancellationToken))
                    .ReturnsAsync(vocabulary.ToList());

        private void SetupWinningSettings(
            IEnumerable<PublicContentItemGroup> hosts,
            params EffectiveContentItemSetting[] winningSettings) =>
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(CreateSettingKeysFor(hosts))),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(winningSettings.ToList());

        private void SetupReactionCounts(
            IReadOnlyList<Guid> contentItemGroupIds,
            IReadOnlyList<Guid> reactionIds,
            params AssociationPairCount[] pairCounts) =>
            this.associationServiceMock.Setup(service =>
                service.RetrieveContentItemReactionCountsAsync(
                    It.Is(SameIdsAs(contentItemGroupIds)),
                    It.Is(SameIdsAs(reactionIds)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(pairCounts.ToList());

        private void SetupCallerReactions(
            IReadOnlyList<Guid> contentItemGroupIds,
            params AssociationPairKey[] pairKeys) =>
            this.associationServiceMock.Setup(service =>
                service.RetrieveCallerContentItemReactionsAsync(
                    It.Is(SameIdsAs(contentItemGroupIds)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(pairKeys.ToList());

        // the ids by value and in order, whatever collection carries them
        private static Expression<Func<IReadOnlyList<Guid>, bool>> SameIdsAs(
            IReadOnlyList<Guid> expectedIds) =>
            actualIds => actualIds != null && actualIds.SequenceEqual(expectedIds);
    }
}
