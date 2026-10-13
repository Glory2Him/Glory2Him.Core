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

        // ContentItemSetting-Modified amends an override or a content type's default, so the
        // forwarder hears it (LiveUpdates.md rule 5). Pinned as literals for the same reason.
        [Fact]
        public async Task ShouldSubscribeTheLiveUpdatesToContentItemSettingModified()
        {
            // given
            var modifiedSubscriptions = new List<EventSubscription>();
            CancellationToken registrationCancellationToken = TestContext.Current.CancellationToken;
            using var deliveryCancellationTokenSource = new CancellationTokenSource();
            CancellationToken deliveryCancellationToken = deliveryCancellationTokenSource.Token;

            Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                ValueTask<EventEnvelope<ContentItemSetting>>> modifiedHandler = null;

            this.eventBrokerMock.Setup(broker =>
                broker.SubscribeToContentItemSettingEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == new Guid("04949f46-790d-4413-acd5-5ea363df7701")
                            && subscription.Name ==
                                "LiveUpdateOrchestrationService.OnContentItemSettingModified"),
                    ContentItemSettingEventOperation.Modified,
                    It.Is<Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                        ValueTask<EventEnvelope<ContentItemSetting>>>>(handler =>
                            handler != null),
                    registrationCancellationToken))
                        .Callback<EventSubscription, ContentItemSettingEventOperation,
                            Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                                ValueTask<EventEnvelope<ContentItemSetting>>>,
                            CancellationToken>((subscription, _, handler, _) =>
                            {
                                modifiedSubscriptions.Add(subscription);
                                modifiedHandler = handler;
                            });

            var deliveredEnvelope = new EventEnvelope<ContentItemSetting>();

            await this.eventSubscriptionRegistration.RegisterAsync(registrationCancellationToken);

            EventSubscription modifiedSubscription =
                modifiedSubscriptions.Should().ContainSingle().Subject;

            // when
            await modifiedHandler(deliveredEnvelope, deliveryCancellationToken);

            // then
            modifiedSubscription.Id.Should().Be(new Guid("04949f46-790d-4413-acd5-5ea363df7701"));

            modifiedSubscription.Name.Should()
                .Be("LiveUpdateOrchestrationService.OnContentItemSettingModified");

            Guid modifiedAddressId =
                EventBrokerIdentifiers.ContentItemSettingEventAddressIds[
                    ContentItemSettingEventOperation.Modified];

            EventBrokerIdentifiers.ContentItemSettingEventAddresses[modifiedAddressId]
                .Should().Be("ContentItemSetting-Modified");

            this.liveUpdateOrchestrationServiceMock.Verify(service =>
                service.OnContentItemSettingModifiedAsync(
                    deliveredEnvelope,
                    deliveryCancellationToken),
                Times.Once);

            this.liveUpdateOrchestrationServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingServiceMock.VerifyNoOtherCalls();
            this.contentItemSettingOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
