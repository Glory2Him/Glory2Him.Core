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
using Force.DeepCloner;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Exceptions;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        /// <summary>
        /// §10.19, reached the only way an association can reach it. Association has no submit
        /// verb, so <c>Association-Submitted</c> is published solely as the fallback arm of the
        /// decision switch — an administrator re-opening a terminal row (§8.6 HR-4). It is still
        /// a REQUIRED delivery: it reaches
        /// <c>ApprovalOrchestrationService.OnAssociationSubmittedAsync</c>, which moves the
        /// association's approval to Submitted and re-evaluates the round.
        ///
        /// <para>That arm is also why the inspection in the tail is unconditional rather than
        /// written against the operation. The address here is not chosen by the verb the caller
        /// invoked — it is derived from the decided status — so a condition would have to
        /// re-derive the switch to stay correct, and would be wrong the moment it drifted.</para>
        /// </summary>
        [Fact]
        public async Task ShouldLogCriticalWhenTheSubmittedFactDeliveryFailsAsync()
        {
            // given: the administrator override out of a terminal state, which re-opens the round
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Association storageAssociation =
                CreateTerminalStorageAssociation(ApprovalStatus.Approved);

            Association inputAssociation = CreateReopenDecision(storageAssociation.Id);

            var failedPublishResult = new EventPublishResult<Association>
            {
                EventId = Guid.NewGuid(),
                Deliveries = new List<EventDelivery<Association>>
                {
                    new EventDelivery<Association>
                    {
                        SubscriptionId = Guid.NewGuid(),
                        IsSuccess = false,
                        IsFailure = true,
                        Status = "Error",
                        ResponseCode = "500",
                        ResponseMessage = "the handler failed",
                    },
                },
            };

            SetupStorageRead(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            Association savedAssociation = null;

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Association, CancellationToken>(
                            (entity, _) => savedAssociation = entity.DeepClone())
                        .ReturnsAsync((Association entity, CancellationToken _) =>
                            entity.DeepClone());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            failedPublishResult));

            // when: the re-open itself SUCCEEDS — the row is committed before the fact goes out
            await this.associationService.TransitionAssociationApprovalAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then: the re-open COMMITTED. The six sibling entities assert this through the
            // returned row; Association's transition is reached through the decision switch, so
            // it is asserted on what reached storage. Without it a regression that logged the
            // contained failure but skipped or altered the write would still pass, and §10.19's
            // whole premise is that the write stands and only its fact went astray.
            savedAssociation.Should().NotBeNull();
            savedAssociation.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    It.Is<Association>(association =>
                        association.Id == storageAssociation.Id
                            && association.ApprovalStatus == ApprovalStatus.Submitted),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and: the contained failure is reported rather than dropped
            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(
                        FailedEventDeliveryException.ForFailedDeliveries(
                            failedPublishResult,
                            AssociationEventOperation.Submitted)))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
