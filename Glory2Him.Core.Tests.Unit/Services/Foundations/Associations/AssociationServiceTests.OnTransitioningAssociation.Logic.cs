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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    /// <summary>
    /// The event path of the three state transitions that have a request address. Sort is
    /// absent by design — its signature needs an anchor and a side, and an envelope carries
    /// exactly one entity.
    ///
    /// <para>These handlers are reachable through PUBLIC event addresses, which is the whole
    /// reason the foundation service enforces the approval rules rather than trusting the
    /// orchestration. An untested handler is an unguarded front door: the deduplication check
    /// could be deleted outright and nothing would say so, while a redelivered approval
    /// re-ran the write and re-published the fact.</para>
    /// </summary>
    public partial class AssociationServiceTests
    {
        public static TheoryData<string, string> TransitionRequestAddresses() =>
            new TheoryData<string, string>
            {
                {
                    "Approve",
                    EventBrokerIdentifiers.AssociationOnApprovingAssociationSubscriptionName
                },
                {
                    "SetConfidence",
                    EventBrokerIdentifiers
                        .AssociationOnSettingAssociationConfidenceSubscriptionName
                },
                {
                    "SetScope",
                    EventBrokerIdentifiers.AssociationOnSettingAssociationScopeSubscriptionName
                }
            };

        [Theory]
        [MemberData(nameof(TransitionRequestAddresses))]
        public async Task ShouldSkipTransitionAndReplyNullWhenTheRequestWasAlreadyProcessedAsync(
            string transitionName,
            string receiverName)
        {
            // given: a redelivery of a request this receiver has already applied. Approving
            // twice would re-publish Approved and re-stamp PublishDate; re-scoping twice would
            // re-run the pair-uniqueness check against a row that already moved.
            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = new Association { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    receiverName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Association>? actualReplyEnvelope =
                await InvokeTransitionHandlerAsync(transitionName, requestEnvelope);

            // then: no work, no fact, no reply. The VerifyNoOtherCalls is what makes deleting
            // the guard fail — without it the handler would fall through to the storage read.
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    receiverName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldApproveAndReplyOnApprovingAssociationEventAsync()
        {
            // given
            Association storageAssociation = CreateApprovableStorageAssociation();
            storageAssociation.CreatedBy = GetRandomString();

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = CreateApprovalDecision(storageAssociation.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupUnprocessedTransitionRequest(
                requestEnvelope,
                EventBrokerIdentifiers.AssociationOnApprovingAssociationSubscriptionName);

            SetupStorageRead(storageAssociation);
            SetupTransitionWriteBrokers();

            // HR-2 now lives behind the access broker, which the fixture leaves permissive.
            // The actor is still stamped on the transition's audit fields, so it is set here.
            SetupActor(GetRandomString());

            // when
            EventEnvelope<Association>? actualReplyEnvelope =
                await this.associationService.OnApprovingAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            actualReplyEnvelope!.Content.ApprovalStatus.Should()
                .Be(requestEnvelope.Content.ApprovalStatus);

            actualReplyEnvelope.Content.IsPublished.Should()
                .Be(requestEnvelope.Content.IsPublished);

            // the request's own security context decides, not an ambient one — there is no
            // HttpContext on the event path
            actualReplyEnvelope.SecurityContext.Should()
                .BeSameAs(requestEnvelope.SecurityContext);

            // the operation's OWN fact, never Modified
            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Approved),
                Times.Once);

            VerifyTransitionStorageCalls(
                requestEnvelope,
                EventBrokerIdentifiers.AssociationOnApprovingAssociationSubscriptionName,
                storageAssociation);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSetConfidenceAndReplyOnSettingAssociationConfidenceEventAsync()
        {
            // given
            Association storageAssociation = CreateSubmittableStorageAssociation();
            storageAssociation.CreatedBy = GetRandomString();

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = CreateConfidenceDecision(storageAssociation.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupUnprocessedTransitionRequest(
                requestEnvelope,
                EventBrokerIdentifiers
                    .AssociationOnSettingAssociationConfidenceSubscriptionName);

            SetupStorageRead(storageAssociation);
            SetupTransitionWriteBrokers();

            // set-confidence refuses the owner outright
            SetupActor(GetRandomString());

            // when
            EventEnvelope<Association>? actualReplyEnvelope =
                await this.associationService.OnSettingAssociationConfidenceAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            actualReplyEnvelope!.Content.ConfidenceScore.Should()
                .Be(requestEnvelope.Content.ConfidenceScore);

            actualReplyEnvelope.Content.SourceBatchId.Should()
                .Be(requestEnvelope.Content.SourceBatchId);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.ConfidenceSet),
                Times.Once);

            VerifyTransitionStorageCalls(
                requestEnvelope,
                EventBrokerIdentifiers
                    .AssociationOnSettingAssociationConfidenceSubscriptionName,
                storageAssociation);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSetScopeAndReplyOnSettingAssociationScopeEventAsync()
        {
            // given: the envelope states BOTH scopes, because the entity's Scope properties
            // are non-nullable and "not supplied" cannot be expressed on this path
            // BOTH stored scopes differ from both requested scopes. Leaving endpoint B equal
            // on the two sides would assert nothing about it: the reply would carry
            // AllVersions whether the service copied the request's value or never touched the
            // stored one, and deleting the EntityBScope write would fail no test in the suite.
            Association storageAssociation = CreateSubmittableStorageAssociation();
            storageAssociation.EntityAScope = Scope.AllVersions;
            storageAssociation.EntityBScope = Scope.AllVersions;

            var requestEnvelope = new EventEnvelope<Association>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = new Association
                {
                    Id = storageAssociation.Id,
                    EntityAScope = Scope.ThisVersionOnly,
                    EntityBScope = Scope.ThisVersionOnly
                },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupUnprocessedTransitionRequest(
                requestEnvelope,
                EventBrokerIdentifiers.AssociationOnSettingAssociationScopeSubscriptionName);

            SetupStorageRead(storageAssociation);
            SetupTransitionWriteBrokers();

            // no row occupies the key the toggle moves onto
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Association>().AsQueryable());

            // when
            EventEnvelope<Association>? actualReplyEnvelope =
                await this.associationService.OnSettingAssociationScopeAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.EntityAScope.Should().Be(Scope.ThisVersionOnly);
            actualReplyEnvelope.Content.EntityBScope.Should().Be(Scope.ThisVersionOnly);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Scoped),
                Times.Once);

            VerifyTransitionStorageCalls(
                requestEnvelope,
                EventBrokerIdentifiers.AssociationOnSettingAssociationScopeSubscriptionName,
                storageAssociation);

            // set-scope is the one transition that reads the collection: a scope toggle moves
            // the row's effective id, so it re-runs the pair-uniqueness check an add relies on
            // the index for
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private ValueTask<EventEnvelope<Association>?> InvokeTransitionHandlerAsync(
            string transitionName,
            EventEnvelope<Association> requestEnvelope)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            return transitionName switch
            {
                "Approve" => this.associationService.OnApprovingAssociationAsync(
                    requestEnvelope, cancellationToken),

                "SetConfidence" =>
                    this.associationService.OnSettingAssociationConfidenceAsync(
                        requestEnvelope, cancellationToken),

                _ => this.associationService.OnSettingAssociationScopeAsync(
                    requestEnvelope, cancellationToken)
            };
        }

        private void SetupUnprocessedTransitionRequest(
            EventEnvelope<Association> requestEnvelope,
            string receiverName) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    receiverName,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

        // the tail every transition shares: stamp, save, publish
        private void SetupTransitionWriteBrokers()
        {
            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<AssociationEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));
        }

        // The complete storage traffic a transition handler makes. Named exhaustively so the
        // VerifyNoOtherCalls epilogue in each test has something to be exhaustive ABOUT — an
        // unverified call would otherwise fail that epilogue rather than pass it silently.
        //
        // The two ProcessedEvents writes are the inbound request and the outbound fact: the
        // dual record is what lets a published fact looping back into a request handler be
        // recognised as already applied.
        private void VerifyTransitionStorageCalls(
            EventEnvelope<Association> requestEnvelope,
            string receiverName,
            Association storageAssociation)
        {
            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    receiverName,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    storageAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName == receiverName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }
    }
}
