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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // THE ADDING HANDLER'S OWN DEDUPLICATION QUESTION, asked on its own (#631). Same receiver
        // name and same storage probe OnAddingAssociationAsync uses, so the orchestration asking
        // first and the handler asking again cannot answer differently — a repeated read, not a
        // second rule. Both answers are pinned, so a constant cannot pass.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldAnswerWhetherTheAddingRequestWasAlreadyAppliedAsync(
            bool alreadyProcessed)
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyProcessed);

            // when
            bool actualAnswer =
                await this.associationService.HasAlreadyAddedAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualAnswer.Should().Be(alreadyProcessed);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.AssociationOnAddingAssociationSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // #631 criterion 5b. Asked by the orchestration AHEAD of every endpoint read, so a caller
        // that has already cancelled must not pay for the storage round trip either.
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnHasAlreadyAddedAssociationIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<bool> hasAlreadyAddedTask =
                this.associationService.HasAlreadyAddedAssociationAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                hasAlreadyAddedTask.AsTask);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
