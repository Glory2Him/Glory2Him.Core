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
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldAddAssociationAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association randomAssociation =
                CreateAssociationFiller(randomDateTimeOffset).Create();
            Association inputAssociation = randomAssociation;
            Association auditAppliedAssociation = inputAssociation.DeepClone();
            Association storageAssociation = auditAppliedAssociation.DeepClone();
            Association expectedAssociation = storageAssociation.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedAssociation.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAssociationAsync(
                    auditAppliedAssociation,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<Association>>(
                        new EventPublishResult<Association>()));

            // when
            Association actualAssociation =
                await this.associationService.AddAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(expectedAssociation);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertAssociationAsync(
                        auditAppliedAssociation,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .AssociationOnAddingAssociationSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAddAssociationUnderAMintedIdAsync()
        {
            // given
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string contributorUserId = GetRandomString();

            Association inputAssociation =
                CreateAssociationFiller(currentDateTime, contributorUserId).Create();

            // a contributor's suggestion sends two endpoints and nothing else, so its Id is
            // empty; the new row takes the Id the service mints (§13 rules 1 and 4)
            inputAssociation.Id = Guid.Empty;
            Guid mintedId = Guid.NewGuid();

            Association expectedInsertedAssociation = inputAssociation.DeepClone();
            expectedInsertedAssociation.Id = mintedId;
            Association storedAssociation = expectedInsertedAssociation.DeepClone();
            Association insertedAssociation = null;
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(mintedId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.Is(SameAssociationAs(expectedInsertedAssociation)),
                    this.ambientSecurityContext))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(contributorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAssociationAsync(
                    It.Is(SameAssociationAs(expectedInsertedAssociation)),
                    inputCancellationToken))
                        .Callback<Association, CancellationToken>((entity, _) =>
                            insertedAssociation = entity.DeepClone())
                        .ReturnsAsync(storedAssociation);

            // when
            Association actualAssociation =
                await this.associationService.AddAssociationAsync(
                    inputAssociation,
                    inputCancellationToken);

            // then: the row is inserted under the minted Id, and the fact and the returned row
            // carry it
            insertedAssociation.Should().BeEquivalentTo(expectedInsertedAssociation);
            insertedAssociation.Id.Should().Be(mintedId);
            actualAssociation.Should().BeSameAs(storedAssociation);
            actualAssociation.Id.Should().Be(mintedId);

            // one Id for the new row, and one for each of the two processed-event records
            this.identifierBrokerMock.Verify(broker =>
                    broker.GetIdentifierAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(
                        It.Is(SameAssociationAs(expectedInsertedAssociation)),
                        this.ambientSecurityContext),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertAssociationAsync(
                        It.Is(SameAssociationAs(expectedInsertedAssociation)),
                        inputCancellationToken),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.Is(SameOutboundEnvelopeAs(storedAssociation)),
                    AssociationEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .AssociationOnAddingAssociationSubscriptionName),
                    inputCancellationToken),
                Times.Exactly(2));

            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAddAssociationUnderTheMintedIdWhateverIdTheCallerSentAsync()
        {
            // given
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string contributorUserId = GetRandomString();

            Association inputAssociation =
                CreateAssociationFiller(currentDateTime, contributorUserId).Create();

            // the caller's Id is overwritten, neither compared nor refused (§13 rule 1)
            Guid callersId = Guid.NewGuid();
            inputAssociation.Id = callersId;
            Guid mintedId = Guid.NewGuid();
            mintedId.Should().NotBe(callersId);

            Association expectedInsertedAssociation = inputAssociation.DeepClone();
            expectedInsertedAssociation.Id = mintedId;
            Association storedAssociation = expectedInsertedAssociation.DeepClone();
            Association insertedAssociation = null;
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(mintedId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.Is(SameAssociationAs(expectedInsertedAssociation)),
                    this.ambientSecurityContext))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(contributorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAssociationAsync(
                    It.Is(SameAssociationAs(expectedInsertedAssociation)),
                    inputCancellationToken))
                        .Callback<Association, CancellationToken>((entity, _) =>
                            insertedAssociation = entity.DeepClone())
                        .ReturnsAsync(storedAssociation);

            // when
            Association actualAssociation =
                await this.associationService.AddAssociationAsync(
                    inputAssociation,
                    inputCancellationToken);

            // then: the row is inserted under the minted Id and never the caller's, and the
            // fact and the returned row carry the minted one
            insertedAssociation.Should().BeEquivalentTo(expectedInsertedAssociation);
            insertedAssociation.Id.Should().Be(mintedId);
            actualAssociation.Should().BeSameAs(storedAssociation);
            actualAssociation.Id.Should().Be(mintedId);

            // one Id for the new row, and one for each of the two processed-event records
            this.identifierBrokerMock.Verify(broker =>
                    broker.GetIdentifierAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(
                        It.Is(SameAssociationAs(expectedInsertedAssociation)),
                        this.ambientSecurityContext),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertAssociationAsync(
                        It.Is(SameAssociationAs(expectedInsertedAssociation)),
                        inputCancellationToken),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.Is(SameOutboundEnvelopeAs(storedAssociation)),
                    AssociationEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .AssociationOnAddingAssociationSubscriptionName),
                    inputCancellationToken),
                Times.Exactly(2));

            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
