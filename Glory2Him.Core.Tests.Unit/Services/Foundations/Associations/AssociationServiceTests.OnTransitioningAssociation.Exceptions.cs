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
using Glory2Him.Core.Models.Foundations.Associations;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnApprovingAssociationEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Association>?> onApprovingTask =
                this.associationService.OnApprovingAssociationAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onApprovingTask.AsTask);

            // approve is the one verb here that consults the access broker — it reaches
            // MayDecideApprovalAsync through the shared transition do-work, so the guard has to be
            // proven to fire ahead of that too. The two Setting verbs below take different
            // do-work with no access gate, which is why they do not assert it.
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnSettingAssociationConfidenceEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Association>?> onSettingConfidenceTask =
                this.associationService.OnSettingAssociationConfidenceAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onSettingConfidenceTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnSettingAssociationScopeEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Association> requestEnvelope = CreateRandomAssociationRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Association>?> onSettingScopeTask =
                this.associationService.OnSettingAssociationScopeAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onSettingScopeTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
