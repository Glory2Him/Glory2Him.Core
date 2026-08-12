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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        // Sets up the two endpoint reads (a ContentItem on A, a Tag on B) and hands back the
        // ContentItem so a test can assert what was derived from it.
        private ContentItem SetupEndpointReads(Association rawRequest)
        {
            var resolvedContentItem = new ContentItem
            {
                Id = rawRequest.EntityAKeyId,
                ContentItemGroupId = Guid.NewGuid(),
                ContentType = ContentType.Story,
            };

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    rawRequest.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(resolvedContentItem);

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(
                    rawRequest.EntityBKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new Tag { Id = rawRequest.EntityBKeyId });

            return resolvedContentItem;
        }

        [Fact]
        public async Task ShouldInsertAndReturnCreatedWhenThePairIsUnoccupiedAsync()
        {
            // given
            Association rawRequest = CreateRawAddRequest();
            SetupEndpointReads(rawRequest);

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AssociationPairMatch?)null);

            var insertedId = Guid.NewGuid();

            this.associationServiceMock.Setup(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association association, CancellationToken _) =>
                        {
                            association.Id = insertedId;
                            return association;
                        });

            // when
            AssociationSuggestionResult actualResult =
                await this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Status.Should().Be(AssociationSuggestionStatus.Created);
            actualResult.AssociationId.Should().Be(insertedId);

            this.associationServiceMock.Verify(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldReturnAlreadyApprovedWithoutInsertingWhenAnApprovedRowOccupiesThePairAsync()
        {
            // given
            Association rawRequest = CreateRawAddRequest();
            SetupEndpointReads(rawRequest);

            AssociationPairMatch approvedMatch =
                CreatePairMatch(ApprovalStatus.Approved, isDeleted: false);

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(approvedMatch);

            // when
            AssociationSuggestionResult actualResult =
                await this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            // then: returned as-is, nothing inserted, only the id echoed
            actualResult.Status.Should().Be(AssociationSuggestionStatus.AlreadyApproved);
            actualResult.AssociationId.Should().Be(approvedMatch.Id);

            this.associationServiceMock.Verify(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldReturnAlreadyPendingWithoutInsertingForANonApprovedLiveRowAsync(
            ApprovalStatus nonApprovedStatus)
        {
            // given: pending AND rejected map to the SAME AlreadyPending status, so a contributor
            // cannot infer a rejection by resubmitting.
            Association rawRequest = CreateRawAddRequest();
            SetupEndpointReads(rawRequest);

            AssociationPairMatch liveMatch =
                CreatePairMatch(nonApprovedStatus, isDeleted: false);

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(liveMatch);

            // when
            AssociationSuggestionResult actualResult =
                await this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Status.Should().Be(AssociationSuggestionStatus.AlreadyPending);
            actualResult.AssociationId.Should().Be(liveMatch.Id);

            this.associationServiceMock.Verify(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldReturnAlreadyPendingWithoutInsertingWhenTheOnlyRowIsSoftDeletedAsync()
        {
            // given: a soft-deleted row occupies the pair. This pass never inserts past it — that
            // would duplicate it or launder a moderator takedown — and reports it as pending,
            // revealing nothing. The row is deliberately a once-APPROVED one: the deleted branch
            // must mask it as AlreadyPending, never leak AlreadyApproved (which would disclose the
            // takedown). (Resurrecting the caller's own row is a later pass.)
            Association rawRequest = CreateRawAddRequest();
            SetupEndpointReads(rawRequest);

            AssociationPairMatch deletedMatch =
                CreatePairMatch(ApprovalStatus.Approved, isDeleted: true);

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(deletedMatch);

            // when
            AssociationSuggestionResult actualResult =
                await this.associationOrchestrationService.AddAssociationAsync(
                    rawRequest,
                    TestContext.Current.CancellationToken);

            // then: never inserts past the deleted row
            actualResult.Status.Should().Be(AssociationSuggestionStatus.AlreadyPending);
            actualResult.AssociationId.Should().Be(deletedMatch.Id);

            this.associationServiceMock.Verify(service =>
                service.AddAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldResolveEndpointsAndDeriveScopeGroupAndContentTypeBeforeLookupAsync()
        {
            // given: the caller supplies BOGUS scope/group/content-type values, which the
            // orchestration must overwrite with the resolved ones — the content type is an
            // authorization input and a caller-set scope could claim AllVersions on a group-less
            // entity. The derived values are asserted on the entity handed to the lookup.
            Association rawRequest = CreateRawAddRequest();
            rawRequest.EntityAScope = Scope.ThisVersionOnly;      // wrong on purpose
            rawRequest.EntityAGroupId = Guid.NewGuid();           // wrong on purpose
            rawRequest.EntityAContentType = ContentType.Testimony; // wrong on purpose
            rawRequest.EntityBScope = Scope.AllVersions;          // wrong on purpose
            rawRequest.EntityBContentType = ContentType.Story;    // wrong on purpose (a Tag has none)

            ContentItem resolvedContentItem = SetupEndpointReads(rawRequest);

            Association? capturedForLookup = null;

            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Association, CancellationToken>(
                            (association, _) => capturedForLookup = association)
                        .ReturnsAsync(CreatePairMatch(ApprovalStatus.Approved, isDeleted: false));

            // when
            await this.associationOrchestrationService.AddAssociationAsync(
                rawRequest,
                TestContext.Current.CancellationToken);

            // then
            capturedForLookup.Should().NotBeNull();

            // A endpoint (a ContentItem): versioned -> AllVersions, group and content type from
            // the resolved row, overriding the caller's bogus values
            capturedForLookup!.EntityAScope.Should().Be(Scope.AllVersions);
            capturedForLookup.EntityAGroupId.Should().Be(resolvedContentItem.ContentItemGroupId);
            capturedForLookup.EntityAContentType.Should().Be(resolvedContentItem.ContentType);

            // B endpoint (a Tag): non-versioned -> ThisVersionOnly, group is its own key id, no
            // content type
            capturedForLookup.EntityBScope.Should().Be(Scope.ThisVersionOnly);
            capturedForLookup.EntityBGroupId.Should().Be(rawRequest.EntityBKeyId);
            capturedForLookup.EntityBContentType.Should().BeNull();
        }
    }
}
