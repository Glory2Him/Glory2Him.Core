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
using FluentAssertions;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Tests.Integration.Brokers;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Tags
{
    /// <summary>
    /// Proves the tag find-or-add's read by name against a real catalogue
    /// (<c>Backend/Foundations/TagService.TagSuggestions.md §3</c> rule 9; §ARC12.2.1 rule 6).
    ///
    /// <para>The condition is written in <c>TagService</c> as a query-shaping function and
    /// handed to the broker. Its unit tests execute that function over LINQ-to-Objects; what
    /// only SQL answers is whether the name compares under the column's collation, which ignores
    /// letter case, and whether the condition and its projection to <c>TagNameMatch</c>
    /// translate and carry the columns the member branches on. Each test asks the member itself
    /// and asserts its answer, so the projection is proven through the answer it decides and
    /// never apart from it.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class TagNarrowReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly List<Tag> seededTags;

        public TagNarrowReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededTags = new List<Tag>();
        }

        [Fact]
        public async Task ShouldFindATagWhoseNameDiffersOnlyInLetterCaseAsync()
        {
            // given
            Tag storedTag = CreateTag(name: "Grace");
            storedTag.ApprovalStatus = ApprovalStatus.Submitted;
            await SeedAsync(storedTag);
            Tag inputTag = CreateTag(name: "grace");

            var expectedTagNameResult = new TagNameResult
            {
                Id = storedTag.Id,
                Outcome = TagNameOutcome.Found
            };

            ITagService tagService = CreateTagService();

            // when
            TagNameResult actualTagNameResult =
                await tagService.FindOrAddTagAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            // then
            actualTagNameResult.Should().BeEquivalentTo(expectedTagNameResult);
        }

        /// <summary>
        /// The soft-deleted case proves the condition has no <c>IsDeleted</c> term and that
        /// <c>TagNameMatch</c> carries <c>IsDeleted</c>; the rejected case, that it carries
        /// <c>ApprovalStatus</c>. A projection that left either column at its default would read
        /// the row as a live <c>Draft</c> and answer <c>Found</c>.
        /// </summary>
        [Theory]
        [InlineData("Mercy", ApprovalStatus.Rejected, false)]
        [InlineData("Hope", ApprovalStatus.Submitted, true)]
        public async Task ShouldWithholdARejectedOrSoftDeletedTagFoundByItsNameAsync(
            string name,
            ApprovalStatus approvalStatus,
            bool isDeleted)
        {
            // given
            Tag storedTag = CreateTag(name);
            storedTag.ApprovalStatus = approvalStatus;
            storedTag.IsDeleted = isDeleted;
            await SeedAsync(storedTag);
            Tag inputTag = CreateTag(name);

            var expectedTagNameResult = new TagNameResult
            {
                Id = storedTag.Id,
                Outcome = TagNameOutcome.Withheld
            };

            ITagService tagService = CreateTagService();

            // when
            TagNameResult actualTagNameResult =
                await tagService.FindOrAddTagAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            // then
            actualTagNameResult.Should().BeEquivalentTo(expectedTagNameResult);
        }

        // A real service over the real storage broker, so the condition under test is the one
        // the service authors rather than a copy of it. The envelope it mints carries an
        // authenticated caller holding neither ReadOnly nor Tag-ReadOnly, so the contribution
        // gate admits it (§3 rule 2); every other broker is a mock.
        private ITagService CreateTagService()
        {
            var eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();

            eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Tag>()))
                    .ReturnsAsync((Tag content) =>
                        new EventEnvelope<Tag>
                        {
                            Content = content,
                            SecurityContext = new SecurityContext
                            {
                                IsAuthenticated = true,
                                Roles = Array.Empty<string>()
                            },
                            Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                        });

            return new TagService(
                storageBroker: this.broker.StorageBroker,
                dateTimeBroker: new Mock<IDateTimeBroker>().Object,
                identifierBroker: new Mock<IIdentifierBroker>().Object,
                eventBroker: new Mock<IEventBroker>().Object,
                eventEnvelopeBroker: eventEnvelopeBrokerMock.Object,
                securityAuditBroker: new Mock<ISecurityAuditBroker>().Object,
                accessBroker: new Mock<IAccessBroker>().Object,
                envelopeIntegrityBroker: new Mock<IEnvelopeIntegrityBroker>().Object,
                loggingBroker: new Mock<ILoggingBroker>().Object);
        }

        private static Tag CreateTag(string name)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new Tag
            {
                Id = Guid.NewGuid(),
                Name = name,
                ApprovalStatus = ApprovalStatus.Draft,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                DeletedBy = null,
                DeletedWhen = null,
            };
        }

        private async Task SeedAsync(params Tag[] tags)
        {
            await this.broker.SeedAsync(tags);
            this.seededTags.AddRange(tags);
        }

        public void Dispose() =>
            this.broker.ClearAsync(this.seededTags).AsTask().GetAwaiter().GetResult();
    }
}
