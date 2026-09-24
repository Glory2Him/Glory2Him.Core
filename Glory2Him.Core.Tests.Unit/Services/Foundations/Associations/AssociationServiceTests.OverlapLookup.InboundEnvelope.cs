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

        public static TheoryData<string, SecurityContext, string> SignedCallersNotAllowedToContributeOnFindOverlap() =>
            new TheoryData<string, SecurityContext, string>
            {
                {
                    "unauthenticated",
                    new SecurityContext { IsAuthenticated = false },
                    "The current user is not authenticated."
                },
                {
                    "ReadOnly",
                    CreateAuthenticatedSecurityContext(Roles.ReadOnly),
                    "The current user is blocked from contributing content item associations."
                },
            };

        // The gate is the SIGNED caller's. The ambient caller is left authenticated and
        // unblocked, so a gate asked of it would pass and this refusal could not happen.
        [Theory]
        [MemberData(nameof(SignedCallersNotAllowedToContributeOnFindOverlap))]
        public async Task ShouldThrowUnauthorizedOnFindOverlapAsTheInboundEnvelopesCallerIfNotAllowedToContributeAsync(
            string signedCaller,
            SecurityContext signedSecurityContext,
            string expectedMessage)
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();

            EventEnvelope<Association> inboundEnvelope =
                CreateInboundEnvelopeCarryingAnotherPair(signedSecurityContext);

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(message: expectedMessage);

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<AssociationPairMatch> findTask =
                this.associationService.FindOverlappingAssociationAsync(
                    pairRequest,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualException =
                await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(
                expectedAssociationValidationException,
                because: $"a {signedCaller} signed caller may not probe");

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

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

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnFindOverlapAsTheInboundEnvelopesCallerIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();
            EventEnvelope<Association> inboundEnvelope = CreateInboundEnvelopeCarryingAnotherPair();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutAssociationException =
                new TimeoutAssociationException(
                    message: "Failed content item association timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedAssociationDependencyException =
                new AssociationDependencyException(
                    message: "Content item association dependency error occurred, contact support.",
                    innerException: timeoutAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<AssociationPairMatch> findTask =
                this.associationService.FindOverlappingAssociationAsync(
                    pairRequest,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationDependencyException>(findTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAssociationDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnFindOverlapAsTheInboundEnvelopesCallerIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();
            EventEnvelope<Association> inboundEnvelope = CreateInboundEnvelopeCarryingAnotherPair();
            SqlException sqlException = GetSqlException();

            var failedStorageAssociationException =
                new FailedStorageAssociationException(
                    message: "Failed content item association storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

            var expectedAssociationDependencyException =
                new AssociationDependencyException(
                    message: "Content item association dependency error occurred, contact support.",
                    innerException: failedStorageAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<AssociationPairMatch> findTask =
                this.associationService.FindOverlappingAssociationAsync(
                    pairRequest,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationDependencyException>(findTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAssociationDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedAssociationDependencyException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnFindOverlapAsTheInboundEnvelopesCallerIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Association pairRequest = CreateResolvedPairRequest();
            EventEnvelope<Association> inboundEnvelope = CreateInboundEnvelopeCarryingAnotherPair();
            var serviceException = new Exception();

            var failedAssociationServiceException =
                new FailedAssociationServiceException(
                    message: "Failed content item association service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedAssociationServiceException =
                new AssociationServiceException(
                    message: "Content item association service error occurred, contact support.",
                    innerException: failedAssociationServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectOverlappingAssociationAsync(
                    It.IsAny<EntityType>(), It.IsAny<EntityType>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Scope>(), It.IsAny<Scope>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<AssociationPairMatch> findTask =
                this.associationService.FindOverlappingAssociationAsync(
                    pairRequest,
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            AssociationServiceException actualException =
                await Assert.ThrowsAsync<AssociationServiceException>(findTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAssociationServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationServiceException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
