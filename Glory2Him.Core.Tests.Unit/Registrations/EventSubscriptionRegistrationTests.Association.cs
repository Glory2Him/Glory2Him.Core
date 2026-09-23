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
using Glory2Him.Core.Services.Foundations.Associations;
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

        // ONLY THE HANDLER MOVED. The event name "AssociationAdding" is inside the HMAC, so a
        // renamed address breaks every signature published to it; the id is what the substrate
        // knows this subscription by, so a new one orphans it; and the name is the foundation's
        // ProcessedEvents receiver key, so a new one forgets every event already applied. Pinned
        // as LITERALS rather than through the constants, because a test that reads the constant
        // it guards cannot notice the constant change.
        [Fact]
        public async Task ShouldKeepTheSubscriptionIdentityUnchanged()
        {
            // given
            var addingSubscriptions = new List<EventSubscription>();

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
                            CancellationToken>((subscription, _, _, _) =>
                                addingSubscriptions.Add(subscription));

            // when
            await this.eventSubscriptionRegistration.RegisterAsync(
                TestContext.Current.CancellationToken);

            // then
            EventSubscription addingSubscription = addingSubscriptions.Should().ContainSingle().Subject;
            addingSubscription.Id.Should().Be(new Guid("019f8170-a642-7cec-bc2e-da65a18d6c88"));
            addingSubscription.Name.Should().Be("AssociationService.OnAddingAssociation");

            Guid addingAddressId =
                EventBrokerIdentifiers.AssociationEventAddressIds[AssociationEventOperation.Adding];

            EventBrokerIdentifiers.AssociationEventAddresses[addingAddressId]
                .Should().Be("Association-Adding");

            $"{nameof(Association)}{AssociationEventOperation.Adding}"
                .Should().Be("AssociationAdding");
        }

        // THE OTHER SEVEN STAY WHERE THEY WERE. None of them derives anything — each works from
        // columns already on the stored row — so moving any of them would add a layer that only
        // forwards (§ARC12.1). Set-scope is the nearest neighbour: it re-runs the add's duplicate
        // check, but it recomputes the effective id from the stored row rather than resolving an
        // endpoint. Every Association handler the registration hands the broker is DRIVEN, and
        // the set of request operations that lands on the foundation must be exactly these seven.
        [Fact]
        public async Task ShouldLeaveTheRemainingSevenAssociationAddressesOnTheFoundationAsync()
        {
            // given
            var expectedFoundationBindings = new Dictionary<AssociationEventOperation, string>
            {
                { AssociationEventOperation.Modifying, nameof(IAssociationService.OnModifyingAssociationAsync) },
                { AssociationEventOperation.RemovingById, nameof(IAssociationService.OnRemovingAssociationByIdAsync) },
                { AssociationEventOperation.HardRemovingById, nameof(IAssociationService.OnHardRemovingAssociationByIdAsync) },
                { AssociationEventOperation.RetrievingById, nameof(IAssociationService.OnRetrievingAssociationByIdAsync) },
                { AssociationEventOperation.Approving, nameof(IAssociationService.OnApprovingAssociationAsync) },
                { AssociationEventOperation.SettingConfidence, nameof(IAssociationService.OnSettingAssociationConfidenceAsync) },
                { AssociationEventOperation.SettingScope, nameof(IAssociationService.OnSettingAssociationScopeAsync) },
            };

            var subscribedHandlers =
                new List<(AssociationEventOperation Operation,
                    Func<EventEnvelope<Association>, CancellationToken,
                        ValueTask<EventEnvelope<Association>?>> Handler)>();

            this.eventBrokerMock.Setup(broker =>
                broker.SubscribeToAssociationEventAsync(
                    It.IsAny<EventSubscription>(),
                    It.IsAny<AssociationEventOperation>(),
                    It.IsAny<Func<EventEnvelope<Association>, CancellationToken,
                        ValueTask<EventEnvelope<Association>?>>>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<EventSubscription, AssociationEventOperation,
                            Func<EventEnvelope<Association>, CancellationToken,
                                ValueTask<EventEnvelope<Association>?>>,
                            CancellationToken>((_, operation, handler, _) =>
                                subscribedHandlers.Add((operation, handler)));

            await this.eventSubscriptionRegistration.RegisterAsync(
                TestContext.Current.CancellationToken);

            var actualFoundationBindings = new Dictionary<AssociationEventOperation, string>();

            // when
            foreach ((AssociationEventOperation operation,
                Func<EventEnvelope<Association>, CancellationToken,
                    ValueTask<EventEnvelope<Association>?>> handler) in subscribedHandlers)
            {
                this.associationServiceMock.Invocations.Clear();
                await handler(new EventEnvelope<Association>(), TestContext.Current.CancellationToken);

                foreach (IInvocation invocation in this.associationServiceMock.Invocations)
                {
                    actualFoundationBindings.Add(operation, invocation.Method.Name);
                }
            }

            // then
            actualFoundationBindings.Should().HaveCount(7);
            actualFoundationBindings.Should().BeEquivalentTo(expectedFoundationBindings);
        }
    }
}
