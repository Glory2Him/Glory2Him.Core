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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Attachments;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    /// <summary>
    /// The collection half of §14.7 posture D rule 1 — "authenticated callers see their own" —
    /// where "their own" means the approvals whose ENTITY the caller authored.
    ///
    /// <para>ApprovalService's own tests mock this broker, so they can only assert that the read
    /// DELEGATES. What the answer should be is settled here, against a real broker over a seeded
    /// storage broker.</para>
    /// </summary>
    public partial class AccessBrokerTests
    {
        [Theory]
        [InlineData(EntityType.ContentItem)]
        [InlineData(EntityType.Tag)]
        [InlineData(EntityType.Reaction)]
        [InlineData(EntityType.BibleReference)]
        [InlineData(EntityType.Comment)]
        [InlineData(EntityType.Link)]
        [InlineData(EntityType.Attachment)]
        [InlineData(EntityType.Association)]
        public async Task ShouldKeepOnlyTheApprovalsWhoseEntityTheActorAuthoredAsync(
            EntityType entityType)
        {
            // given: two approvals of the same type, one over the actor's own entity and one over
            // somebody else's. Every EntityType is exercised because each is a separate traversal
            // — a type whose arm is missing drops its author's own work silently.
            string actorUserId = "author-" + GetRandomString();
            Guid ownEntityId = Guid.NewGuid();
            Guid othersEntityId = Guid.NewGuid();

            SetupAuthoredEntity(entityType, ownEntityId, actorUserId);
            SetupAuthoredEntity(entityType, othersEntityId, "somebody-else-" + GetRandomString());

            Approval ownApproval = CreateApprovalOver(entityType, ownEntityId);
            Approval othersApproval = CreateApprovalOver(entityType, othersEntityId);

            IQueryable<Approval> approvals =
                new List<Approval> { ownApproval, othersApproval }.AsQueryable();

            // when
            IQueryable<Approval> actualApprovals =
                await this.accessBroker.FilterApprovalsToEntityAuthorAsync(
                    approvals: approvals,
                    authorUserId: actorUserId,
                    cancellationToken: default);

            // then
            actualApprovals.Should().Equal(new[] { ownApproval },
                because: "an approval belongs to whoever wrote the entity underneath it, and to " +
                    "nobody else");
        }

        [Fact]
        public async Task ShouldNotMatchAnApprovalWhoseEntityTypeDiffersFromTheAuthoredRowAsync()
        {
            // given: the actor authored a Tag, and an approval carries that same id while naming
            // ContentItem. Only the pairing is ownership — an id read without its discriminator
            // is right by accident, and a filter that matched here would hand the actor an
            // approval over content they never wrote.
            string actorUserId = "author-" + GetRandomString();
            Guid sharedEntityId = Guid.NewGuid();

            SetupAuthoredEntity(EntityType.Tag, sharedEntityId, actorUserId);

            Approval mismatchedApproval =
                CreateApprovalOver(EntityType.ContentItem, sharedEntityId);

            IQueryable<Approval> approvals =
                new List<Approval> { mismatchedApproval }.AsQueryable();

            // when
            IQueryable<Approval> actualApprovals =
                await this.accessBroker.FilterApprovalsToEntityAuthorAsync(
                    approvals: approvals,
                    authorUserId: actorUserId,
                    cancellationToken: default);

            // then
            actualApprovals.Should().BeEmpty(
                because: "the EntityType arm is tested alongside the id, so a row matching on id " +
                    "alone is not this actor's");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldMatchNothingWhenTheAuthorCannotBeResolvedAsync(
            string unresolvableUserId)
        {
            // given: an actor whose id came back blank. Blank never matches blank on the
            // single-row gates either — the danger is the opposite reading, where an unresolvable
            // caller falls through to an unfiltered set.
            Guid entityId = Guid.NewGuid();
            SetupAuthoredEntity(EntityType.ContentItem, entityId, createdBy: string.Empty);

            IQueryable<Approval> approvals = new List<Approval>
            {
                CreateApprovalOver(EntityType.ContentItem, entityId)
            }.AsQueryable();

            // when
            IQueryable<Approval> actualApprovals =
                await this.accessBroker.FilterApprovalsToEntityAuthorAsync(
                    approvals: approvals,
                    authorUserId: unresolvableUserId,
                    cancellationToken: default);

            // then
            actualApprovals.Should().BeEmpty(
                because: "an unresolvable actor must match nothing rather than everything");

            // and it failed closed BEFORE reading anything — a caller with no id is refused, not
            // investigated
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldMatchNothingWhenTheActorAuthoredNoEntityAtAllAsync()
        {
            // given: a real actor who has written nothing. "Nothing to show" is an empty answer,
            // not an error, because the caller-facing read proceeds either way.
            SetupAuthoredEntities();

            IQueryable<Approval> approvals = new List<Approval>
            {
                CreateApprovalOver(EntityType.ContentItem, Guid.NewGuid()),
                CreateApprovalOver(EntityType.Tag, Guid.NewGuid())
            }.AsQueryable();

            // when
            IQueryable<Approval> actualApprovals =
                await this.accessBroker.FilterApprovalsToEntityAuthorAsync(
                    approvals: approvals,
                    authorUserId: "author-" + GetRandomString(),
                    cancellationToken: default);

            // then
            actualApprovals.Should().BeEmpty(
                because: "a caller who has authored nothing owns no approvals");
        }

        private static Approval CreateApprovalOver(EntityType entityType, Guid entityId) =>
            new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,

                // What the workflow really writes (§14.6.1). Pinned on every fixture so a filter
                // re-anchored on this column matches nothing and fails loudly.
                CreatedBy = SystemIdentity.UserId,
            };

        // Seeds every approvable table, then adds one row to the one under test. All eight are
        // seeded because the broker composes across all eight on every call — an unseeded mock
        // returns a null queryable and the composition would throw rather than answer.
        private void SetupAuthoredEntity(EntityType entityType, Guid entityId, string createdBy)
        {
            switch (entityType)
            {
                case EntityType.ContentItem:
                    this.contentItems.Add(new ContentItem { Id = entityId, CreatedBy = createdBy });
                    break;

                case EntityType.Tag:
                    this.tags.Add(new Tag { Id = entityId, CreatedBy = createdBy });
                    break;

                case EntityType.Reaction:
                    this.reactions.Add(new Reaction { Id = entityId, CreatedBy = createdBy });
                    break;

                case EntityType.BibleReference:
                    this.bibleReferences.Add(
                        new BibleReference { Id = entityId, CreatedBy = createdBy });

                    break;

                case EntityType.Comment:
                    this.comments.Add(new Comment { Id = entityId, CreatedBy = createdBy });
                    break;

                case EntityType.Link:
                    this.links.Add(new Link { Id = entityId, CreatedBy = createdBy });
                    break;

                case EntityType.Attachment:
                    this.attachments.Add(new Attachment { Id = entityId, CreatedBy = createdBy });
                    break;

                case EntityType.Association:
                    this.associations.Add(
                        new Association { Id = entityId, CreatedBy = createdBy });

                    break;
            }

            SetupAuthoredEntities();
        }

        private readonly List<ContentItem> contentItems = new List<ContentItem>();
        private readonly List<Tag> tags = new List<Tag>();
        private readonly List<Reaction> reactions = new List<Reaction>();
        private readonly List<BibleReference> bibleReferences = new List<BibleReference>();
        private readonly List<Comment> comments = new List<Comment>();
        private readonly List<Link> links = new List<Link>();
        private readonly List<Attachment> attachments = new List<Attachment>();
        private readonly List<Association> associations = new List<Association>();

        private void SetupAuthoredEntities()
        {
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.contentItems.AsQueryable());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.tags.AsQueryable());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllReactionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.reactions.AsQueryable());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllBibleReferencesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.bibleReferences.AsQueryable());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.comments.AsQueryable());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.links.AsQueryable());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAttachmentsAsync())
                    .ReturnsAsync(this.attachments.AsQueryable());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.associations.AsQueryable());
        }
    }
}
