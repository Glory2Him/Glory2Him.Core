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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldSubmitBibleReferenceByOwnerAsync()
        {
            // given: the owner submitting their own draft — no moderation role required
            BibleReference storageBibleReference = CreateSubmittableStorageBibleReference();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            BibleReference submittedBibleReference = storageBibleReference.DeepClone();
            submittedBibleReference.ApprovalStatus = ApprovalStatus.Submitted;

            BibleReference auditAppliedBibleReference = submittedBibleReference.DeepClone();
            BibleReference updatedBibleReference = auditAppliedBibleReference.DeepClone();
            BibleReference expectedBibleReference = updatedBibleReference.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageBibleReference.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            SetupBibleReferenceStorageRead(storageBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedBibleReference);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(
                    auditAppliedBibleReference,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                            new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                    storageBibleReference.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectBibleReferenceByIdAsync(
                        storageBibleReference.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<BibleReference>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateBibleReferenceAsync(
                        auditAppliedBibleReference,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        BibleReferenceEventOperation.Submitted),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .BibleReferenceOnSubmittingBibleReferenceSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            // submit never consults the cross-entity decision — that is the approve's gate
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSubmitBibleReferenceByPublisherWhoIsNotTheOwnerAsync()
        {
            // given: the publisher tier may move a submission status too — the same set the §9.2
            // modify carve-out admits. The caller is NOT the owner, so this proves the
            // publisher-tier branch rather than the ownership branch.
            BibleReference storageBibleReference = CreateSubmittableStorageBibleReference();

            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"someone-else-{Guid.NewGuid()}");

            SetupBibleReferenceStorageRead(storageBibleReference);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((BibleReference entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((BibleReference entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    It.IsAny<BibleReferenceEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                            new EventPublishResult<BibleReference>()));

            // when
            await this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                storageBibleReference.Id,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        BibleReferenceEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheStatusFieldOnSubmitAsync()
        {
            // given: submit owns ONLY the approval status. It drives Draft -> Submitted and must
            // leave every other field exactly as stored — a content edit is the general modify's
            // job, not submit's. Asserting the whole row against the pre-act snapshot, excluding
            // only the one field submit owns, catches any stray write.
            BibleReference storageBibleReference = CreateSubmittableStorageBibleReference();
            BibleReference expectedStorageBibleReference = storageBibleReference.DeepClone();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageBibleReference.CreatedBy);

            // when
            BibleReference savedBibleReference = await CaptureSavedBibleReferenceOnSubmitAsync(storageBibleReference);

            // then
            savedBibleReference.Should().NotBeNull();
            savedBibleReference.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            savedBibleReference.Should().BeEquivalentTo(
                expectedStorageBibleReference,
                options => options.Excluding(bibleReference => bibleReference.ApprovalStatus));
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnSubmitAsync()
        {
            // given: like every transition, submit publishes its own fact and never Modified —
            // the approval workflow's cycle-breaker (design §9.7.1, issue #111 case 1).
            BibleReference storageBibleReference = CreateSubmittableStorageBibleReference();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageBibleReference.CreatedBy);

            // when
            await CaptureSavedBibleReferenceOnSubmitAsync(storageBibleReference);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        BibleReferenceEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishBibleReferenceAsync(
                        It.IsAny<EventEnvelope<BibleReference>>(),
                        BibleReferenceEventOperation.Submitted),
                Times.Once);
        }

        // Runs a permitted submit end to end (owner already set up by the caller) and hands back
        // a snapshot of the row that reached the storage broker.
        private async ValueTask<BibleReference> CaptureSavedBibleReferenceOnSubmitAsync(BibleReference storageBibleReference)
        {
            BibleReference savedBibleReference = null;

            SetupBibleReferenceStorageRead(storageBibleReference);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((BibleReference entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<BibleReference, CancellationToken>(
                            (entity, _) => savedBibleReference = entity.DeepClone())
                        .ReturnsAsync((BibleReference entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    It.IsAny<BibleReferenceEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                            new EventPublishResult<BibleReference>()));

            await this.bibleReferenceService.SubmitBibleReferenceByIdAsync(
                storageBibleReference.Id,
                TestContext.Current.CancellationToken);

            return savedBibleReference;
        }
    }
}
