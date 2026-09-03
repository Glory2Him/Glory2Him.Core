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
    /// <summary>
    /// The narrow tier of the endpoint veto — <c>ContentItem-{ContentType}-ReadOnly</c> on
    /// either end (design §18.6 rule 2, §14.7 posture A′ rule 1).
    ///
    /// <para><b>A <c>Series</c>–<c>Quote</c> row is the case to reason from</b>, because it is
    /// the one where both endpoints are content items carrying <i>different</i> content types,
    /// so all four narrow names — two grants, two blocks — are in play at once and neither can
    /// stand in for the other.</para>
    ///
    /// <para><b>The <c>OR</c> runs in both directions.</b> On the grant side one end is enough
    /// to admit: requiring both would leave every cross-type association unreviewable by anyone
    /// short of a global role. On the block side one end is enough to bar. Each case below pairs
    /// a block on one end with a grant on the other, so a rule that quietly let the grant rescue
    /// the row would fail here rather than pass everywhere.</para>
    /// </summary>
    public partial class AssociationServiceTests
    {
        // Both ends are content items, so the entity-type tier says nothing about either of
        // them on its own — only the content type separates the two endpoints.
        private static Association CreateSeriesQuoteAssociation(DateTimeOffset dateTimeOffset)
        {
            Association association = CreateAssociationFiller(dateTimeOffset).Create();
            association.EntityAType = EntityType.ContentItem;
            association.EntityAKeyId = Guid.NewGuid();
            association.EntityAContentType = ContentType.Series;
            association.EntityBType = EntityType.ContentItem;
            association.EntityBKeyId = Guid.NewGuid();
            association.EntityBContentType = ContentType.Quote;

            return association;
        }

        private static AssociationValidationException ExpectedEndpointBlockException() =>
            new AssociationValidationException(
                message:
                    "Content item association validation error occurred, fix the errors and try again.",
                innerException: new UnauthorizedAssociationException(
                    message:
                        "The current user is blocked from contributing content item associations."));

        public static TheoryData<ContentType, ContentType> BlockedEndpointAgainstGrantedEndpoint() =>
            new TheoryData<ContentType, ContentType>
            {
                { ContentType.Series, ContentType.Quote },
                { ContentType.Quote, ContentType.Series },
            };

        [Theory]
        [MemberData(nameof(BlockedEndpointAgainstGrantedEndpoint))]
        public async Task ShouldBlockContributionWhenTheNarrowBlockCoversEitherEndpointAsync(
            ContentType blockedContentType,
            ContentType grantedContentType)
        {
            // given: blocked on one end, a reviewer for the other. Each side is exercised in
            // turn so neither passes by position — canonical ordering decides which of the
            // caller's two entities lands on A, and a rule reading one side only would pass or
            // fail on the alphabet.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReadOnlyFor(EntityType.ContentItem, blockedContentType),
                Roles.ReviewersFor(EntityType.ContentItem, grantedContentType));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association inputAssociation = CreateSeriesQuoteAssociation(randomDateTimeOffset);

            AssociationValidationException expectedException = ExpectedEndpointBlockException();

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldBlockContributionWhenTheNarrowBlockCoversAnEndpointAndTheCallerIsAdministratorAsync()
        {
            // given: no grant outranks the veto, and Administrators is the widest one there is.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Administrators,
                Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Quote));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association inputAssociation = CreateSeriesQuoteAssociation(randomDateTimeOffset);

            AssociationValidationException expectedException = ExpectedEndpointBlockException();

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldNotBlockContributionWhenTheNarrowBlockCoversNeitherEndpointAsync()
        {
            // given: a block on a content type neither end carries is SILENT — not weakened,
            // not outvoted, simply not asked. Reaching the audit stamp is the proof: the veto
            // is the first thing an add does.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Testimony));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association inputAssociation = CreateSeriesQuoteAssociation(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAssociation);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            await Record.ExceptionAsync(addAssociationTask.AsTask);

            // then
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotBlockContributionWhenTheNarrowBlockNamesADifferentEntityTypeAsync()
        {
            // given: the composed name carries the ENTITY type as well as the content type, so
            // a Tag-Quote-ReadOnly can never be matched against a ContentItem endpoint that
            // happens to carry Quote — the mirror of posture A′ rule 6 on the grant side.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReadOnlyFor(EntityType.Tag, ContentType.Quote));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association inputAssociation = CreateSeriesQuoteAssociation(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAssociation);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            await Record.ExceptionAsync(addAssociationTask.AsTask);

            // then
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()),
                Times.Once);
        }

        // ── The undecidable narrow tier ────────────────────────────────────

        [Fact]
        public async Task ShouldBlockContributionWhenAContentItemEndpointCarriesNoContentTypeAsync()
        {
            // given: the dodge this closes, and it needs no lie — just an omission. A null
            // content type is LEGAL on a ContentItem endpoint at this layer (the value is
            // derived by the orchestration, and validation admits a null), so without the
            // fail-closed branch a caller could step around every narrow block there is by
            // leaving the field out, with no knowledge of which types the sanction covers.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Quote));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association inputAssociation = CreateSeriesQuoteAssociation(randomDateTimeOffset);
            inputAssociation.EntityAContentType = null;
            inputAssociation.EntityBContentType = null;

            AssociationValidationException expectedException = ExpectedEndpointBlockException();

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    addAssociationTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldNotBlockContributionWhenAnEndpointCarriesNoContentTypeAndNoNarrowBlockIsHeldAsync()
        {
            // given: the fail-closed branch costs an UNSANCTIONED caller nothing. It fires only
            // for somebody the narrow tier actually covers, so a null content type stays the
            // ordinary case for everybody else.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ContentItemReviewers);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association inputAssociation = CreateSeriesQuoteAssociation(randomDateTimeOffset);
            inputAssociation.EntityAContentType = null;
            inputAssociation.EntityBContentType = null;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAssociation);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            await Record.ExceptionAsync(addAssociationTask.AsTask);

            // then
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotBlockContributionWhenANonContentItemEndpointCarriesNoContentTypeAsync()
        {
            // given: only ContentItem carries a content type (§18.6 rule 5), so a null on a Tag
            // endpoint is not an undecidable narrow tier — it is the absence of one. Failing
            // closed there would bar a narrowly sanctioned contributor from every association
            // in the system, which is the over-application this branch is scoped to avoid.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Quote));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association inputAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();

            inputAssociation.EntityAType = EntityType.Tag;
            inputAssociation.EntityAKeyId = Guid.NewGuid();
            inputAssociation.EntityAContentType = null;
            inputAssociation.EntityBType = EntityType.BibleReference;
            inputAssociation.EntityBKeyId = Guid.NewGuid();
            inputAssociation.EntityBContentType = null;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAssociation);

            // when
            ValueTask<Association> addAssociationTask =
                this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            await Record.ExceptionAsync(addAssociationTask.AsTask);

            // then
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldBlockRemoveWhenTheNarrowBlockCoversAStoredEndpointAsync()
        {
            // given: the remove path is handed an id, so the endpoint half of the veto only
            // runs once the row is loaded — and it runs against the STORED endpoints.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Quote));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association storageAssociation = CreateSeriesQuoteAssociation(randomDateTimeOffset);
            Guid associationId = storageAssociation.Id;

            AssociationValidationException expectedException = ExpectedEndpointBlockException();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    associationId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

            // when
            ValueTask<Association> removeAssociationTask =
                this.associationService.RemoveAssociationByIdAsync(
                    associationId,
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    removeAssociationTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldBlockHardRemoveWhenTheNarrowBlockCoversAStoredEndpointAsync()
        {
            // given: an administrator holding the narrow block. Hard remove is the one write an
            // endpoint-blocked Administrators could otherwise still perform, and it takes the
            // audit trail with it.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Administrators,
                Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Series));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association storageAssociation = CreateSeriesQuoteAssociation(randomDateTimeOffset);
            Guid associationId = storageAssociation.Id;

            AssociationValidationException expectedException = ExpectedEndpointBlockException();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    associationId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

            // when
            ValueTask<Association> hardRemoveAssociationTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    associationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    hardRemoveAssociationTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedException);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
