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
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    /// <summary>
    /// What each transition WRITES. The validation and exception suites cover the refusals,
    /// which leaves the successful paths asserted only by the fact-publishing test — and that
    /// one checks which address was published on, not what landed on the row. Everything here
    /// exists to fail when the field copies or the sort arithmetic are wrong.
    /// </summary>
    public partial class AssociationServiceTests
    {
        public static TheoryData<SortPosition, int> SortPositionsAndOffsets() =>
            new TheoryData<SortPosition, int>
            {
                { SortPosition.Before, -50 },
                { SortPosition.After, 50 }
            };

        [Theory]
        [MemberData(nameof(SortPositionsAndOffsets))]
        public async Task ShouldPlaceTheRowAHalfStepFromTheAnchorOnSortAsync(
            SortPosition position,
            int expectedOffset)
        {
            // given: the two directions must land on DIFFERENT sides of the anchor. A single
            // test, or one that only asserted "the sort order changed", passes on a service
            // that ignores the position and always inserts after — the caller's Before/After
            // would be inert and nothing would say so.
            int anchorSortOrder = 200;
            string actorUserId = GetRandomString();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Association storageAssociation = CreateSubmittableStorageAssociation();
            storageAssociation.CreatedBy = actorUserId;
            storageAssociation.SortOrder = anchorSortOrder + 5000;

            Association anchorAssociation = CreateAnchorAssociation(anchorSortOrder);
            Association savedAssociation = null;

            SetupStorageRead(storageAssociation);
            SetupStorageRead(anchorAssociation);
            SetupActor(actorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association entity, CancellationToken _) =>
                        {
                            savedAssociation = entity.DeepClone();

                            return entity;
                        });

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Sorted))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            // when
            Association actualAssociation =
                await this.associationService.SortAssociationAsync(
                    new Association { Id = storageAssociation.Id },
                    new Association { Id = anchorAssociation.Id },
                    position,
                    TestContext.Current.CancellationToken);

            // then: a half-step on the side the caller named. At the default spacing of 100
            // that IS the midpoint between the anchor and its neighbour, which is why exactly
            // one row is written and no neighbour is read.
            savedAssociation.Should().NotBeNull();
            savedAssociation.SortOrder.Should().Be(anchorSortOrder + expectedOffset);
            actualAssociation.SortOrder.Should().Be(anchorSortOrder + expectedOffset);

            // the anchor is read, never written — a sort that renumbered its neighbours would
            // be multi-row work and belongs at orchestration
            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheConfidenceFieldsFromTheCallerOnSetConfidenceAsync()
        {
            // given: the same shape as the approve field-scope test, for the operation that
            // owns the OTHER four fields. All four move together — a human correcting a
            // machine score must clear the provenance in the same write, or the row claims a
            // model produced a score a publisher typed.
            //
            // Without this, deleting any one of the four copies leaves the suite green.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            string actorUserId = GetRandomString();

            Association storageAssociation = CreateSubmittableStorageAssociation();
            storageAssociation.EntityAType = EntityType.ContentItem;
            storageAssociation.EntityAContentType = ContentType.Story;
            storageAssociation.EntityBType = EntityType.Tag;
            storageAssociation.EntityBContentType = null;
            storageAssociation.SortOrder = 100;

            // set-confidence REFUSES the owner outright, so the actor must be somebody else
            storageAssociation.CreatedBy = GetRandomString();

            // the service copies onto the instance the storage broker returns, so the
            // expectation has to be taken before the act rather than read off it afterwards
            Association expectedStorageAssociation = storageAssociation.DeepClone();

            Association inputAssociation =
                CreateConfidenceDecision(storageAssociation.Id);

            // the caller also sends everything the operation does NOT own, deliberately
            // different from storage, so "came from storage" is a falsifiable claim
            inputAssociation.EntityAType = EntityType.Link;
            inputAssociation.EntityAKeyId = Guid.NewGuid();
            inputAssociation.EntityAGroupId = Guid.NewGuid();
            inputAssociation.EntityAContentType = null;
            inputAssociation.EntityAScope = Scope.ThisVersionOnly;
            inputAssociation.EntityBType = EntityType.ContentItem;
            inputAssociation.EntityBKeyId = Guid.NewGuid();
            inputAssociation.EntityBGroupId = Guid.NewGuid();
            inputAssociation.EntityBContentType = ContentType.Testimony;
            inputAssociation.EntityBScope = Scope.ThisVersionOnly;
            inputAssociation.UserId = $"caller-{Guid.NewGuid()}";
            inputAssociation.SortOrder = expectedStorageAssociation.SortOrder + 1;
            inputAssociation.ApprovalStatus = ApprovalStatus.Approved;
            inputAssociation.IsPublished = true;
            inputAssociation.PublishDate = GetRandomDateTimeOffset();
            inputAssociation.CreatedBy = $"caller-{Guid.NewGuid()}";
            inputAssociation.CreatedWhen =
                expectedStorageAssociation.CreatedWhen.AddDays(1);

            Association savedAssociation = null;

            SetupStorageRead(storageAssociation);
            SetupActor(actorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association entity, CancellationToken _) =>
                        {
                            savedAssociation = entity.DeepClone();

                            return entity;
                        });

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.ConfidenceSet))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            // when
            await this.associationService.SetAssociationConfidenceAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then: the four fields the operation owns came from the caller
            savedAssociation.Should().NotBeNull();
            savedAssociation.ConfidenceScore.Should().Be(inputAssociation.ConfidenceScore);
            savedAssociation.ConfidenceReason.Should().Be(inputAssociation.ConfidenceReason);
            savedAssociation.SourceBatchId.Should().Be(inputAssociation.SourceBatchId);
            savedAssociation.ModelVersion.Should().Be(inputAssociation.ModelVersion);

            // everything else came from STORAGE — most sharply the approval state, which a
            // scoring pass must never carry
            savedAssociation.ApprovalStatus.Should()
                .Be(expectedStorageAssociation.ApprovalStatus);

            savedAssociation.IsPublished.Should().Be(expectedStorageAssociation.IsPublished);
            savedAssociation.PublishDate.Should().Be(expectedStorageAssociation.PublishDate);
            savedAssociation.EntityAType.Should().Be(expectedStorageAssociation.EntityAType);
            savedAssociation.EntityAKeyId.Should().Be(expectedStorageAssociation.EntityAKeyId);
            savedAssociation.EntityAGroupId.Should().Be(expectedStorageAssociation.EntityAGroupId);
            savedAssociation.EntityAScope.Should().Be(expectedStorageAssociation.EntityAScope);

            savedAssociation.EntityAContentType.Should()
                .Be(expectedStorageAssociation.EntityAContentType);

            savedAssociation.EntityBType.Should().Be(expectedStorageAssociation.EntityBType);
            savedAssociation.EntityBKeyId.Should().Be(expectedStorageAssociation.EntityBKeyId);
            savedAssociation.EntityBGroupId.Should().Be(expectedStorageAssociation.EntityBGroupId);
            savedAssociation.EntityBScope.Should().Be(expectedStorageAssociation.EntityBScope);

            savedAssociation.EntityBContentType.Should()
                .Be(expectedStorageAssociation.EntityBContentType);

            savedAssociation.UserId.Should().Be(expectedStorageAssociation.UserId);
            savedAssociation.SortOrder.Should().Be(expectedStorageAssociation.SortOrder);
            savedAssociation.CreatedBy.Should().Be(expectedStorageAssociation.CreatedBy);
            savedAssociation.CreatedWhen.Should().Be(expectedStorageAssociation.CreatedWhen);
        }

        [Fact]
        public async Task ShouldClearTheMachineProvenanceOnAHumanConfidenceCorrectionAsync()
        {
            // given: the reason all four fields move as a unit. A publisher overriding a model
            // score must leave the row saying a human set it — otherwise a retraction sweeping
            // the model's batch would sweep up the human's correction too.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            string actorUserId = GetRandomString();

            Association storageAssociation = CreateSubmittableStorageAssociation();
            storageAssociation.CreatedBy = GetRandomString();
            storageAssociation.SourceBatchId = Guid.NewGuid();
            storageAssociation.ModelVersion = "model-v7";
            storageAssociation.ConfidenceReason = "scored by the batch";

            Association inputAssociation =
                CreateHumanConfidenceDecision(storageAssociation.Id);

            Association savedAssociation = null;

            SetupStorageRead(storageAssociation);
            SetupActor(actorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association entity, CancellationToken _) =>
                        {
                            savedAssociation = entity.DeepClone();

                            return entity;
                        });

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.ConfidenceSet))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            // when
            await this.associationService.SetAssociationConfidenceAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then: the nulls are WRITTEN, not skipped as "nothing supplied"
            savedAssociation.Should().NotBeNull();
            savedAssociation.SourceBatchId.Should().BeNull();
            savedAssociation.ModelVersion.Should().BeNull();
            savedAssociation.ConfidenceScore.Should().Be(inputAssociation.ConfidenceScore);
            savedAssociation.ConfidenceReason.Should().Be(inputAssociation.ConfidenceReason);
        }
    }
}
