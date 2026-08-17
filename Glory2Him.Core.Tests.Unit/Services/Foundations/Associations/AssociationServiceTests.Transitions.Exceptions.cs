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
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Theory]
        [MemberData(nameof(TransitionNames))]
        public async Task ShouldThrowDependencyExceptionOnTransitionIfSqlErrorOccursAndLogItAsync(
            string transitionName)
        {
            // given: a storage failure on the load every transition performs
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Admin, Roles.Publisher, Roles.Reviewer);

            Association storageAssociation = CreateApprovableStorageAssociation();
            SqlException sqlException = GetSqlException();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when / then
            await Assert.ThrowsAsync<AssociationDependencyException>(async () =>
                await InvokeTransitionAsync(transitionName, storageAssociation));

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogCriticalAsync(It.IsAny<Xeption>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(TransitionNames))]
        public async Task ShouldThrowServiceExceptionOnTransitionIfServiceErrorOccursAndLogItAsync(
            string transitionName)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Admin, Roles.Publisher, Roles.Reviewer);

            Association storageAssociation = CreateApprovableStorageAssociation();
            var serviceException = new Exception();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when / then
            await Assert.ThrowsAsync<AssociationServiceException>(async () =>
                await InvokeTransitionAsync(transitionName, storageAssociation));

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogErrorAsync(It.IsAny<Xeption>()),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(TransitionNames))]
        public async Task ShouldThrowTimeoutDependencyExceptionOnTransitionIfOperationTimesOutAsync(
            string transitionName)
        {
            // given: a cancellation NOT asked for by the caller is a timeout in the dependency,
            // not a caller cancellation — the two are distinguished by whether the token that
            // surfaced is the one the caller handed in
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Admin, Roles.Publisher, Roles.Reviewer);

            Association storageAssociation = CreateApprovableStorageAssociation();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new OperationCanceledException());

            // when / then
            await Assert.ThrowsAsync<AssociationDependencyException>(async () =>
                await InvokeTransitionAsync(transitionName, storageAssociation));

            this.loggingBrokerMock.Verify(broker =>
                    broker.LogErrorAsync(It.IsAny<Xeption>()),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(TransitionNames))]
        public async Task ShouldThrowOperationCanceledOnTransitionIfTheCallerCancelledAsync(
            string transitionName)
        {
            // given: a token the caller already cancelled. Every transition checks it before
            // doing anything, so nothing is read and nothing is written.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Admin, Roles.Publisher, Roles.Reviewer);

            Association storageAssociation = CreateApprovableStorageAssociation();

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // when / then
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await InvokeCancelledTransitionAsync(
                    transitionName,
                    storageAssociation,
                    cancellationTokenSource.Token));

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectAssociationByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private ValueTask<Association> InvokeCancelledTransitionAsync(
            string transitionName,
            Association storageAssociation,
            CancellationToken cancellationToken) =>
            transitionName switch
            {

                "Approve" => this.associationService.TransitionAssociationApprovalAsync(
                    CreateApprovalDecision(storageAssociation.Id), cancellationToken),

                // the bypass verb has its own arm for the same reason every other name does:
                // without one it falls through to the default and this theory row would drive
                // set-scope while claiming to cover the bypass
                "BypassApprove" => this.associationService.TransitionAssociationApprovalAsync(
                    CreateBypassApprovalDecision(storageAssociation.Id),
                    cancellationToken),

                "Sort" => this.associationService.SortAssociationAsync(
                    new Association { Id = storageAssociation.Id },
                    new Association { Id = Guid.NewGuid() },
                    Glory2Him.Core.Models.Enums.SortPosition.After,
                    cancellationToken),

                "SetConfidence" => this.associationService.SetAssociationConfidenceAsync(
                    CreateConfidenceDecision(storageAssociation.Id), cancellationToken),

                _ => this.associationService.SetAssociationScopeAsync(
                    storageAssociation.Id,
                    storageAssociation.EntityAScope,
                    storageAssociation.EntityBScope,
                    cancellationToken)
            };
    }
}
