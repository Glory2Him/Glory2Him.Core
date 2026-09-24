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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        // THE OVERLAP PROBE ON THE ASSOCIATION-ADDING EVENT PATH (#631 criterion 2b), the twin of
        // the pair probe's: the gate is the envelope's signed caller's, nothing is minted, and
        // the key comes from the association argument — not from the envelope's content, which is
        // deliberately a different pair.
        [Fact]
        public async Task ShouldProbeTheOverlapAsTheInboundEnvelopesCallerAsync()
        {
            // given
            Association incoming = CreateResolvedPairRequest();

            Association storageRow = CreateStoredRowForPair(
                incoming, ApprovalStatus.Approved, isDeleted: false);

            EventEnvelope<Association> inboundEnvelope =
                CreateInboundEnvelopeCarryingAnotherPair();

            // THE AMBIENT CALLER IS NOBODY: a gate asked of it would refuse
            this.ambientSecurityContext = new SecurityContext();
            SetupOverlapProbe(storageRow);

            // when
            AssociationPairMatch actualMatch =
                await this.associationService.FindOverlappingAssociationAsync(
                    incoming,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().NotBeNull();
            actualMatch.Id.Should().Be(storageRow.Id);

            this.capturedOverlapProbeKey.Should().Be(new OverlapProbeKey(
                incoming.EntityAType,
                incoming.EntityBType,
                incoming.UserId,
                incoming.EntityAGroupId,
                incoming.EntityBGroupId,
                incoming.EntityAScope,
                incoming.EntityBScope,
                incoming.EntityAGroupId,
                incoming.EntityBKeyId,
                ExcludedAssociationId: null));

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<Association>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<Association>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnFindOverlapAsTheInboundEnvelopesCallerIfCancellationRequestedAsync()
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();
            EventEnvelope<Association> inboundEnvelope = CreateInboundEnvelopeCarryingAnotherPair();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<AssociationPairMatch> findTask =
                this.associationService.FindOverlappingAssociationAsync(
                    pairRequest,
                    inboundEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(findTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
