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
    }
}
