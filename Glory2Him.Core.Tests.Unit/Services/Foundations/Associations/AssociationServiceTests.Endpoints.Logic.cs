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
using System.Data.SqlTypes;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Services.Foundations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // Two ids chosen so the two comparers disagree, which is the whole point of the
        // trap. SQL Server orders `uniqueidentifier` by bytes 10-15 first, so it reads the
        // trailing ...02 / ...01 and puts Second below First. .NET compares the leading
        // 4-byte integer first, so it reads 00000000 / 00000001 and puts First below Second.
        private static readonly Guid SqlLowGuid =
            new Guid("00000001-0000-0000-0000-000000000001");

        private static readonly Guid SqlHighGuid =
            new Guid("00000000-0000-0000-0000-000000000002");

        [Fact]
        public void ShouldOrderEndpointGuidsTheWaySqlServerDoesNotTheWayDotNetDoes()
        {
            // given: a pair the two comparers rank in opposite directions — if that premise
            // ever stops holding, the assertions below stop proving anything, so both
            // comparers are pinned here rather than assumed
            int dotNetComparison = SqlHighGuid.CompareTo(SqlLowGuid);
            int sqlComparison = new SqlGuid(SqlHighGuid).CompareTo(new SqlGuid(SqlLowGuid));

            dotNetComparison.Should().BeNegative(
                because: "the .NET comparer reads the leading integer and ranks these the other way");

            sqlComparison.Should().BePositive(
                because: "SQL Server reads bytes 10-15 first");

            // when: the same entity type on both sides, so the guid tiebreak decides
            int actualComparison = AssociationService.CompareEndpoints(
                firstType: EntityType.BibleReference,
                firstGroupId: SqlHighGuid,
                secondType: EntityType.BibleReference,
                secondGroupId: SqlLowGuid);

            // then: ours must agree with SQL Server, or the database's own canonical-order
            // check constraint would reject rows this service considers correctly ordered
            actualComparison.Should().BePositive();
        }

        [Fact]
        public void ShouldOrderEndpointsOnTheEntityTypeNameBeforeTheGroupId()
        {
            // given: the type names decide, so the guid draw must not matter — Attachment
            // sorts below ContentItem ordinally whichever way the ids fall
            int comparisonWithLowGuidFirst = AssociationService.CompareEndpoints(
                firstType: EntityType.Attachment,
                firstGroupId: SqlLowGuid,
                secondType: EntityType.ContentItem,
                secondGroupId: SqlHighGuid);

            int comparisonWithHighGuidFirst = AssociationService.CompareEndpoints(
                firstType: EntityType.Attachment,
                firstGroupId: SqlHighGuid,
                secondType: EntityType.ContentItem,
                secondGroupId: SqlLowGuid);

            // then
            comparisonWithLowGuidFirst.Should().BeNegative();
            comparisonWithHighGuidFirst.Should().BeNegative();
        }

        [Fact]
        public async Task ShouldSwapEndpointsOnAddWhenTheyAreNotInCanonicalOrderAsync()
        {
            // given: a Bible reference paired with a content item, supplied the wrong way
            // round. ContentItem sorts above BibleReference ordinally, so the service must
            // move the Bible reference to the A side.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid contentItemKeyId = Guid.NewGuid();
            Guid contentItemGroupId = Guid.NewGuid();
            Guid bibleReferenceKeyId = Guid.NewGuid();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();

            inputAssociation.EntityAType = EntityType.ContentItem;
            inputAssociation.EntityAKeyId = contentItemKeyId;
            inputAssociation.EntityAGroupId = contentItemGroupId;
            inputAssociation.EntityAContentType = ContentType.Story;
            inputAssociation.EntityBType = EntityType.BibleReference;
            inputAssociation.EntityBKeyId = bibleReferenceKeyId;
            inputAssociation.EntityBGroupId = bibleReferenceKeyId;
            inputAssociation.EntityBContentType = null;

            SetupAddPathBrokers(inputAssociation, randomDateTimeOffset);

            // when
            await this.associationService.AddAssociationAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then: every field of an endpoint travels with it — a half-swapped row would
            // claim a key id belonging to the other entity
            inputAssociation.EntityAType.Should().Be(EntityType.BibleReference);
            inputAssociation.EntityAKeyId.Should().Be(bibleReferenceKeyId);
            inputAssociation.EntityAGroupId.Should().Be(bibleReferenceKeyId);
            inputAssociation.EntityAScope.Should().Be(Scope.ThisVersionOnly);
            inputAssociation.EntityAContentType.Should().BeNull();

            inputAssociation.EntityBType.Should().Be(EntityType.ContentItem);
            inputAssociation.EntityBKeyId.Should().Be(contentItemKeyId);
            inputAssociation.EntityBGroupId.Should().Be(contentItemGroupId);
            inputAssociation.EntityBScope.Should().Be(Scope.AllVersions);
            inputAssociation.EntityBContentType.Should().Be(ContentType.Story);
        }

        [Fact]
        public async Task ShouldOrderSameTypeEndpointsOnAddUsingSqlServerGuidSemanticsAsync()
        {
            // given: the Related Passages case — the same entity type on both sides, so the
            // guid tiebreak is the only thing deciding. The ids are the pair the .NET and
            // SQL comparers rank oppositely, so a Guid.CompareTo here would leave the row
            // exactly as supplied and this test would fail.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();

            inputAssociation.EntityAType = EntityType.BibleReference;
            inputAssociation.EntityAKeyId = SqlHighGuid;
            inputAssociation.EntityBType = EntityType.BibleReference;
            inputAssociation.EntityBKeyId = SqlLowGuid;

            SetupAddPathBrokers(inputAssociation, randomDateTimeOffset);

            // when
            await this.associationService.AddAssociationAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then
            inputAssociation.EntityAKeyId.Should().Be(SqlLowGuid);
            inputAssociation.EntityBKeyId.Should().Be(SqlHighGuid);
        }

        [Theory]
        [InlineData(EntityType.ContentItem, Scope.AllVersions)]
        [InlineData(EntityType.Link, Scope.AllVersions)]
        [InlineData(EntityType.Attachment, Scope.AllVersions)]
        [InlineData(EntityType.BibleReference, Scope.ThisVersionOnly)]
        [InlineData(EntityType.Tag, Scope.ThisVersionOnly)]
        [InlineData(EntityType.Reaction, Scope.ThisVersionOnly)]
        [InlineData(EntityType.Comment, Scope.ThisVersionOnly)]
        [InlineData(EntityType.Association, Scope.ThisVersionOnly)]
        public async Task ShouldDeriveEndpointScopeFromThePublicationModelOnAddAsync(
            EntityType entityType,
            Scope expectedScope)
        {
            // given: whatever scope the caller asks for, the publication model decides —
            // a non-versioned entity has exactly one row, so AllVersions would be a
            // distinction without a difference
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();

            inputAssociation.EntityAType = entityType;
            inputAssociation.EntityAKeyId = SqlLowGuid;
            inputAssociation.EntityAGroupId = Guid.NewGuid();
            inputAssociation.EntityAContentType = null;

            // Tag sorts at or above every other member ordinally, and SqlHighGuid breaks the
            // Tag-to-Tag tie the same way, so the endpoint under test stays on the A side
            // for every row of the theory and the assertion can read A directly
            inputAssociation.EntityBType = EntityType.Tag;
            inputAssociation.EntityBKeyId = SqlHighGuid;
            inputAssociation.EntityBContentType = null;

            Scope callerSuppliedScope = expectedScope == Scope.AllVersions
                ? Scope.ThisVersionOnly
                : Scope.AllVersions;

            inputAssociation.EntityAScope = callerSuppliedScope;

            SetupAddPathBrokers(inputAssociation, randomDateTimeOffset);

            // when
            await this.associationService.AddAssociationAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then
            inputAssociation.EntityAType.Should().Be(entityType);
            inputAssociation.EntityAScope.Should().Be(expectedScope);
        }

        [Fact]
        public async Task ShouldSetGroupIdToKeyIdOnAddForANonVersionedEndpointAsync()
        {
            // given: a non-versioned entity is its own group, which is what lets one rule
            // ("the two group ids must differ") also catch a tag associated with itself
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid tagKeyId = Guid.NewGuid();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();

            inputAssociation.EntityAType = EntityType.ContentItem;
            inputAssociation.EntityAKeyId = Guid.NewGuid();
            inputAssociation.EntityAGroupId = Guid.NewGuid();
            inputAssociation.EntityAContentType = null;
            inputAssociation.EntityBType = EntityType.Tag;
            inputAssociation.EntityBKeyId = tagKeyId;
            inputAssociation.EntityBGroupId = Guid.NewGuid();
            inputAssociation.EntityBContentType = null;

            SetupAddPathBrokers(inputAssociation, randomDateTimeOffset);

            // when
            await this.associationService.AddAssociationAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then: the caller's unrelated group id draw is discarded
            inputAssociation.EntityBType.Should().Be(EntityType.Tag);
            inputAssociation.EntityBGroupId.Should().Be(tagKeyId);
            inputAssociation.EntityBKeyId.Should().Be(tagKeyId);
        }

        [Fact]
        public async Task ShouldNormalizeEndpointOrderBeforeHandingTheRowToStorageAsync()
        {
            // given: issue #106 places normalisation inside DoAdd specifically so it cannot
            // be bypassed. Asserting on the caller's object after the call cannot tell the
            // difference between normalising before the insert and normalising after it —
            // the object is the same reference either way — so this snapshots the row at the
            // moment storage receives it.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid contentItemKeyId = Guid.NewGuid();
            Guid bibleReferenceKeyId = Guid.NewGuid();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();

            inputAssociation.EntityAType = EntityType.ContentItem;
            inputAssociation.EntityAKeyId = contentItemKeyId;
            inputAssociation.EntityAGroupId = Guid.NewGuid();
            inputAssociation.EntityBType = EntityType.BibleReference;
            inputAssociation.EntityBKeyId = bibleReferenceKeyId;

            Association associationAsStorageSawIt = null;

            SetupAddPathBrokers(inputAssociation, randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAssociationAsync(
                    inputAssociation,
                    It.IsAny<CancellationToken>()))
                    .Callback<Association, CancellationToken>((association, _) =>
                        associationAsStorageSawIt = association.DeepClone())
                    .ReturnsAsync(inputAssociation);

            // when
            await this.associationService.AddAssociationAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then: BibleReference sorts below ContentItem ordinally, so storage must have
            // been handed the swapped row, not the caller's ordering
            associationAsStorageSawIt.Should().NotBeNull();
            associationAsStorageSawIt.EntityAType.Should().Be(EntityType.BibleReference);
            associationAsStorageSawIt.EntityAKeyId.Should().Be(bibleReferenceKeyId);
            associationAsStorageSawIt.EntityBType.Should().Be(EntityType.ContentItem);
            associationAsStorageSawIt.EntityBKeyId.Should().Be(contentItemKeyId);
        }

        [Fact]
        public async Task ShouldRejectATagAssociatedWithItselfOnAddAsync()
        {
            // given: the same tag on both endpoints, with two different caller-supplied
            // group ids. Only the non-versioned GroupId := KeyId derivation collapses these
            // to one value and lets the same-endpoint rule see them as equal — so this
            // pins that derivation running BEFORE validation, which nothing else does.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Guid tagKeyId = Guid.NewGuid();

            Association invalidAssociation =
                CreateAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            invalidAssociation.EntityAType = EntityType.Tag;
            invalidAssociation.EntityAKeyId = tagKeyId;
            invalidAssociation.EntityAGroupId = Guid.NewGuid();
            invalidAssociation.EntityBType = EntityType.Tag;
            invalidAssociation.EntityBKeyId = tagKeyId;
            invalidAssociation.EntityBGroupId = Guid.NewGuid();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<AssociationValidationException>(
                addAssociationTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private void SetupAddPathBrokers(
            Association association,
            DateTimeOffset currentDateTimeOffset)
        {
            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(association, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(association);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(association.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAssociationAsync(
                    association,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(association);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<Association>>(
                        new EventPublishResult<Association>()));
        }
    }
}
