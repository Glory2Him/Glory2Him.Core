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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    // THE ASSOCIATION-ADDING ADDRESS MOVED A TIER (#631). These read the registration itself: the
    // handler actually handed to the broker for that address is captured and driven, so what is
    // asserted is where a delivery lands, not what a line of the registration happens to say.
    public partial class EventSubscriptionRegistrationTests
    {
        // A DELIVERY TO ASSOCIATION-ADDING NEVER REACHES THE FOUNDATION DIRECTLY. While it did, an
        // add request entered DoAddAssociationAsync with the publisher's own Entity{A,B}ContentType
        // on the row, validated for enum-definedness only. The foundation's handler still exists —
        // the orchestration delegates to it — but the registration must not bind it.
        [Fact]
        public async Task ShouldNotBindTheFoundationToTheAddingAddress()
        {
            // given
            Func<EventEnvelope<Association>, CancellationToken,
                ValueTask<EventEnvelope<Association>?>> addingHandler = null;

            this.eventBrokerMock.Setup(broker =>
                broker.SubscribeToAssociationEventAsync(
                    It.IsAny<EventSubscription>(),
                    AssociationEventOperation.Adding,
                    It.IsAny<Func<EventEnvelope<Association>, CancellationToken,
                        ValueTask<EventEnvelope<Association>?>>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<EventSubscription, AssociationEventOperation,
                            Func<EventEnvelope<Association>, CancellationToken,
                                ValueTask<EventEnvelope<Association>?>>,
                            CancellationToken>((_, _, handler, _) => addingHandler = handler);

            var deliveredEnvelope = new EventEnvelope<Association>();

            await this.eventSubscriptionRegistration.RegisterAsync(
                TestContext.Current.CancellationToken);

            // when
            await addingHandler(deliveredEnvelope, TestContext.Current.CancellationToken);

            // then
            this.associationOrchestrationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    deliveredEnvelope,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.OnAddingAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
