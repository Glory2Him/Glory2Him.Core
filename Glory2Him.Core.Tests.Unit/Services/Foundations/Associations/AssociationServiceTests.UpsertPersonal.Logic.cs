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
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        public async Task ShouldCreateTheReadersRowWhenTheyHaveNoneAsync(ApprovalStatus callersStatus)
        {
            // given: the reader has no row on the item. For each term of the personal key the store
            // holds a live row that misses on that term alone, so a condition that dropped or
            // inverted any term would find one of them and take another arm (§ARC12.2.1 rule 5).
            string readerUserId = GetRandomString();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);
            upsertRequest.ApprovalStatus = callersStatus;

            List<Association> storageAssociations =
                CreateRandomAssociations()
                    .Concat(CreatePersonalKeyNearMisses(upsertRequest))
                    .ToList();

            Association expectedInsertedAssociation =
                StampAddAudit(upsertRequest.DeepClone(), readerUserId, currentDateTime);

            Association insertedAssociation = null;
            Association storedAssociation = null;

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(storageAssociations, inputCancellationToken);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext))
                    .ReturnsAsync((Association entity, SecurityContext _) =>
                        StampAddAudit(entity.DeepClone(), readerUserId, currentDateTime));

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), inputCancellationToken))
                    .ReturnsAsync((Association entity, CancellationToken _) =>
                    {
                        insertedAssociation = entity.DeepClone();
                        storedAssociation = WithDatabaseComputedEffectiveIds(entity.DeepClone());

                        return storedAssociation;
                    });

            // when
            PersonalAssociationUpsert actualUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    inputCancellationToken);

            // then: the row the caller sent, at the status the caller set, stamped and inserted
            insertedAssociation.Should().BeEquivalentTo(expectedInsertedAssociation);
            insertedAssociation.ApprovalStatus.Should().Be(callersStatus);
            actualUpsert.Outcome.Should().Be(PersonalAssociationUpsertOutcome.Created);
            actualUpsert.Association.Should().BeSameAs(storedAssociation);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(upsertRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalUpsertLookupAsked(inputCancellationToken, Times.Once());

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), inputCancellationToken),
                    Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(It.IsAny<EventEnvelope<Association>>(), storedAssociation),
                    Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.Is(SameOutboundEnvelopeAs(storedAssociation)),
                    AssociationEventOperation.Added),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReviveTheReadersWithdrawnRowAtItsOwnStatusAsync()
        {
            // given: the reader withdrew this reaction themselves, at Approved, and the caller sends
            // Submitted, so a revive that wrote a status would show. An older withdrawn row of theirs
            // on the item, written before this feature and pointing at another reaction, comes
            // first in the store: the most recently updated row is the one revived (§DOM4.10 rule 6).
            string readerUserId = GetRandomString();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);

            Association readersWithdrawnRow =
                CreateStoredPersonalRowOnTheSameReaction(upsertRequest, isDeleted: true);

            readersWithdrawnRow.ApprovalStatus = ApprovalStatus.Approved;

            Association readersOlderWithdrawnRow =
                CreateStoredPersonalRow(upsertRequest, isDeleted: true);

            readersOlderWithdrawnRow.UpdatedWhen =
                readersWithdrawnRow.UpdatedWhen.AddDays(-GetRandomNumber());

            List<Association> storageAssociations =
                new List<Association> { readersOlderWithdrawnRow, readersWithdrawnRow }
                    .Concat(CreateRandomAssociations())
                    .ToList();

            Association expectedSavedAssociation = readersWithdrawnRow.DeepClone();
            expectedSavedAssociation.IsDeleted = false;
            expectedSavedAssociation.DeletedBy = null;
            expectedSavedAssociation.DeletedWhen = null;
            StampModifyAudit(expectedSavedAssociation, readerUserId, currentDateTime);

            Association savedAssociation = null;
            Association updatedAssociation = null;

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(storageAssociations, inputCancellationToken);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext))
                    .ReturnsAsync((Association entity, SecurityContext _) =>
                        StampModifyAudit(entity.DeepClone(), readerUserId, currentDateTime));

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken))
                    .ReturnsAsync((Association entity, CancellationToken _) =>
                    {
                        savedAssociation = entity.DeepClone();
                        updatedAssociation = entity.DeepClone();

                        return updatedAssociation;
                    });

            // when
            PersonalAssociationUpsert actualUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    inputCancellationToken);

            // then: the reader's row, live again at the status it was withdrawn at
            savedAssociation.Should().BeEquivalentTo(expectedSavedAssociation);
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            actualUpsert.Outcome.Should().Be(PersonalAssociationUpsertOutcome.Restored);
            actualUpsert.Association.Should().BeSameAs(updatedAssociation);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(upsertRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalUpsertLookupAsked(inputCancellationToken, Times.Once());

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken),
                    Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(It.IsAny<EventEnvelope<Association>>(), updatedAssociation),
                    Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.Is(SameOutboundEnvelopeAs(updatedAssociation)),
                    AssociationEventOperation.Restored),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReviveAndRepointTheReadersWithdrawnRowAsync()
        {
            // given: the reader withdrew Love and now gives Moved. There is no second row to insert
            // (§DOM4.10 rule 6), so their withdrawn row comes back pointing at Moved, and that is a
            // change, not a revive of the same pair.
            string readerUserId = GetRandomString();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);

            Association readersWithdrawnRow =
                CreateStoredPersonalRow(upsertRequest, isDeleted: true);

            readersWithdrawnRow.ApprovalStatus = ApprovalStatus.Approved;

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(readersWithdrawnRow).ToList();

            Association expectedSavedAssociation = readersWithdrawnRow.DeepClone();
            expectedSavedAssociation.IsDeleted = false;
            expectedSavedAssociation.DeletedBy = null;
            expectedSavedAssociation.DeletedWhen = null;
            expectedSavedAssociation.EntityBKeyId = upsertRequest.EntityBKeyId;
            expectedSavedAssociation.EntityBGroupId = upsertRequest.EntityBGroupId;
            StampModifyAudit(expectedSavedAssociation, readerUserId, currentDateTime);

            Association savedAssociation = null;
            Association updatedAssociation = null;

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(storageAssociations, inputCancellationToken);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext))
                    .ReturnsAsync((Association entity, SecurityContext _) =>
                        StampModifyAudit(entity.DeepClone(), readerUserId, currentDateTime));

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken))
                    .ReturnsAsync((Association entity, CancellationToken _) =>
                    {
                        savedAssociation = entity.DeepClone();
                        updatedAssociation = entity.DeepClone();

                        return updatedAssociation;
                    });

            // when
            PersonalAssociationUpsert actualUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    inputCancellationToken);

            // then: one write, live again and pointing at the reaction the reader gave
            savedAssociation.Should().BeEquivalentTo(expectedSavedAssociation);
            actualUpsert.Outcome.Should().Be(PersonalAssociationUpsertOutcome.Repointed);
            actualUpsert.Association.Should().BeSameAs(updatedAssociation);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(upsertRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalUpsertLookupAsked(inputCancellationToken, Times.Once());

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken),
                    Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(It.IsAny<EventEnvelope<Association>>(), updatedAssociation),
                    Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.Is(SameOutboundEnvelopeAs(updatedAssociation)),
                    AssociationEventOperation.Repointed),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRepointTheReadersLiveRowAsync()
        {
            // given: the reader holds Love, at Submitted, and gives Moved. A withdrawn row of theirs
            // on the item, written before this feature, pointing at Moved and more recently updated,
            // comes first in the store: the live row is the one taken (§DOM4.10 rule 6). The live
            // row's DeletedWhen is left as the filler draws it, so a repoint that wrote the revive's
            // fields as well would show.
            string readerUserId = GetRandomString();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);
            upsertRequest.ApprovalStatus = ApprovalStatus.Draft;

            Association readersLiveRow =
                CreateStoredPersonalRow(upsertRequest, isDeleted: false);

            readersLiveRow.ApprovalStatus = ApprovalStatus.Submitted;

            Association readersWithdrawnRow =
                CreateStoredPersonalRowOnTheSameReaction(upsertRequest, isDeleted: true);

            readersWithdrawnRow.UpdatedWhen =
                readersLiveRow.UpdatedWhen.AddDays(GetRandomNumber());

            List<Association> storageAssociations =
                new List<Association> { readersWithdrawnRow, readersLiveRow }
                    .Concat(CreateRandomAssociations())
                    .ToList();

            Association expectedSavedAssociation = readersLiveRow.DeepClone();
            expectedSavedAssociation.EntityBKeyId = upsertRequest.EntityBKeyId;
            expectedSavedAssociation.EntityBGroupId = upsertRequest.EntityBGroupId;
            StampModifyAudit(expectedSavedAssociation, readerUserId, currentDateTime);

            Association savedAssociation = null;
            Association updatedAssociation = null;

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(storageAssociations, inputCancellationToken);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext))
                    .ReturnsAsync((Association entity, SecurityContext _) =>
                        StampModifyAudit(entity.DeepClone(), readerUserId, currentDateTime));

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken))
                    .ReturnsAsync((Association entity, CancellationToken _) =>
                    {
                        savedAssociation = entity.DeepClone();
                        updatedAssociation = entity.DeepClone();

                        return updatedAssociation;
                    });

            // when
            PersonalAssociationUpsert actualUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    inputCancellationToken);

            // then: the endpoint moved and nothing else did, the status least of all (§DOM4.5 rule 4)
            savedAssociation.Should().BeEquivalentTo(expectedSavedAssociation);
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            actualUpsert.Outcome.Should().Be(PersonalAssociationUpsertOutcome.Repointed);
            actualUpsert.Association.Should().BeSameAs(updatedAssociation);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(upsertRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalUpsertLookupAsked(inputCancellationToken, Times.Once());

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken),
                    Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(It.IsAny<EventEnvelope<Association>>(), updatedAssociation),
                    Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.Is(SameOutboundEnvelopeAs(updatedAssociation)),
                    AssociationEventOperation.Repointed),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldRepointATerminalRowAsync(ApprovalStatus terminalStatus)
        {
            // given: the reader's reaction was decided, and they change it. This member is the
            // terminal bar's one exception (§SEC14.7 posture A′ rule 2): the repoint is written, and
            // the change goes back through the approval process on the fact it publishes.
            string readerUserId = GetRandomString();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);

            Association readersDecidedRow =
                CreateStoredPersonalRow(upsertRequest, isDeleted: false);

            readersDecidedRow.ApprovalStatus = terminalStatus;
            readersDecidedRow.IsPublished = terminalStatus == ApprovalStatus.Approved;

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(readersDecidedRow).ToList();

            Association expectedSavedAssociation = readersDecidedRow.DeepClone();
            expectedSavedAssociation.EntityBKeyId = upsertRequest.EntityBKeyId;
            expectedSavedAssociation.EntityBGroupId = upsertRequest.EntityBGroupId;
            StampModifyAudit(expectedSavedAssociation, readerUserId, currentDateTime);

            Association savedAssociation = null;
            Association updatedAssociation = null;

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(storageAssociations, inputCancellationToken);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext))
                    .ReturnsAsync((Association entity, SecurityContext _) =>
                        StampModifyAudit(entity.DeepClone(), readerUserId, currentDateTime));

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken))
                    .ReturnsAsync((Association entity, CancellationToken _) =>
                    {
                        savedAssociation = entity.DeepClone();
                        updatedAssociation = entity.DeepClone();

                        return updatedAssociation;
                    });

            // when
            PersonalAssociationUpsert actualUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    inputCancellationToken);

            // then: written, at the status it was decided at
            savedAssociation.Should().BeEquivalentTo(expectedSavedAssociation);
            savedAssociation.ApprovalStatus.Should().Be(terminalStatus);
            actualUpsert.Outcome.Should().Be(PersonalAssociationUpsertOutcome.Repointed);
            actualUpsert.Association.Should().BeSameAs(updatedAssociation);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(upsertRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalUpsertLookupAsked(inputCancellationToken, Times.Once());

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken),
                    Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(It.IsAny<EventEnvelope<Association>>(), updatedAssociation),
                    Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.Is(SameOutboundEnvelopeAs(updatedAssociation)),
                    AssociationEventOperation.Repointed),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A reader's reaction on a Quote as the orchestration hands it over (§ARC16.8.1): the host on
        // endpoint A under AllVersions, with a group id that differs from its key id, and the
        // reaction on B, a non-versioned endpoint, so ThisVersionOnly with its group its key.
        private static Association CreatePersonalUpsertRequest(string readerUserId)
        {
            Association upsertRequest = CreatePersonalLookupRequest(readerUserId);
            upsertRequest.EntityBScope = Scope.ThisVersionOnly;
            upsertRequest.ApprovalStatus = ApprovalStatus.Submitted;

            return upsertRequest;
        }

        // One live row per term of the personal key, each missing on that term alone: another
        // host type, another host, another far-end type and another reader.
        private static IEnumerable<Association> CreatePersonalKeyNearMisses(Association upsertRequest) =>
            new[]
            {
                nameof(Association.EntityAType),
                nameof(Association.EntityAEffectiveId),
                nameof(Association.EntityBType),
                nameof(Association.UserId)
            }.Select(term =>
                MissOnePersonalKeyTerm(
                    CreateStoredPersonalRow(upsertRequest, isDeleted: false),
                    term));

        // The reader's stored row on the request's host, pointing at the reaction the request names.
        private static Association CreateStoredPersonalRowOnTheSameReaction(
            Association upsertRequest,
            bool isDeleted)
        {
            Association storedRow = CreateStoredPersonalRow(upsertRequest, isDeleted);
            storedRow.EntityBKeyId = upsertRequest.EntityBKeyId;
            storedRow.EntityBGroupId = upsertRequest.EntityBGroupId;

            return WithDatabaseComputedEffectiveIds(storedRow);
        }

        // what the audit broker does with a row being changed: the caller signed on the envelope, now
        private static Association StampModifyAudit(
            Association association,
            string userId,
            DateTimeOffset currentDateTime)
        {
            association.UpdatedBy = userId;
            association.UpdatedWhen = currentDateTime;

            return association;
        }

        // what the audit broker does with a new row: the caller signed on the envelope, now
        private static Association StampAddAudit(
            Association association,
            string userId,
            DateTimeOffset currentDateTime)
        {
            association.CreatedBy = userId;
            association.UpdatedBy = userId;
            association.CreatedWhen = currentDateTime;
            association.UpdatedWhen = currentDateTime;

            return association;
        }

        // The mock stands in for the storage client: it APPLIES the query-shaping function the
        // service authored to the seeded set, so each test proves the condition by executing it
        // (§ARC12.2.1 rule 5). The rows come back whole, because the upsert writes the row it finds.
        private void SetupPersonalUpsertLookupOver(
            IEnumerable<Association> storageAssociations,
            CancellationToken cancellationToken) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<Association>>>(),
                    cancellationToken))
                        .ReturnsAsync((
                            Func<IQueryable<Association>, IQueryable<Association>> query,
                            CancellationToken _) =>
                                (IReadOnlyList<Association>)
                                    query(storageAssociations.AsQueryable()).ToList());

        private void VerifyPersonalUpsertLookupAsked(CancellationToken cancellationToken, Times times) =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<Association>>>(),
                    cancellationToken),
                times);
    }
}
