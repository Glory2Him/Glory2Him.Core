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
using System.Threading;
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

        [Fact]
        public async Task ShouldAnswerAnItemNobodyReactedToWithNoReactionsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");
            Reaction joy = CreatePublicReaction(name: "Joy");
            var contentItemIds = new List<Guid> { host.ContentItemId };

            SetupPublicContentItemGroups(contentItemIds, host);
            SetupPublicReactions(love, joy);
            SetupWinningSettings(hosts: [host], CreateWinningSetting(host, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [host.GroupId],
                reactionIds: [love.Id, joy.Id]);

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,
                    Reactions = new List<ContentItemReactionCount>(),
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: answered, with no reaction entered at zero
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task ShouldLeaveOutAnIdThatIsNotPubliclyVisibleAsync()
        {
            // given: two ids the public groups read does not answer — one naming an item nobody
            // may see and one naming nothing look the same from here — beside one it does
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup visibleHost = CreatePublicContentItemGroup();
            Guid notVisibleContentItemId = Guid.NewGuid();
            Guid nonexistentContentItemId = Guid.NewGuid();
            Reaction love = CreatePublicReaction(name: "Love");

            var contentItemIds = new List<Guid>
            {
                notVisibleContentItemId,
                visibleHost.ContentItemId,
                nonexistentContentItemId,
            };

            SetupPublicContentItemGroups(contentItemIds, visibleHost);
            SetupPublicReactions(love);

            SetupWinningSettings(
                hosts: [visibleHost],
                CreateWinningSetting(visibleHost, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [visibleHost.GroupId],
                reactionIds: [love.Id],
                CreatePairCount(visibleHost, love, count: 2));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = visibleHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 2) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: absent — not an empty summary, and not a refusal (§SEC14.5 rule 4)
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldAnswerNoReactionsForAnItemThatHidesThemAsync(
            bool hidingHostResolvesASetting)
        {
            // given: one item whose winning setting shows reactions, beside one whose winning
            // setting hides them — or whose key the settings read leaves out of its answer
            // because it resolves no row, which must never mean "show"
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup showingHost = CreatePublicContentItemGroup();
            PublicContentItemGroup hidingHost = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");

            var contentItemIds = new List<Guid>
            {
                showingHost.ContentItemId,
                hidingHost.ContentItemId,
            };

            var winningSettings = new List<EffectiveContentItemSetting>
            {
                CreateWinningSetting(showingHost, showReactions: true),
            };

            if (hidingHostResolvesASetting)
            {
                winningSettings.Add(CreateWinningSetting(hidingHost, showReactions: false));
            }

            SetupPublicContentItemGroups(contentItemIds, showingHost, hidingHost);
            SetupPublicReactions(love);
            SetupWinningSettings(hosts: [showingHost, hidingHost], winningSettings.ToArray());

            // The count read answers a row for the hiding item's group as well, so its empty
            // answer cannot rest on that group being left out of the count alone.
            SetupReactionCounts(
                contentItemGroupIds: [showingHost.GroupId],
                reactionIds: [love.Id],
                CreatePairCount(showingHost, love, count: 2),
                CreatePairCount(hidingHost, love, count: 5));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = showingHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 2) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
                new ContentItemReactionSummary
                {
                    ContentItemId = hidingHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount>(),
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: answered, with no counts, and its group not handed to the count
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());

            this.associationServiceMock.Verify(service =>
                service.RetrieveContentItemReactionCountsAsync(
                    It.Is(SameIdsAs(new List<Guid> { showingHost.GroupId })),
                    It.Is(SameIdsAs(new List<Guid> { love.Id })),
                    TestContext.Current.CancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task ShouldCountOnlyThePublicVocabularyAsync()
        {
            // given: the item also carries counts on a reaction outside the public vocabulary —
            // one withdrawn from it since readers gave it — which the vocabulary read leaves out
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");
            Reaction joy = CreatePublicReaction(name: "Joy");
            var contentItemIds = new List<Guid> { host.ContentItemId };

            SetupPublicContentItemGroups(contentItemIds, host);
            SetupPublicReactions(love, joy);
            SetupWinningSettings(hosts: [host], CreateWinningSetting(host, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [host.GroupId],
                reactionIds: [love.Id, joy.Id],
                CreatePairCount(host, love, count: 4));

            // when
            await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                contentItemIds,
                TestContext.Current.CancellationToken);

            // then: the count is handed the vocabulary's ids and no other
            this.associationServiceMock.Verify(service =>
                service.RetrieveContentItemReactionCountsAsync(
                    It.Is(SameIdsAs(new List<Guid> { host.GroupId })),
                    It.Is(SameIdsAs(new List<Guid> { love.Id, joy.Id })),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldNameTheCallersOwnReactionEvenWhenItIsNotCountedAsync(bool isCounted)
        {
            // given: a signed-in reader who holds Love on the item, counted already or not yet —
            // a reaction awaiting review under a tightened tier is not counted (§ARC16.8)
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId: GetRandomString());
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction joy = CreatePublicReaction(name: "Joy");
            Reaction love = CreatePublicReaction(name: "Love");
            var contentItemIds = new List<Guid> { host.ContentItemId };

            AssociationPairCount[] pairCounts = isCounted
                ? [CreatePairCount(host, love, count: 3)]
                : [];

            SetupPublicContentItemGroups(contentItemIds, host);
            SetupPublicReactions(joy, love);
            SetupWinningSettings(hosts: [host], CreateWinningSetting(host, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [host.GroupId],
                reactionIds: [joy.Id, love.Id],
                pairCounts);

            SetupCallerReactions(
                contentItemGroupIds: [host.GroupId],
                CreatePairKey(host, love));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,

                    Reactions = pairCounts
                        .Select(pairCount => CreateReactionCount(love, pairCount.Count))
                        .ToList(),

                    ViewerReactionId = love.Id,
                    ViewerReactionName = love.Name,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: pressed, whether or not the number has moved
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task ShouldAnswerNoViewerReactionForAnAnonymousCallerAsync()
        {
            // given: an anonymous caller. The caller's-reactions read would name Love on the first
            // item were it asked, so an answer that asked it could not come back unchanged.
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup firstHost = CreatePublicContentItemGroup();
            PublicContentItemGroup secondHost = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");

            var contentItemIds = new List<Guid>
            {
                firstHost.ContentItemId,
                secondHost.ContentItemId,
            };

            SetupPublicContentItemGroups(contentItemIds, firstHost, secondHost);
            SetupPublicReactions(love);

            SetupWinningSettings(
                hosts: [firstHost, secondHost],
                CreateWinningSetting(firstHost, showReactions: true),
                CreateWinningSetting(secondHost, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [firstHost.GroupId, secondHost.GroupId],
                reactionIds: [love.Id],
                CreatePairCount(firstHost, love, count: 1));

            SetupCallerReactions(
                contentItemGroupIds: [firstHost.GroupId, secondHost.GroupId],
                CreatePairKey(firstHost, love));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = firstHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 1) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
                new ContentItemReactionSummary
                {
                    ContentItemId = secondHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount>(),
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: every viewer member null, and the caller's-reactions read never asked
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());

            this.associationServiceMock.Verify(service =>
                service.RetrieveCallerContentItemReactionsAsync(
                    It.IsAny<IReadOnlyList<Guid>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldAnswerNoViewerReactionForAReactionOutsideTheVocabularyAsync()
        {
            // given: a signed-in reader whose own row names a reaction withdrawn from the
            // vocabulary since they gave it. The card offers only the vocabulary, so it could not
            // mark that reaction anyway (AssociationOrchestrationService.md, Deviations 2).
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId: GetRandomString());
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");
            Reaction withdrawnReaction = CreatePublicReaction(name: "Withdrawn");
            var contentItemIds = new List<Guid> { host.ContentItemId };

            SetupPublicContentItemGroups(contentItemIds, host);
            SetupPublicReactions(love);
            SetupWinningSettings(hosts: [host], CreateWinningSetting(host, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [host.GroupId],
                reactionIds: [love.Id],
                CreatePairCount(host, love, count: 2));

            SetupCallerReactions(
                contentItemGroupIds: [host.GroupId],
                CreatePairKey(host, withdrawnReaction));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 2) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: both viewer members null, not the id alone
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task ShouldAnswerADuplicatedIdOnceWithItsGroupsCountAsync()
        {
            // given: one id supplied twice
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");

            var contentItemIds = new List<Guid>
            {
                host.ContentItemId,
                host.ContentItemId,
            };

            SetupPublicContentItemGroups(contentItemIds: [host.ContentItemId], host);
            SetupPublicReactions(love);
            SetupWinningSettings(hosts: [host], CreateWinningSetting(host, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [host.GroupId],
                reactionIds: [love.Id],
                CreatePairCount(host, love, count: 6));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 6) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: answered once, echoing itself, with its group's count
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task ShouldCountTheSameForEveryCallerAsync()
        {
            // given: the same items asked about by an administrator who holds Joy on the first,
            // and then by an anonymous caller. The second item hides reactions, so a count that
            // widened for the administrator would show.
            PublicContentItemGroup showingHost = CreatePublicContentItemGroup();
            PublicContentItemGroup hidingHost = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");
            Reaction joy = CreatePublicReaction(name: "Joy");

            var contentItemIds = new List<Guid>
            {
                showingHost.ContentItemId,
                hidingHost.ContentItemId,
            };

            List<ContentItemSettingKey> settingKeys =
                CreateSettingKeysFor([showingHost, hidingHost]);

            SetupPublicContentItemGroups(contentItemIds, showingHost, hidingHost);
            SetupPublicReactions(love, joy);

            SetupWinningSettings(
                hosts: [showingHost, hidingHost],
                CreateWinningSetting(showingHost, showReactions: true),
                CreateWinningSetting(hidingHost, showReactions: false));

            SetupReactionCounts(
                contentItemGroupIds: [showingHost.GroupId],
                reactionIds: [love.Id, joy.Id],
                CreatePairCount(showingHost, love, count: 2),
                CreatePairCount(showingHost, joy, count: 1));

            SetupCallerReactions(
                contentItemGroupIds: [showingHost.GroupId, hidingHost.GroupId],
                CreatePairKey(showingHost, joy));

            var expectedReactions = new List<ContentItemReactionCount>
            {
                CreateReactionCount(love, count: 2),
                CreateReactionCount(joy, count: 1),
            };

            var expectedAdministratorSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = showingHost.ContentItemId,
                    Reactions = expectedReactions,
                    ViewerReactionId = joy.Id,
                    ViewerReactionName = joy.Name,
                },
                new ContentItemReactionSummary
                {
                    ContentItemId = hidingHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount>(),
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            var expectedAnonymousSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = showingHost.ContentItemId,
                    Reactions = expectedReactions,
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
                new ContentItemReactionSummary
                {
                    ContentItemId = hidingHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount>(),
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            this.ambientSecurityContext =
                CreateReaderSecurityContext(readerUserId: GetRandomString(), Roles.Administrators);

            IReadOnlyList<ContentItemReactionSummary> administratorSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            this.ambientSecurityContext = CreateAnonymousSecurityContext();

            IReadOnlyList<ContentItemReactionSummary> anonymousSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: the same counts, and only the viewer members differ
            administratorSummaries.Should().BeEquivalentTo(
                expectedAdministratorSummaries,
                options => options.WithStrictOrdering());

            anonymousSummaries.Should().BeEquivalentTo(
                expectedAnonymousSummaries,
                options => options.WithStrictOrdering());

            // and the hosts, the vocabulary, the settings and the counts were asked for
            // identically, once for each caller
            this.contentItemServiceMock.Verify(service =>
                service.RetrievePublicContentItemGroupsAsync(
                    It.Is(SameIdsAs(contentItemIds)),
                    TestContext.Current.CancellationToken),
                Times.Exactly(2));

            this.reactionServiceMock.Verify(service =>
                service.RetrievePublicReactionsAsync(TestContext.Current.CancellationToken),
                Times.Exactly(2));

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(settingKeys)),
                    TestContext.Current.CancellationToken),
                Times.Exactly(2));

            this.associationServiceMock.Verify(service =>
                service.RetrieveContentItemReactionCountsAsync(
                    It.Is(SameIdsAs(new List<Guid> { showingHost.GroupId })),
                    It.Is(SameIdsAs(new List<Guid> { love.Id, joy.Id })),
                    TestContext.Current.CancellationToken),
                Times.Exactly(2));

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAskForTheCountsOnceForEveryCountedHostAsync()
        {
            // given: several items whose winning settings show reactions
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup firstHost = CreatePublicContentItemGroup();
            PublicContentItemGroup secondHost = CreatePublicContentItemGroup();
            PublicContentItemGroup thirdHost = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");
            Reaction joy = CreatePublicReaction(name: "Joy");

            var contentItemIds = new List<Guid>
            {
                firstHost.ContentItemId,
                secondHost.ContentItemId,
                thirdHost.ContentItemId,
            };

            var contentItemGroupIds = new List<Guid>
            {
                firstHost.GroupId,
                secondHost.GroupId,
                thirdHost.GroupId,
            };

            SetupPublicContentItemGroups(contentItemIds, firstHost, secondHost, thirdHost);
            SetupPublicReactions(love, joy);

            SetupWinningSettings(
                hosts: [firstHost, secondHost, thirdHost],
                CreateWinningSetting(firstHost, showReactions: true),
                CreateWinningSetting(secondHost, showReactions: true),
                CreateWinningSetting(thirdHost, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds,
                reactionIds: [love.Id, joy.Id],
                CreatePairCount(firstHost, love, count: 1),
                CreatePairCount(thirdHost, joy, count: 2));

            // when
            await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                contentItemIds,
                TestContext.Current.CancellationToken);

            // then: one round trip over every counted host's group and the vocabulary's ids
            this.associationServiceMock.Verify(service =>
                service.RetrieveContentItemReactionCountsAsync(
                    It.Is(SameIdsAs(contentItemGroupIds)),
                    It.Is(SameIdsAs(new List<Guid> { love.Id, joy.Id })),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAskForTheSettingsOnceKeyedOnEachHostsContentTypeAndSuppliedIdAsync()
        {
            // given: two items of different content types, set explicitly — the filler's default
            // would give both one type, and a lookup that ignored it would still pass — whose
            // winning settings differ: the first's shows reactions and the second's hides them
            this.ambientSecurityContext = CreateAnonymousSecurityContext();

            PublicContentItemGroup testimonyHost =
                CreatePublicContentItemGroup(contentType: ContentType.Testimony);

            PublicContentItemGroup devotionalHost =
                CreatePublicContentItemGroup(contentType: ContentType.Devotional);

            Reaction love = CreatePublicReaction(name: "Love");

            var contentItemIds = new List<Guid>
            {
                testimonyHost.ContentItemId,
                devotionalHost.ContentItemId,
            };

            var expectedSettingKeys = new List<ContentItemSettingKey>
            {
                new ContentItemSettingKey
                {
                    ContentType = ContentType.Testimony,
                    ContentItemId = testimonyHost.ContentItemId,
                },
                new ContentItemSettingKey
                {
                    ContentType = ContentType.Devotional,
                    ContentItemId = devotionalHost.ContentItemId,
                },
            };

            SetupPublicContentItemGroups(contentItemIds, testimonyHost, devotionalHost);
            SetupPublicReactions(love);

            SetupWinningSettings(
                hosts: [testimonyHost, devotionalHost],
                CreateWinningSetting(testimonyHost, showReactions: true),
                CreateWinningSetting(devotionalHost, showReactions: false));

            SetupReactionCounts(
                contentItemGroupIds: [testimonyHost.GroupId],
                reactionIds: [love.Id],
                CreatePairCount(testimonyHost, love, count: 3));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = testimonyHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 3) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
                new ContentItemReactionSummary
                {
                    ContentItemId = devotionalHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount>(),
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.Is(SameSettingKeysAs(expectedSettingKeys)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAskForTheVocabularyOnceAsync()
        {
            // given: several visible items
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup firstHost = CreatePublicContentItemGroup();
            PublicContentItemGroup secondHost = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");

            var contentItemIds = new List<Guid>
            {
                firstHost.ContentItemId,
                secondHost.ContentItemId,
            };

            SetupPublicContentItemGroups(contentItemIds, firstHost, secondHost);
            SetupPublicReactions(love);

            SetupWinningSettings(
                hosts: [firstHost, secondHost],
                CreateWinningSetting(firstHost, showReactions: true),
                CreateWinningSetting(secondHost, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [firstHost.GroupId, secondHost.GroupId],
                reactionIds: [love.Id],
                CreatePairCount(secondHost, love, count: 1));

            // when
            await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                contentItemIds,
                TestContext.Current.CancellationToken);

            // then: one vocabulary for the whole page, not one per item
            this.reactionServiceMock.Verify(service =>
                service.RetrievePublicReactionsAsync(TestContext.Current.CancellationToken),
                Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAskForThePublicGroupsOnceAsync()
        {
            // given: several ids, one of them supplied twice, and one the read does not answer
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup firstHost = CreatePublicContentItemGroup();
            PublicContentItemGroup secondHost = CreatePublicContentItemGroup();
            Guid notVisibleContentItemId = Guid.NewGuid();
            Reaction love = CreatePublicReaction(name: "Love");

            var contentItemIds = new List<Guid>
            {
                firstHost.ContentItemId,
                notVisibleContentItemId,
                firstHost.ContentItemId,
                secondHost.ContentItemId,
            };

            var distinctContentItemIds = new List<Guid>
            {
                firstHost.ContentItemId,
                notVisibleContentItemId,
                secondHost.ContentItemId,
            };

            SetupPublicContentItemGroups(distinctContentItemIds, firstHost, secondHost);
            SetupPublicReactions(love);

            SetupWinningSettings(
                hosts: [firstHost, secondHost],
                CreateWinningSetting(firstHost, showReactions: true),
                CreateWinningSetting(secondHost, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [firstHost.GroupId, secondHost.GroupId],
                reactionIds: [love.Id]);

            // when
            await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                contentItemIds,
                TestContext.Current.CancellationToken);

            // then: one round trip, over every distinct id
            this.contentItemServiceMock.Verify(service =>
                service.RetrievePublicContentItemGroupsAsync(
                    It.Is(SameIdsAs(distinctContentItemIds)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldLeaveOutAReactionNobodyGaveAsync()
        {
            // given: an item given Love and no other reaction of the vocabulary
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction joy = CreatePublicReaction(name: "Joy");
            Reaction love = CreatePublicReaction(name: "Love");
            Reaction peace = CreatePublicReaction(name: "Peace");
            var contentItemIds = new List<Guid> { host.ContentItemId };

            SetupPublicContentItemGroups(contentItemIds, host);
            SetupPublicReactions(joy, love, peace);
            SetupWinningSettings(hosts: [host], CreateWinningSetting(host, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [host.GroupId],
                reactionIds: [joy.Id, love.Id, peace.Id],
                CreatePairCount(host, love, count: 1));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 1) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: Love alone — a reaction with a zero count does not appear
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task ShouldCountEveryReactionOnAnItemLimitedToLoveAsync()
        {
            // given: an item whose winning setting limits reactions to Love, given Love twice and
            // Joy once before it was narrowed
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");
            Reaction joy = CreatePublicReaction(name: "Joy");
            var contentItemIds = new List<Guid> { host.ContentItemId };

            SetupPublicContentItemGroups(contentItemIds, host);
            SetupPublicReactions(love, joy);

            SetupWinningSettings(
                hosts: [host],
                CreateWinningSetting(host, showReactions: true, limitReactionsToLoveOnly: true));

            SetupReactionCounts(
                contentItemGroupIds: [host.GroupId],
                reactionIds: [love.Id, joy.Id],
                CreatePairCount(host, love, count: 2),
                CreatePairCount(host, joy, count: 1));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,

                    Reactions = new List<ContentItemReactionCount>
                    {
                        CreateReactionCount(love, count: 2),
                        CreateReactionCount(joy, count: 1),
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

            // then: Joy's count beside Love's — the narrowing governs the write, not history
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task ShouldAnswerEachItemsOwnCountsUnderItsSuppliedIdAsync()
        {
            // given: two items of different groups, each supplied by an id that is not its group
            // id, Love counted on the first's group and Joy on the second's
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup firstHost = CreatePublicContentItemGroup();
            PublicContentItemGroup secondHost = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");
            Reaction joy = CreatePublicReaction(name: "Joy");

            var contentItemIds = new List<Guid>
            {
                firstHost.ContentItemId,
                secondHost.ContentItemId,
            };

            SetupPublicContentItemGroups(contentItemIds, firstHost, secondHost);
            SetupPublicReactions(love, joy);

            SetupWinningSettings(
                hosts: [firstHost, secondHost],
                CreateWinningSetting(firstHost, showReactions: true),
                CreateWinningSetting(secondHost, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [firstHost.GroupId, secondHost.GroupId],
                reactionIds: [love.Id, joy.Id],
                CreatePairCount(secondHost, joy, count: 4),
                CreatePairCount(firstHost, love, count: 7));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = firstHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 7) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
                new ContentItemReactionSummary
                {
                    ContentItemId = secondHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(joy, count: 4) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: each group's count, echoed onto the id that item was supplied by
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task ShouldLeaveOutACountForAReactionOutsideTheVocabularyAsync()
        {
            // given: the count read answers a row for a reaction outside the public vocabulary
            this.ambientSecurityContext = CreateAnonymousSecurityContext();
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");
            Reaction withdrawnReaction = CreatePublicReaction(name: "Withdrawn");
            var contentItemIds = new List<Guid> { host.ContentItemId };

            SetupPublicContentItemGroups(contentItemIds, host);
            SetupPublicReactions(love);
            SetupWinningSettings(hosts: [host], CreateWinningSetting(host, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [host.GroupId],
                reactionIds: [love.Id],
                CreatePairCount(host, withdrawnReaction, count: 4),
                CreatePairCount(host, love, count: 2));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 2) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: only the vocabulary's reactions are counted
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldAskForTheCallersReactionsOnceForEveryAnsweredHostAsync(
            bool identityCarriesAUserId)
        {
            // given: a signed-in caller — one whose identity carries no user id is still signed
            // in, and is asked for so the read can log the misconfiguration (AssociationService.md
            // §4 rule 2) — and several answered items, one of which hides reactions, beside an id
            // the public groups read does not answer
            string readerUserId = identityCarriesAUserId ? GetRandomString() : null;
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);
            PublicContentItemGroup showingHost = CreatePublicContentItemGroup();
            PublicContentItemGroup hidingHost = CreatePublicContentItemGroup();
            Guid notVisibleContentItemId = Guid.NewGuid();
            Reaction love = CreatePublicReaction(name: "Love");

            var contentItemIds = new List<Guid>
            {
                showingHost.ContentItemId,
                notVisibleContentItemId,
                hidingHost.ContentItemId,
            };

            var answeredContentItemGroupIds = new List<Guid>
            {
                showingHost.GroupId,
                hidingHost.GroupId,
            };

            SetupPublicContentItemGroups(contentItemIds, showingHost, hidingHost);
            SetupPublicReactions(love);

            SetupWinningSettings(
                hosts: [showingHost, hidingHost],
                CreateWinningSetting(showingHost, showReactions: true),
                CreateWinningSetting(hidingHost, showReactions: false));

            SetupReactionCounts(
                contentItemGroupIds: [showingHost.GroupId],
                reactionIds: [love.Id]);

            SetupCallerReactions(answeredContentItemGroupIds);

            // when
            await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                contentItemIds,
                TestContext.Current.CancellationToken);

            // then: one round trip over every answered host's group, counted or not
            this.associationServiceMock.Verify(service =>
                service.RetrieveCallerContentItemReactionsAsync(
                    It.Is(SameIdsAs(answeredContentItemGroupIds)),
                    TestContext.Current.CancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task ShouldAnswerNoViewerReactionOnAnItemTheCallerHasNotReactedToAsync()
        {
            // given: a signed-in reader who holds Love on one answered item and nothing on another
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId: GetRandomString());
            PublicContentItemGroup reactedHost = CreatePublicContentItemGroup();
            PublicContentItemGroup unreactedHost = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");

            var contentItemIds = new List<Guid>
            {
                reactedHost.ContentItemId,
                unreactedHost.ContentItemId,
            };

            SetupPublicContentItemGroups(contentItemIds, reactedHost, unreactedHost);
            SetupPublicReactions(love);

            SetupWinningSettings(
                hosts: [reactedHost, unreactedHost],
                CreateWinningSetting(reactedHost, showReactions: true),
                CreateWinningSetting(unreactedHost, showReactions: true));

            SetupReactionCounts(
                contentItemGroupIds: [reactedHost.GroupId, unreactedHost.GroupId],
                reactionIds: [love.Id],
                CreatePairCount(reactedHost, love, count: 1),
                CreatePairCount(unreactedHost, love, count: 3));

            SetupCallerReactions(
                contentItemGroupIds: [reactedHost.GroupId, unreactedHost.GroupId],
                CreatePairKey(reactedHost, love));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = reactedHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 1) },
                    ViewerReactionId = love.Id,
                    ViewerReactionName = love.Name,
                },
                new ContentItemReactionSummary
                {
                    ContentItemId = unreactedHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 3) },
                    ViewerReactionId = null,
                    ViewerReactionName = null,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: the second item's viewer members are null, though others gave it Love
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task ShouldNameTheCallersOwnReactionOnAnItemThatHidesReactionsAsync()
        {
            // given: a signed-in reader who holds Love on an item whose winning setting hides
            // reactions
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId: GetRandomString());
            PublicContentItemGroup hidingHost = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");
            var contentItemIds = new List<Guid> { hidingHost.ContentItemId };

            SetupPublicContentItemGroups(contentItemIds, hidingHost);
            SetupPublicReactions(love);

            SetupWinningSettings(
                hosts: [hidingHost],
                CreateWinningSetting(hidingHost, showReactions: false));

            SetupReactionCounts(
                contentItemGroupIds: [],
                reactionIds: [love.Id]);

            SetupCallerReactions(
                contentItemGroupIds: [hidingHost.GroupId],
                CreatePairKey(hidingHost, love));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = hidingHost.ContentItemId,
                    Reactions = new List<ContentItemReactionCount>(),
                    ViewerReactionId = love.Id,
                    ViewerReactionName = love.Name,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: no counts, and the reader's own reaction still named
            actualSummaries.Should().BeEquivalentTo(
                expectedSummaries,
                options => options.WithStrictOrdering());
        }

        [Fact]
        public async Task ShouldNameTheCallersOwnReactionUnderTheSuppliedIdAsync()
        {
            // given: a signed-in reader who holds Love on an item supplied by an id that is not its
            // group id. Their row is keyed on the group.
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId: GetRandomString());
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
                CreatePairCount(host, love, count: 1));

            SetupCallerReactions(
                contentItemGroupIds: [host.GroupId],
                CreatePairKey(host, love));

            var expectedSummaries = new List<ContentItemReactionSummary>
            {
                new ContentItemReactionSummary
                {
                    ContentItemId = host.ContentItemId,
                    Reactions = new List<ContentItemReactionCount> { CreateReactionCount(love, count: 1) },
                    ViewerReactionId = love.Id,
                    ViewerReactionName = love.Name,
                },
            };

            // when
            IReadOnlyList<ContentItemReactionSummary> actualSummaries =
                await this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    contentItemIds,
                    TestContext.Current.CancellationToken);

            // then: the group's row, mapped back onto the id it was supplied by
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
