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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldExcludeAnAssociationWhoseEndpointIsSoftDeletedOnRetrieveAllAsync()
        {
            // given: an Administrators caller, so nothing about THEIR posture drops the row.
            // §SEC14.5 rule 3 refuses a soft-deleted row to every caller, Administrators
            // included, so the ContentItem foundation's own collection read hands back a set
            // without it — and the composite drops the pairing hanging off it (§SEC14.3 rule 3).
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            ContentItem liveContentItem = CreateEndpointContentItem();
            ContentItem softDeletedContentItem = CreateEndpointContentItem();
            Tag sharedTag = CreateEndpointTag();

            Association survivingAssociation =
                CreateStoredAssociation(liveContentItem, sharedTag);

            Association associationOnDeletedEndpoint =
                CreateStoredAssociation(softDeletedContentItem, sharedTag);

            SetupVisibleAssociations(survivingAssociation, associationOnDeletedEndpoint);

            // the soft-deleted item is absent from its own entity's read, which is the whole
            // of what "soft-deleted" means to every caller of it
            SetupEndpointCollectionReads(
                contentItems: new[] { liveContentItem },
                tags: new[] { sharedTag });

            // when
            IQueryable<Association> actualQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            List<Association> actualAssociations = actualQuery.ToList();

            // then: dropped from the set rather than reported — §SEC14.5 rule 4 — and the
            // answer reveals no count of what was dropped
            actualAssociations.Should().ContainSingle()
                .Which.Id.Should().Be(survivingAssociation.Id);
        }

        [Fact]
        public async Task ShouldExcludeAnAssociationWhoseEndpointIsNotVisibleToTheCallerOnRetrieveAllAsync()
        {
            // given: an ordinary authenticated caller. The far endpoint is dropped on the B
            // side, which canonical ordering makes a separate branch rather than a mirror of A.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem sharedContentItem = CreateEndpointContentItem();
            Tag visibleTag = CreateEndpointTag();
            Tag tagOutsideTheCallersRead = CreateEndpointTag();

            Association survivingAssociation =
                CreateStoredAssociation(sharedContentItem, visibleTag);

            Association associationOnHiddenEndpoint =
                CreateStoredAssociation(sharedContentItem, tagOutsideTheCallersRead);

            SetupVisibleAssociations(survivingAssociation, associationOnHiddenEndpoint);

            SetupEndpointCollectionReads(
                contentItems: new[] { sharedContentItem },
                tags: new[] { visibleTag });

            // when
            IQueryable<Association> actualQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            List<Association> actualAssociations = actualQuery.ToList();

            // then
            actualAssociations.Should().ContainSingle()
                .Which.Id.Should().Be(survivingAssociation.Id);
        }

        /// <summary>
        /// Criterion 2's positive case: <b>the same association row</b> is in the set for a
        /// caller the endpoint's own read admits to a <c>Submitted</c> item, and absent for one
        /// it does not admit.
        ///
        /// <para><b>What this asserts is the pass-through, and only that.</b> The composite reads
        /// no role and cannot — §SEC14.3's endpoint term takes its caller clause from the
        /// endpoint's own read, which is why a moderator keeps the pairing on the item they
        /// moderate. The role→admission mapping below is therefore stated by fiat in the theory
        /// data rather than proven here; it belongs to <c>ContentItemService</c> and is proven in
        /// that service's own tests. Reading this test as evidence for it would be reading it as
        /// more than it is.</para>
        ///
        /// <para><b>What it adds over its two neighbours</b>, which also pair a surviving row
        /// with a dropped one: they use two DIFFERENT rows, so a composite keying on something
        /// about the row itself would satisfy them. This one holds the row fixed and varies only
        /// the endpoint set, so the answer can come from nowhere else.</para>
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldIncludeAnAssociationOnASubmittedContentItemForItsReviewerOnRetrieveAllAsync(
            bool theContentItemReadAdmitsThisCaller)
        {
            // given: one row, one evaluator, and the endpoint read answering two different ways.
            // The caller is a reviewer in the first case and holds nothing in the second, which
            // is §SEC14.7 posture A rule 4's admission for a Submitted row — recorded here as the
            // scenario, not asserted by this test.
            this.ambientSecurityContext = theContentItemReadAdmitsThisCaller
                ? CreateAuthenticatedSecurityContext(Roles.ReviewersFor(EntityType.ContentItem))
                : CreateAuthenticatedSecurityContext();

            ContentItem submittedContentItemUnderReview = CreateEndpointContentItem();
            Tag sharedTag = CreateEndpointTag();

            Association associationOnTheSubmittedItem =
                CreateStoredAssociation(submittedContentItemUnderReview, sharedTag);

            SetupVisibleAssociations(associationOnTheSubmittedItem);

            SetupEndpointCollectionReads(
                contentItems: theContentItemReadAdmitsThisCaller
                    ? new[] { submittedContentItemUnderReview }
                    : Array.Empty<ContentItem>(),
                tags: new[] { sharedTag });

            // when
            IQueryable<Association> actualQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            List<Association> actualAssociations = actualQuery.ToList();

            // then
            if (theContentItemReadAdmitsThisCaller)
            {
                actualAssociations.Should().ContainSingle()
                    .Which.Id.Should().Be(associationOnTheSubmittedItem.Id);
            }
            else
            {
                actualAssociations.Should().BeEmpty();
            }
        }

        [Fact]
        public async Task ShouldReturnAnUnenumeratedQueryableOnRetrieveAllAsync()
        {
            // given: the composite composes INTO the query rather than resolving anything above
            // it (§ARC12.2.1 rule 7, §ARC16.8), so nothing is materialised on the way out — which
            // is what keeps #318's route composable and its SQL capped.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association storedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            var enumerationCounter = new EnumerationCounter();

            this.associationServiceMock.Setup(service =>
                service.RetrieveAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new EnumerationTrackingQueryable<Association>(
                        new[] { storedAssociation }.AsQueryable(),
                        enumerationCounter));

            SetupEndpointCollectionReads(
                contentItems: new[] { contentItemEndpoint },
                tags: new[] { tagEndpoint });

            // when
            IQueryable<Association> actualQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then: not one row has been pulled at the point the member returns
            enumerationCounter.Count.Should().Be(0);

            // and the queryable it handed back is still live — enumerating it here is the
            // caller's act, and it answers
            actualQuery.ToList().Should().ContainSingle()
                .Which.Id.Should().Be(storedAssociation.Id);

            enumerationCounter.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ShouldNotIssueAnAdditionalEndpointReadOnRetrieveAllAsync()
        {
            // given: three rows, so an implementation that resolved endpoints per association
            // would show up as three reads rather than one. Non-functional constraint 2: the
            // composite composes into the association query, so the endpoint cost is flat in N.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem firstContentItem = CreateEndpointContentItem();
            ContentItem secondContentItem = CreateEndpointContentItem();
            ContentItem thirdContentItem = CreateEndpointContentItem();
            Tag sharedTag = CreateEndpointTag();

            SetupVisibleAssociations(
                CreateStoredAssociation(firstContentItem, sharedTag),
                CreateStoredAssociation(secondContentItem, sharedTag),
                CreateStoredAssociation(thirdContentItem, sharedTag));

            SetupEndpointCollectionReads(
                contentItems: new[] { firstContentItem, secondContentItem, thirdContentItem },
                tags: new[] { sharedTag });

            // when
            IQueryable<Association> actualQuery =
                await this.associationOrchestrationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            List<Association> actualAssociations = actualQuery.ToList();

            // then
            actualAssociations.Should().HaveCount(3);

            this.associationServiceMock.Verify(service =>
                service.RetrieveAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.tagServiceMock.Verify(service =>
                service.RetrieveAllTagsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveAllReactionsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.bibleReferenceServiceMock.Verify(service =>
                service.RetrieveAllBibleReferencesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.commentServiceMock.Verify(service =>
                service.RetrieveAllCommentsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            // nothing resolved an endpoint one row at a time
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
            this.commentServiceMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // ── fixtures ──────────────────────────────────────────────────────────────────

        private static ContentItem CreateEndpointContentItem(
            ContentType contentType = ContentType.Story) =>
            new ContentItem
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),

                // set explicitly: every random ContentItem shares the default content type, so a
                // fixture that leaves it alone proves nothing about the narrow tier
                ContentType = contentType,
            };

        private static Tag CreateEndpointTag() =>
            new Tag { Id = Guid.NewGuid() };

        // A stored row as the add path derives one: the versioned end keyed on its group under
        // AllVersions, the non-versioned end keyed on its own id under ThisVersionOnly.
        private static Association CreateStoredAssociation(
            ContentItem contentItemEndpoint,
            Tag tagEndpoint) =>
            new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = contentItemEndpoint.Id,
                EntityAGroupId = contentItemEndpoint.GroupId,
                EntityAScope = Scope.AllVersions,
                EntityAContentType = contentItemEndpoint.ContentType,
                EntityBType = EntityType.Tag,
                EntityBKeyId = tagEndpoint.Id,
                EntityBGroupId = tagEndpoint.Id,
                EntityBScope = Scope.ThisVersionOnly,
            };

        private void SetupVisibleAssociations(params Association[] associations) =>
            this.associationServiceMock.Setup(service =>
                service.RetrieveAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(associations.AsQueryable());

        // Every one of the six is set up, including the ones a given test's rows never touch:
        // Moq's default for IQueryable<T> is null, not an empty set, so an unstubbed endpoint
        // read is a NullReferenceException rather than a row that simply never matches.
        private void SetupEndpointCollectionReads(
            IEnumerable<ContentItem>? contentItems = null,
            IEnumerable<Tag>? tags = null,
            IEnumerable<Reaction>? reactions = null,
            IEnumerable<BibleReference>? bibleReferences = null,
            IEnumerable<Comment>? comments = null,
            IEnumerable<Link>? links = null)
        {
            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((contentItems ?? Array.Empty<ContentItem>()).AsQueryable());

            this.tagServiceMock.Setup(service =>
                service.RetrieveAllTagsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((tags ?? Array.Empty<Tag>()).AsQueryable());

            this.reactionServiceMock.Setup(service =>
                service.RetrieveAllReactionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((reactions ?? Array.Empty<Reaction>()).AsQueryable());

            this.bibleReferenceServiceMock.Setup(service =>
                service.RetrieveAllBibleReferencesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((bibleReferences ?? Array.Empty<BibleReference>()).AsQueryable());

            this.commentServiceMock.Setup(service =>
                service.RetrieveAllCommentsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((comments ?? Array.Empty<Comment>()).AsQueryable());

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync((links ?? Array.Empty<Link>()).AsQueryable());
        }

        private sealed class EnumerationCounter
        {
            public int Count { get; set; }
        }

        // Counts the times a queryable is actually walked. Composition through
        // IQueryProvider.CreateQuery never walks it, so a non-zero count at the point the
        // service returns means something above the read materialised rows.
        private sealed class EnumerationTrackingQueryable<T> : IQueryable<T>
        {
            private readonly IQueryable<T> innerQueryable;
            private readonly EnumerationCounter counter;

            public EnumerationTrackingQueryable(
                IQueryable<T> innerQueryable,
                EnumerationCounter counter)
            {
                this.innerQueryable = innerQueryable;
                this.counter = counter;
            }

            public Type ElementType => this.innerQueryable.ElementType;

            public Expression Expression => this.innerQueryable.Expression;

            public IQueryProvider Provider =>
                new EnumerationTrackingQueryProvider(this.innerQueryable.Provider, this.counter);

            public IEnumerator<T> GetEnumerator()
            {
                this.counter.Count++;

                return this.innerQueryable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class EnumerationTrackingQueryProvider : IQueryProvider
        {
            private readonly IQueryProvider innerProvider;
            private readonly EnumerationCounter counter;

            public EnumerationTrackingQueryProvider(
                IQueryProvider innerProvider,
                EnumerationCounter counter)
            {
                this.innerProvider = innerProvider;
                this.counter = counter;
            }

            public IQueryable CreateQuery(Expression expression) =>
                this.innerProvider.CreateQuery(expression);

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
                new EnumerationTrackingQueryable<TElement>(
                    this.innerProvider.CreateQuery<TElement>(expression),
                    this.counter);

            public object? Execute(Expression expression)
            {
                this.counter.Count++;

                return this.innerProvider.Execute(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                this.counter.Count++;

                return this.innerProvider.Execute<TResult>(expression);
            }
        }
    }
}
