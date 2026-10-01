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

        [Fact]
        public async Task ShouldChangeNothingWhenTheReaderGivesTheReactionTheyHoldAsync()
        {
            // given: the reader already holds Love, and gives Love
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);

            Association readersLiveRow =
                CreateStoredPersonalRowOnTheSameReaction(upsertRequest, isDeleted: false);

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(readersLiveRow).ToList();

            Association expectedAssociation = readersLiveRow.DeepClone();

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(storageAssociations, inputCancellationToken);

            // when
            PersonalAssociationUpsert actualUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    inputCancellationToken);

            // then: the row as it stands, untouched, and nothing written or announced
            actualUpsert.Outcome.Should().Be(PersonalAssociationUpsertOutcome.Unchanged);
            actualUpsert.Association.Should().BeSameAs(readersLiveRow);
            actualUpsert.Association.Should().BeEquivalentTo(expectedAssociation);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(upsertRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalUpsertLookupAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldNeverReviveARowAModeratorTookDownAsync(bool isTheSameReaction)
        {
            // given: the reader's row was withdrawn by somebody else — a takedown (§DOM4.10 rule 7).
            // Giving the same reaction would be a revive, and another would be a revive and a
            // repoint; neither is written.
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);

            Association takenDownRow = isTheSameReaction
                ? CreateStoredPersonalRowOnTheSameReaction(upsertRequest, isDeleted: true)
                : CreateStoredPersonalRow(upsertRequest, isDeleted: true);

            takenDownRow.DeletedBy = $"moderator-{Guid.NewGuid()}";

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(takenDownRow).ToList();

            Association expectedAssociation = takenDownRow.DeepClone();

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(storageAssociations, inputCancellationToken);

            // when
            PersonalAssociationUpsert actualUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    inputCancellationToken);

            // then: still taken down, and nothing written or announced
            actualUpsert.Outcome.Should().Be(PersonalAssociationUpsertOutcome.TakenDown);
            actualUpsert.Association.Should().BeSameAs(takenDownRow);
            actualUpsert.Association.Should().BeEquivalentTo(expectedAssociation);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(upsertRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalUpsertLookupAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldWriteOnlyTheEnumeratedFieldsOfAnExistingRowAsync()
        {
            // given: the reader's withdrawn row and another reaction, the arm that writes all five
            // of the fields in scope (§ARC16.2.2). The request differs from the stored row in every
            // field outside them that does not name the host and the reaction, so a field taken from
            // the caller's copy would show.
            string readerUserId = GetRandomString();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);

            Association readersWithdrawnRow =
                CreateStoredPersonalRow(upsertRequest, isDeleted: true);

            readersWithdrawnRow.ApprovalStatus = ApprovalStatus.Approved;
            readersWithdrawnRow.IsPublished = true;
            readersWithdrawnRow.PublishDate = GetRandomDateTimeOffset();
            readersWithdrawnRow.SortOrder = GetRandomNumber();
            readersWithdrawnRow.DeletionReason = GetRandomString();

            upsertRequest.ApprovalStatus = ApprovalStatus.Submitted;
            upsertRequest.IsPublished = false;
            upsertRequest.PublishDate = readersWithdrawnRow.PublishDate.Value.AddDays(GetRandomNumber());
            upsertRequest.SortOrder = readersWithdrawnRow.SortOrder + GetRandomNumber();
            upsertRequest.EntityAContentType = ContentType.Testimony;
            upsertRequest.EntityBContentType = ContentType.Story;
            upsertRequest.CreatedBy = $"caller-{Guid.NewGuid()}";
            upsertRequest.CreatedWhen = readersWithdrawnRow.CreatedWhen.AddDays(GetRandomNumber());
            upsertRequest.UpdatedBy = $"caller-{Guid.NewGuid()}";
            upsertRequest.UpdatedWhen = readersWithdrawnRow.UpdatedWhen.AddDays(GetRandomNumber());
            upsertRequest.DeletedBy = $"caller-{Guid.NewGuid()}";
            upsertRequest.DeletedWhen = readersWithdrawnRow.DeletedWhen?.AddDays(GetRandomNumber());
            upsertRequest.DeletionReason = GetRandomString();
            upsertRequest.ConfidenceScore = GetRandomConfidenceScore();
            upsertRequest.ConfidenceReason = GetRandomString();
            upsertRequest.SourceBatchId = Guid.NewGuid();
            upsertRequest.ModelVersion = GetRandomString();
            upsertRequest.IsApprovedByBypass = true;
            upsertRequest.ApprovedByBypassReason = GetRandomString();

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(readersWithdrawnRow).ToList();

            // the stored row, with the five fields in scope written and the audit stamp beside them
            Association expectedSavedAssociation = readersWithdrawnRow.DeepClone();
            expectedSavedAssociation.EntityBKeyId = upsertRequest.EntityBKeyId;
            expectedSavedAssociation.EntityBGroupId = upsertRequest.EntityBGroupId;
            expectedSavedAssociation.IsDeleted = false;
            expectedSavedAssociation.DeletedBy = null;
            expectedSavedAssociation.DeletedWhen = null;
            expectedSavedAssociation.UpdatedBy = readerUserId;
            expectedSavedAssociation.UpdatedWhen = currentDateTime;

            Association savedAssociation = null;

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

                        return entity;
                    });

            // when
            await this.associationService.UpsertPersonalAssociationAsync(
                upsertRequest,
                inputCancellationToken);

            // then: every other field kept its stored value, whatever the caller's copy said
            savedAssociation.Should().BeEquivalentTo(expectedSavedAssociation);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldNormaliseTheEndpointsBeforeResolvingTheRowAsync()
        {
            // given: the reader holds Love and gives Moved, in a request that names the reaction on
            // endpoint A and the host on B — the order a caller cannot be expected to know, and one
            // no stored row is ever in. Keyed as sent, the lookup would key off the reaction and miss
            // the reader's row.
            string readerUserId = GetRandomString();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association canonicalRequest = CreatePersonalUpsertRequest(readerUserId);
            Association reversedRequest = ReverseEndpoints(canonicalRequest);

            Association readersLiveRow =
                CreateStoredPersonalRow(canonicalRequest, isDeleted: false);

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(readersLiveRow).ToList();

            Association expectedSavedAssociation = readersLiveRow.DeepClone();
            expectedSavedAssociation.EntityBKeyId = canonicalRequest.EntityBKeyId;
            expectedSavedAssociation.EntityBGroupId = canonicalRequest.EntityBGroupId;
            StampModifyAudit(expectedSavedAssociation, readerUserId, currentDateTime);

            Association savedAssociation = null;

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

                        return entity;
                    });

            // when
            PersonalAssociationUpsert actualUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    reversedRequest,
                    inputCancellationToken);

            // then: the reader's own row, repointed to the reaction the request named first
            savedAssociation.Should().BeEquivalentTo(expectedSavedAssociation);
            actualUpsert.Outcome.Should().Be(PersonalAssociationUpsertOutcome.Repointed);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken),
                    Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), It.IsAny<CancellationToken>()),
                    Times.Never);
        }

        [Theory]
        [MemberData(nameof(ReadOnlyRolesOverAReactionByPersonalAct))]
        public async Task ShouldUpsertTheReadersReactionWhateverReadOnlyRoleTheyHoldAsync(
            string readOnlyRole,
            PersonalAssociationUpsertOutcome act)
        {
            // given: a reaction is not a contribution, so a reader's own is outside the read-only
            // veto, and giving, changing and reviving it asks none of the scopes over it (§SEC14.7
            // posture A′ rule 1)
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(readOnlyRole);
            Association upsertRequest = CreatePersonalUpsertRequest(readerUserId);

            List<Association> storageAssociations = CreateRandomAssociations().ToList();

            if (act == PersonalAssociationUpsertOutcome.Repointed)
            {
                storageAssociations.Add(CreateStoredPersonalRow(upsertRequest, isDeleted: false));
            }

            if (act == PersonalAssociationUpsertOutcome.Restored)
            {
                storageAssociations.Add(
                    CreateStoredPersonalRowOnTheSameReaction(upsertRequest, isDeleted: true));
            }

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalUpsertLookupOver(storageAssociations, inputCancellationToken);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext))
                    .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(It.IsAny<Association>(), this.ambientSecurityContext))
                    .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), inputCancellationToken))
                    .ReturnsAsync((Association entity, CancellationToken _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken))
                    .ReturnsAsync((Association entity, CancellationToken _) => entity);

            // when
            PersonalAssociationUpsert actualUpsert =
                await this.associationService.UpsertPersonalAssociationAsync(
                    upsertRequest,
                    inputCancellationToken);

            // then: written, as for a reader holding none
            actualUpsert.Outcome.Should().Be(act);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAssociationAsync(It.IsAny<Association>(), inputCancellationToken),
                    act == PersonalAssociationUpsertOutcome.Created ? Times.Once() : Times.Never());

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(It.IsAny<Association>(), inputCancellationToken),
                    act == PersonalAssociationUpsertOutcome.Created ? Times.Never() : Times.Once());

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // every read-only scope over a reader's reaction on a Quote, for each personal act: give
        // (no row, so Created), change (a live row on another reaction, so Repointed) and revive
        // (their withdrawn row on the same reaction, so Restored)
        public static TheoryData<string, PersonalAssociationUpsertOutcome>
            ReadOnlyRolesOverAReactionByPersonalAct()
        {
            var data = new TheoryData<string, PersonalAssociationUpsertOutcome>();

            string[] readOnlyRoles =
            {
                Roles.ReadOnly,
                Roles.ReactionReadOnly,
                Roles.ContentItemReadOnly,
                Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Quote)
            };

            PersonalAssociationUpsertOutcome[] acts =
            {
                PersonalAssociationUpsertOutcome.Created,
                PersonalAssociationUpsertOutcome.Repointed,
                PersonalAssociationUpsertOutcome.Restored
            };

            foreach (string readOnlyRole in readOnlyRoles)
            {
                foreach (PersonalAssociationUpsertOutcome act in acts)
                {
                    data.Add(readOnlyRole, act);
                }
            }

            return data;
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
