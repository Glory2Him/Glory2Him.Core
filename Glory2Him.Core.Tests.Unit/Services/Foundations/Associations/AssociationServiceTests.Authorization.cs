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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // ── The contribution veto ────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldBlockContributionWhenOnlyOneEndpointIsBannedAndLogItAsync()
        {
            // given: the exact scenario the OR polarity exists for. This caller is banned
            // from tags but trusted with Bible references — under an AND they would be
            // allowed to pair a tag with a scripture passage and land it on a public page,
            // which is precisely what Tag-ReadOnly is meant to prevent.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.TagReadOnly,
                Roles.BibleReferenceReviewer);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();

            inputAssociation.EntityAType = EntityType.BibleReference;
            inputAssociation.EntityAKeyId = Guid.NewGuid();
            inputAssociation.EntityBType = EntityType.Tag;
            inputAssociation.EntityBKeyId = Guid.NewGuid();

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        public static TheoryData<bool> BannedEndpointSides() =>
            new TheoryData<bool> { true, false };

        [Theory]
        [MemberData(nameof(BannedEndpointSides))]
        public async Task ShouldBlockContributionWhenEitherEndpointIsBannedAndLogItAsync(
            bool banTheAEndpoint)
        {
            // given: the veto must reach both sides. Canonical ordering decides which of the
            // caller's two entities lands on A, so a rule that only inspected one side would
            // pass or fail depending on the alphabet.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                banTheAEndpoint ? Roles.BibleReferenceReadOnly : Roles.TagReadOnly);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();

            inputAssociation.EntityAType = EntityType.BibleReference;
            inputAssociation.EntityAKeyId = Guid.NewGuid();
            inputAssociation.EntityBType = EntityType.Tag;
            inputAssociation.EntityBKeyId = Guid.NewGuid();

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldNotBlockContributionWhenTheBanIsForAnUninvolvedEntityTypeAsync()
        {
            // given: a ban on an entity type neither endpoint uses must not bleed across —
            // otherwise a single scoped ReadOnly would behave like the global block role
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.CommentReadOnly);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();

            inputAssociation.EntityAType = EntityType.BibleReference;
            inputAssociation.EntityAKeyId = Guid.NewGuid();
            inputAssociation.EntityBType = EntityType.Tag;
            inputAssociation.EntityBKeyId = Guid.NewGuid();

            SetupAddPathBrokers(inputAssociation, randomDateTimeOffset);

            // when
            Association actualAssociation =
                await this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().NotBeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    inputAssociation,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotApplyTheContributionVetoToReadsAsync()
        {
            // given: a moderator who happens to hold one scoped ReadOnly. Design §18.6
            // defines ReadOnly as a CONTRIBUTION block; threading it into the read path
            // would strip their audit visibility, which is the opposite of what the role
            // is for.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.TagReadOnly,
                Roles.Reviewer);

            string randomActorUserId = GetRandomString();
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.EntityAType = EntityType.Tag;
            storageAssociation.IsDeleted = false;
            storageAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageAssociation.IsPublished = false;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            Association actualAssociation =
                await this.associationService.RetrieveAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(storageAssociation);
        }

        // ── Endpoint-derived review roles ────────────────────────────────────────────

        public static TheoryData<string, EntityType, ContentType?> EndpointScopedReviewRoles() =>
            new TheoryData<string, EntityType, ContentType?>
            {
                // coarse tier, matching on the A endpoint
                { Roles.TagReviewer, EntityType.Tag, null },
                { Roles.TagPublisher, EntityType.Tag, null },

                // coarse tier, matching on the B endpoint (ContentItem is always B here,
                // because Tag sorts above ContentItem ordinally)
                { Roles.ContentItemReviewer, EntityType.Tag, null },

                // narrow tier — the content type on the ContentItem endpoint
                { "ContentItem-Testimony-Reviewer", EntityType.Tag, ContentType.Testimony },
                { "ContentItem-Testimony-Publisher", EntityType.Tag, ContentType.Testimony }
            };

        [Theory]
        [MemberData(nameof(EndpointScopedReviewRoles))]
        public async Task ShouldRetrieveNonPublicAssociationWhenAScopedRoleMatchesAnEndpointAsync(
            string scopedRole,
            EntityType otherEndpointType,
            ContentType? contentItemContentType)
        {
            // given: a scoped role matching AT LEAST ONE endpoint is enough — the pairing is
            // what is under review, and the reviewer can see both ends of it
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(scopedRole);
            string randomActorUserId = GetRandomString();

            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.EntityAType = otherEndpointType;
            storageAssociation.EntityAContentType = null;
            storageAssociation.EntityBType = EntityType.ContentItem;
            storageAssociation.EntityBContentType = contentItemContentType;
            storageAssociation.IsDeleted = false;
            storageAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageAssociation.IsPublished = false;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            Association actualAssociation =
                await this.associationService.RetrieveAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(storageAssociation);
        }

        [Fact]
        public async Task ShouldDenyNonPublicReadWhenTheNarrowRoleIsForADifferentContentTypeAsync()
        {
            // given: a reviewer trusted with stories, looking at a testimony. The whole
            // point of the narrow tier is that it does NOT widen to the entity type.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext("ContentItem-Story-Reviewer");

            string randomActorUserId = GetRandomString();
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.EntityAType = EntityType.Tag;
            storageAssociation.EntityAContentType = null;
            storageAssociation.EntityBType = EntityType.ContentItem;
            storageAssociation.EntityBContentType = ContentType.Testimony;
            storageAssociation.IsDeleted = false;
            storageAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageAssociation.IsPublished = false;

            var expectedException = new NotFoundAssociationException(
                message: $"Content item association not found with id: {storageAssociation.Id}.");

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<Association> retrieveTask =
                this.associationService.RetrieveAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then: a denied read answers not-found, never unauthorized (design §14.5)
            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    retrieveTask.AsTask);

            actualException.InnerException.Should().BeEquivalentTo(expectedException);
        }

        [Fact]
        public async Task ShouldDenyNonPublicReadWhenNoScopedRoleMatchesEitherEndpointAsync()
        {
            // given: a reviewer for an entity type that appears on neither endpoint
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.CommentReviewer);

            string randomActorUserId = GetRandomString();
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.EntityAType = EntityType.Tag;
            storageAssociation.EntityAContentType = null;
            storageAssociation.EntityBType = EntityType.ContentItem;
            storageAssociation.EntityBContentType = null;
            storageAssociation.IsDeleted = false;
            storageAssociation.ApprovalStatus = ApprovalStatus.Draft;
            storageAssociation.IsPublished = false;

            var expectedException = new NotFoundAssociationException(
                message: $"Content item association not found with id: {storageAssociation.Id}.");

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<Association> retrieveTask =
                this.associationService.RetrieveAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    retrieveTask.AsTask);

            actualException.InnerException.Should().BeEquivalentTo(expectedException);
        }

        // ── The collection read filter ───────────────────────────────────────────────

        [Fact]
        public async Task ShouldIncludeScopedReviewableRowsInTheCollectionReadAsync()
        {
            // given: this filter builds an expression tree and has no row to inspect, so the
            // reviewable sets are resolved in memory first. A coarse Tag reviewer must see
            // every non-public tag association and nothing else that is non-public.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagReviewer);

            string randomActorUserId = GetRandomString();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Association reviewableTagAssociation =
                CreateNonPublicAssociation(EntityType.Tag, contentType: null);

            Association unrelatedCommentAssociation =
                CreateNonPublicAssociation(EntityType.Comment, contentType: null);

            IQueryable<Association> storageAssociations = new[]
            {
                reviewableTagAssociation,
                unrelatedCommentAssociation
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(reviewableTagAssociation);
        }

        [Fact]
        public async Task ShouldIncludeNarrowlyReviewableRowsInTheCollectionReadAsync()
        {
            // given: the narrow tier has to survive the trip into the expression tree too —
            // a reviewer for testimonies sees testimonies, not stories
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext("ContentItem-Testimony-Reviewer");

            string randomActorUserId = GetRandomString();

            Association testimonyAssociation =
                CreateNonPublicAssociation(EntityType.Tag, ContentType.Testimony);

            Association storyAssociation =
                CreateNonPublicAssociation(EntityType.Tag, ContentType.Story);

            IQueryable<Association> storageAssociations = new[]
            {
                testimonyAssociation,
                storyAssociation
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(testimonyAssociation);
        }

        [Fact]
        public async Task ShouldExcludeNonPublicRowsFromTheCollectionReadForAnUnscopedCallerAsync()
        {
            // given: no scoped roles at all. Both sets resolve empty, and an empty
            // HashSet.Contains is constant-false, so the query must degrade to exactly the
            // public-plus-own predicate it had before endpoint scoping existed.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();

            Association nonPublicAssociation =
                CreateNonPublicAssociation(EntityType.Tag, contentType: null);

            IQueryable<Association> storageAssociations = new[]
            {
                nonPublicAssociation
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEmpty();
        }

        // a non-public row owned by somebody else, so only a review role can reach it. The
        // ContentItem endpoint carries the content type, because that is the only entity
        // type with a narrow tier.
        private static Association CreateNonPublicAssociation(
            EntityType otherEndpointType,
            ContentType? contentType)
        {
            Association association = CreateRandomAssociation();
            association.EntityAType = otherEndpointType;
            association.EntityAContentType = null;
            association.EntityBType = EntityType.ContentItem;
            association.EntityBContentType = contentType;
            association.IsDeleted = false;
            association.ApprovalStatus = ApprovalStatus.Draft;
            association.IsPublished = false;
            association.CreatedBy = GetRandomString();

            return association;
        }
    }
}
