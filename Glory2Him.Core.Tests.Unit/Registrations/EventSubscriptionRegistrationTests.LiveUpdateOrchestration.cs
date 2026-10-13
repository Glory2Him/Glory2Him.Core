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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    // THE FORWARDER'S EARS (#900 onwards). Each reads the registration itself: the handler handed
    // to the broker for the address is captured and driven, so what is asserted is where a
    // delivery lands, not what a line of the registration happens to say.
    public partial class EventSubscriptionRegistrationTests
    {
        // Association-Approved brings a reaction into a count, so the forwarder hears it
        // (LiveUpdates.md rule 6). The id and the name are pinned as LITERALS, because a test that
        // reads the constant it guards cannot notice the constant change, and a new id orphans
        // the subscription the substrate already knows.
        [Fact]
        public async Task ShouldSubscribeTheLiveUpdatesToAssociationApproved()
        {
            // given: the delivery comes on a token of its own, distinct from the registration's,
            // so the handler is seen to hand the service the token of the fact it is serving
            var approvedSubscriptions = new List<EventSubscription>();
            CancellationToken registrationCancellationToken = TestContext.Current.CancellationToken;
            using var deliveryCancellationTokenSource = new CancellationTokenSource();
            CancellationToken deliveryCancellationToken = deliveryCancellationTokenSource.Token;

            Func<EventEnvelope<Association>, CancellationToken,
                ValueTask<EventEnvelope<Association>>> approvedHandler = null;

            this.eventBrokerMock.Setup(broker =>
                broker.SubscribeToAssociationEventAsync(
                    It.IsAny<EventSubscription>(),
                    AssociationEventOperation.Approved,
                    It.IsAny<Func<EventEnvelope<Association>, CancellationToken,
                        ValueTask<EventEnvelope<Association>>>>(),
                    registrationCancellationToken))
                        .Callback<EventSubscription, AssociationEventOperation,
                            Func<EventEnvelope<Association>, CancellationToken,
                                ValueTask<EventEnvelope<Association>>>,
                            CancellationToken>((subscription, _, handler, _) =>
                            {
                                approvedSubscriptions.Add(subscription);
                                approvedHandler = handler;
                            });

            var deliveredEnvelope = new EventEnvelope<Association>();

            await this.eventSubscriptionRegistration.RegisterAsync(registrationCancellationToken);

            EventSubscription approvedSubscription =
                approvedSubscriptions.Should().ContainSingle().Subject;

            // when
            await approvedHandler(deliveredEnvelope, deliveryCancellationToken);

            // then
            approvedSubscription.Id.Should().Be(new Guid("01a12ccf-d293-7867-b3d3-2acb7d944bbb"));

            approvedSubscription.Name.Should()
                .Be("LiveUpdateOrchestrationService.OnAssociationApproved");

            Guid approvedAddressId =
                EventBrokerIdentifiers.AssociationEventAddressIds[AssociationEventOperation.Approved];

            EventBrokerIdentifiers.AssociationEventAddresses[approvedAddressId]
                .Should().Be("Association-Approved");

            this.liveUpdateOrchestrationServiceMock.Verify(service =>
                service.OnAssociationApprovedAsync(
                    deliveredEnvelope,
                    deliveryCancellationToken),
                Times.Once);

            this.liveUpdateOrchestrationServiceMock.VerifyNoOtherCalls();
            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        // ContentItemSetting-Added switches something on or off for one item, so the forwarder
        // hears it (LiveUpdates.md rule 5). Pinned as literals for the same reason.
        [Fact]
        public async Task ShouldSubscribeTheLiveUpdatesToContentItemSettingAdded()
        {
            // given
            var addedSubscriptions = new List<EventSubscription>();
            CancellationToken registrationCancellationToken = TestContext.Current.CancellationToken;
            using var deliveryCancellationTokenSource = new CancellationTokenSource();
            CancellationToken deliveryCancellationToken = deliveryCancellationTokenSource.Token;

            Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                ValueTask<EventEnvelope<ContentItemSetting>>> addedHandler = null;

            this.eventBrokerMock.Setup(broker =>
                broker.SubscribeToContentItemSettingEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == new Guid("337b7647-25e3-46b1-9ef3-afe39fcd5c3e")
                            && subscription.Name ==
                                "LiveUpdateOrchestrationService.OnContentItemSettingAdded"),
                    ContentItemSettingEventOperation.Added,
                    It.Is<Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                        ValueTask<EventEnvelope<ContentItemSetting>>>>(handler =>
                            handler != null),
                    registrationCancellationToken))
                        .Callback<EventSubscription, ContentItemSettingEventOperation,
                            Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                                ValueTask<EventEnvelope<ContentItemSetting>>>,
                            CancellationToken>((subscription, _, handler, _) =>
                            {
                                addedSubscriptions.Add(subscription);
                                addedHandler = handler;
                            });

            var deliveredEnvelope = new EventEnvelope<ContentItemSetting>();

            await this.eventSubscriptionRegistration.RegisterAsync(registrationCancellationToken);

            EventSubscription addedSubscription =
                addedSubscriptions.Should().ContainSingle().Subject;

            // when
            await addedHandler(deliveredEnvelope, deliveryCancellationToken);

            // then
            addedSubscription.Id.Should().Be(new Guid("337b7647-25e3-46b1-9ef3-afe39fcd5c3e"));

            addedSubscription.Name.Should()
                .Be("LiveUpdateOrchestrationService.OnContentItemSettingAdded");

            Guid addedAddressId =
                EventBrokerIdentifiers.ContentItemSettingEventAddressIds[
                    ContentItemSettingEventOperation.Added];

            EventBrokerIdentifiers.ContentItemSettingEventAddresses[addedAddressId]
                .Should().Be("ContentItemSetting-Added");

            this.liveUpdateOrchestrationServiceMock.Verify(service =>
                service.OnContentItemSettingAddedAsync(
                    deliveredEnvelope,
                    deliveryCancellationToken),
                Times.Once);

            this.liveUpdateOrchestrationServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        // Association-Rejected takes an approved reaction out of a count where the entity/n        // overrides it, so the forwarder hears it (LiveUpdates.md rule 6). Pinned as literals for/n        // the same reason.
        [Fact]
        public async Task ShouldSubscribeTheLiveUpdatesToAssociationRejected()
        {
            // given
            var rejectedSubscriptions = new List<EventSubscription>();
            CancellationToken registrationCancellationToken = TestContext.Current.CancellationToken;
            using var deliveryCancellationTokenSource = new CancellationTokenSource();
            CancellationToken deliveryCancellationToken = deliveryCancellationTokenSource.Token;

            Func<EventEnvelope<Association>, CancellationToken,
                ValueTask<EventEnvelope<Association>>> rejectedHandler = null;

            this.eventBrokerMock.Setup(broker =>
                broker.SubscribeToAssociationEventAsync(
                    It.IsAny<EventSubscription>(),
                    AssociationEventOperation.Rejected,
                    It.IsAny<Func<EventEnvelope<Association>, CancellationToken,
                        ValueTask<EventEnvelope<Association>>>>(),
                    registrationCancellationToken))
                        .Callback<EventSubscription, AssociationEventOperation,
                            Func<EventEnvelope<Association>, CancellationToken,
                                ValueTask<EventEnvelope<Association>>>,
                            CancellationToken>((subscription, _, handler, _) =>
                            {
                                rejectedSubscriptions.Add(subscription);
                                rejectedHandler = handler;
                            });

            var deliveredEnvelope = new EventEnvelope<Association>();

            await this.eventSubscriptionRegistration.RegisterAsync(registrationCancellationToken);

            EventSubscription rejectedSubscription =
                rejectedSubscriptions.Should().ContainSingle().Subject;

            // when
            await rejectedHandler(deliveredEnvelope, deliveryCancellationToken);

            // then
            rejectedSubscription.Id.Should().Be(new Guid("50791ded-4a06-459d-a1ae-f7ea9e948bb7"));

            rejectedSubscription.Name.Should()
                .Be("LiveUpdateOrchestrationService.OnAssociationRejected");

            Guid rejectedAddressId =
                EventBrokerIdentifiers.AssociationEventAddressIds[AssociationEventOperation.Rejected];

            EventBrokerIdentifiers.AssociationEventAddresses[rejectedAddressId]
                .Should().Be("Association-Rejected");

            this.liveUpdateOrchestrationServiceMock.Verify(service =>
                service.OnAssociationRejectedAsync(
                    deliveredEnvelope,
                    deliveryCancellationToken),
                Times.Once);

            this.liveUpdateOrchestrationServiceMock.VerifyNoOtherCalls();
            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        // Association-Submitted takes an approved reaction out of a count until its round is/n        // decided again, so the forwarder hears it (LiveUpdates.md rule 6). The address carries the approval workflow's ear as well, and neither relies on
        // which runs first (§EVN26 rule 5), so both subscriptions are captured and each is driven:
        // the forwarder's lands on the forwarder, and the workflow's still lands on the workflow,
        // under the id and name it had. Pinned as literals for the same reason.
        [Fact]
        public async Task ShouldSubscribeTheLiveUpdatesToAssociationSubmitted()
        {
            // given
            var submittedHandlers = new Dictionary<Guid, (EventSubscription Subscription,
                Func<EventEnvelope<Association>, CancellationToken,
                    ValueTask<EventEnvelope<Association>>> Handler)>();

            CancellationToken registrationCancellationToken = TestContext.Current.CancellationToken;
            using var deliveryCancellationTokenSource = new CancellationTokenSource();
            CancellationToken deliveryCancellationToken = deliveryCancellationTokenSource.Token;
            var liveUpdateSubscriptionId = new Guid("c3f01354-adee-40aa-bf72-79722741c9c0");
            var approvalSubscriptionId = new Guid("01a0e17c-3a26-73a7-9e78-cf6cbb9a7d07");

            this.eventBrokerMock.Setup(broker =>
                broker.SubscribeToAssociationEventAsync(
                    It.IsAny<EventSubscription>(),
                    AssociationEventOperation.Submitted,
                    It.IsAny<Func<EventEnvelope<Association>, CancellationToken,
                        ValueTask<EventEnvelope<Association>>>>(),
                    registrationCancellationToken))
                        .Callback<EventSubscription, AssociationEventOperation,
                            Func<EventEnvelope<Association>, CancellationToken,
                                ValueTask<EventEnvelope<Association>>>,
                            CancellationToken>((subscription, _, handler, _) =>
                                submittedHandlers.Add(subscription.Id, (subscription, handler)));

            var liveUpdateEnvelope = new EventEnvelope<Association>();
            var approvalEnvelope = new EventEnvelope<Association>();

            await this.eventSubscriptionRegistration.RegisterAsync(registrationCancellationToken);

            submittedHandlers.Keys.Should().BeEquivalentTo(
                new[] { liveUpdateSubscriptionId, approvalSubscriptionId });

            // when
            await submittedHandlers[liveUpdateSubscriptionId].Handler(
                liveUpdateEnvelope, deliveryCancellationToken);

            await submittedHandlers[approvalSubscriptionId].Handler(
                approvalEnvelope, deliveryCancellationToken);

            // then
            submittedHandlers[liveUpdateSubscriptionId].Subscription.Name.Should()
                .Be("LiveUpdateOrchestrationService.OnAssociationSubmitted");

            submittedHandlers[approvalSubscriptionId].Subscription.Name.Should()
                .Be("ApprovalOrchestrationService.OnAssociationSubmitted");

            Guid submittedAddressId =
                EventBrokerIdentifiers.AssociationEventAddressIds[AssociationEventOperation.Submitted];

            EventBrokerIdentifiers.AssociationEventAddresses[submittedAddressId]
                .Should().Be("Association-Submitted");

            this.liveUpdateOrchestrationServiceMock.Verify(service =>
                service.OnAssociationSubmittedAsync(
                    liveUpdateEnvelope,
                    deliveryCancellationToken),
                Times.Once);

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.OnAssociationSubmittedAsync(
                    approvalEnvelope,
                    deliveryCancellationToken),
                Times.Once);

            this.liveUpdateOrchestrationServiceMock.VerifyNoOtherCalls();
            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        // Association-Removed takes a reaction out of its item's counts, softly or for good, so the
        // forwarder hears it (LiveUpdates.md rule 6). The approval workflow deliberately does not
        // (§APR9.7.6), so the forwarder's is the address's one subscription. Pinned as literals
        // for the same reason.
        [Fact]
        public async Task ShouldSubscribeTheLiveUpdatesToAssociationRemoved()
        {
            // given
            var removedSubscriptions = new List<EventSubscription>();
            CancellationToken registrationCancellationToken = TestContext.Current.CancellationToken;
            using var deliveryCancellationTokenSource = new CancellationTokenSource();
            CancellationToken deliveryCancellationToken = deliveryCancellationTokenSource.Token;

            Func<EventEnvelope<Association>, CancellationToken,
                ValueTask<EventEnvelope<Association>>> removedHandler = null;

            this.eventBrokerMock.Setup(broker =>
                broker.SubscribeToAssociationEventAsync(
                    It.IsAny<EventSubscription>(),
                    AssociationEventOperation.Removed,
                    It.IsAny<Func<EventEnvelope<Association>, CancellationToken,
                        ValueTask<EventEnvelope<Association>>>>(),
                    registrationCancellationToken))
                        .Callback<EventSubscription, AssociationEventOperation,
                            Func<EventEnvelope<Association>, CancellationToken,
                                ValueTask<EventEnvelope<Association>>>,
                            CancellationToken>((subscription, _, handler, _) =>
                            {
                                removedSubscriptions.Add(subscription);
                                removedHandler = handler;
                            });

            var deliveredEnvelope = new EventEnvelope<Association>();

            await this.eventSubscriptionRegistration.RegisterAsync(registrationCancellationToken);

            EventSubscription removedSubscription =
                removedSubscriptions.Should().ContainSingle().Subject;

            // when
            await removedHandler(deliveredEnvelope, deliveryCancellationToken);

            // then
            removedSubscription.Id.Should().Be(new Guid("b66e11c1-7878-4da0-a399-b3244e2614bc"));

            removedSubscription.Name.Should()
                .Be("LiveUpdateOrchestrationService.OnAssociationRemoved");

            Guid removedAddressId =
                EventBrokerIdentifiers.AssociationEventAddressIds[AssociationEventOperation.Removed];

            EventBrokerIdentifiers.AssociationEventAddresses[removedAddressId]
                .Should().Be("Association-Removed");

            this.liveUpdateOrchestrationServiceMock.Verify(service =>
                service.OnAssociationRemovedAsync(
                    deliveredEnvelope,
                    deliveryCancellationToken),
                Times.Once);

            this.liveUpdateOrchestrationServiceMock.VerifyNoOtherCalls();
            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        // Association-Restored brings an approved reaction back into a count with no approval behind
        // it, so the forwarder hears it (LiveUpdates.md rule 6). The approval workflow deliberately
        // does not (§ARC16.2.2), so the forwarder's is the address's one subscription. Pinned as
        // literals for the same reason.
        [Fact]
        public async Task ShouldSubscribeTheLiveUpdatesToAssociationRestored()
        {
            // given
            var restoredSubscriptions = new List<EventSubscription>();
            CancellationToken registrationCancellationToken = TestContext.Current.CancellationToken;
            using var deliveryCancellationTokenSource = new CancellationTokenSource();
            CancellationToken deliveryCancellationToken = deliveryCancellationTokenSource.Token;

            Func<EventEnvelope<Association>, CancellationToken,
                ValueTask<EventEnvelope<Association>>> restoredHandler = null;

            this.eventBrokerMock.Setup(broker =>
                broker.SubscribeToAssociationEventAsync(
                    It.IsAny<EventSubscription>(),
                    AssociationEventOperation.Restored,
                    It.IsAny<Func<EventEnvelope<Association>, CancellationToken,
                        ValueTask<EventEnvelope<Association>>>>(),
                    registrationCancellationToken))
                        .Callback<EventSubscription, AssociationEventOperation,
                            Func<EventEnvelope<Association>, CancellationToken,
                                ValueTask<EventEnvelope<Association>>>,
                            CancellationToken>((subscription, _, handler, _) =>
                            {
                                restoredSubscriptions.Add(subscription);
                                restoredHandler = handler;
                            });

            var deliveredEnvelope = new EventEnvelope<Association>();

            await this.eventSubscriptionRegistration.RegisterAsync(registrationCancellationToken);

            EventSubscription restoredSubscription =
                restoredSubscriptions.Should().ContainSingle().Subject;

            // when
            await restoredHandler(deliveredEnvelope, deliveryCancellationToken);

            // then
            restoredSubscription.Id.Should().Be(new Guid("a4d6fb22-26cf-4ee8-ad59-1be814252f2e"));

            restoredSubscription.Name.Should()
                .Be("LiveUpdateOrchestrationService.OnAssociationRestored");

            Guid restoredAddressId =
                EventBrokerIdentifiers.AssociationEventAddressIds[AssociationEventOperation.Restored];

            EventBrokerIdentifiers.AssociationEventAddresses[restoredAddressId]
                .Should().Be("Association-Restored");

            this.liveUpdateOrchestrationServiceMock.Verify(service =>
                service.OnAssociationRestoredAsync(
                    deliveredEnvelope,
                    deliveryCancellationToken),
                Times.Once);

            this.liveUpdateOrchestrationServiceMock.VerifyNoOtherCalls();
            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
