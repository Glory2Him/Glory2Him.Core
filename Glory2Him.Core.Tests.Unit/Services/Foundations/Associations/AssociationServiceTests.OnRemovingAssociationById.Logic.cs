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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldRemoveAssociationByIdAndReplyOnRemovingAssociationByIdEventAsync()
        {
            // given
            string randomDeletionReason = GetRandomString();
            Association storageAssociation = CreateRandomAssociation();
            storageAssociation.IsDeleted = false;
            Association auditedAssociation = storageAssociation.DeepClone();
            Association removedAssociation = auditedAssociation.DeepClone();
            Association expectedAssociation = removedAssociation.DeepClone();

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Association
                {
                    Id = storageAssociation.Id,
                    DeletionReason = randomDeletionReason
                },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageAssociation.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageAssociation, It.IsAny<SecurityContext>(), randomDeletionReason))
                    .ReturnsAsync(auditedAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(removedAssociation);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Association>>(
                        new EventPublishResult<Association>()));

            // when
            EventEnvelope<Association>? actualReplyEnvelope =
                await this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageAssociation, It.IsAny<SecurityContext>(), randomDeletionReason),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers
                                    .AssociationOnRemovingAssociationByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .AssociationOnRemovingAssociationByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReadOnlyRolesOverAReaction))]
        public async Task ShouldRemoveTheCallersOwnReactionWhateverReadOnlyRoleTheyHoldOnRemovingAssociationByIdEventAsync(
            string readOnlyRole)
        {
            // given: the signed caller holds the read-only role and the ambient caller holds
            // none, so the exemption is asked of the inbound envelope's caller alone, and a
            // fact minted through CreateAsync would carry the wrong caller
            // (Backend/Foundations/AssociationService.md §6 rules 1 and 2)
            string randomDeletionReason = GetRandomString();
            string readerUserId = GetRandomString();
            Association storageAssociation = CreateRandomReaction(readerUserId);
            storageAssociation.IsDeleted = false;
            Association auditedAssociation = storageAssociation.DeepClone();
            auditedAssociation.IsDeleted = true;
            Association removedAssociation = auditedAssociation.DeepClone();
            Association expectedAssociation = removedAssociation.DeepClone();
            EventEnvelope<Association>? publishedEnvelope = null;

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(readOnlyRole),
                Content = new Association
                {
                    Id = storageAssociation.Id,
                    DeletionReason = randomDeletionReason
                },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext))
                    .ReturnsAsync(readerUserId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(
                    storageAssociation,
                    requestEnvelope.SecurityContext,
                    randomDeletionReason))
                        .ReturnsAsync(auditedAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, TestContext.Current.CancellationToken))
                    .ReturnsAsync(removedAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.Is<EventEnvelope<Association>>(envelope =>
                        envelope.Content == removedAssociation
                            && envelope.SecurityContext == requestEnvelope.SecurityContext),
                    AssociationEventOperation.Removed))
                        .Callback((EventEnvelope<Association> envelope, AssociationEventOperation _) =>
                            publishedEnvelope = envelope)
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            // when
            EventEnvelope<Association>? actualReplyEnvelope =
                await this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedAssociation);
            actualReplyEnvelope.SecurityContext.Should().BeSameAs(requestEnvelope.SecurityContext);

            publishedEnvelope.Should().NotBeNull();
            publishedEnvelope!.Content.Should().BeSameAs(removedAssociation);
            publishedEnvelope.SecurityContext.Should().BeSameAs(requestEnvelope.SecurityContext);
            publishedEnvelope.SecurityContext.Should().NotBeSameAs(this.ambientSecurityContext);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(
                    storageAssociation,
                    requestEnvelope.SecurityContext,
                    randomDeletionReason),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, TestContext.Current.CancellationToken),
                Times.Once);

            // one envelope made next for the fact, one for the reply — both from the inbound one
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(requestEnvelope, removedAssociation),
                Times.Exactly(2));

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Association>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.Is<EventEnvelope<Association>>(envelope =>
                        envelope.Content == removedAssociation
                            && envelope.SecurityContext == requestEnvelope.SecurityContext),
                    AssociationEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers
                                    .AssociationOnRemovingAssociationByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == publishedEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers
                                    .AssociationOnRemovingAssociationByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipRemoveAndReplyNullWhenRemovingAssociationByIdEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Association { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Association>? actualReplyEnvelope =
                await this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReplyWithExistingAssociationOnRemovingAssociationByIdEventWhenAlreadyDeletedAsync()
        {
            // given
            Association alreadyDeletedAssociation = CreateRandomAssociation();
            alreadyDeletedAssociation.IsDeleted = true;
            Association expectedAssociation = alreadyDeletedAssociation.DeepClone();

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Association { Id = alreadyDeletedAssociation.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    alreadyDeletedAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(alreadyDeletedAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(alreadyDeletedAssociation.CreatedBy);

            // when
            EventEnvelope<Association>? actualReplyEnvelope =
                await this.associationService.OnRemovingAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: already deleted — no mutation happened, so no fact is published and
            // nothing is recorded as processed; the existing entity is returned as the reply.
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnRemovingAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    alreadyDeletedAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
