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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        // The probed key, pinned rather than drawn. Link is deliberately NOT the zero member —
        // a defaulted EntityType would be ContentItem and would match by accident, hiding a
        // dropped EntityType conjunct.
        private const EntityType LookupProbeEntityType = EntityType.Link;

        // A DIFFERENT non-zero member, for the row that shares the entity id but not the type.
        private const EntityType LookupOtherEntityType = EntityType.Comment;

        private static readonly Guid LookupProbeEntityId =
            Guid.Parse("6b1f0d9a-3c47-4e21-9a55-0f2d7c4b8e13");

        private static readonly Guid LookupOtherEntityId =
            Guid.Parse("c8a52e74-91b6-4d03-8f27-5ad1e6903b4c");

        // A stored row on a caller-chosen key. Every field the projection carries is passed in
        // so no assertion can pass on a value the filler happened to draw.
        private static Approval CreateLookupStorageApproval(
            Guid approvalId,
            EntityType entityType,
            Guid entityId,
            ApprovalStatus approvalStatus,
            bool isDeleted,
            DateTimeOffset updatedWhen)
        {
            Approval approval = CreateRandomApproval();
            approval.Id = approvalId;
            approval.EntityType = entityType;
            approval.EntityId = entityId;
            approval.ApprovalStatus = approvalStatus;
            approval.IsDeleted = isDeleted;
            approval.UpdatedWhen = updatedWhen;

            return approval;
        }

        /// <summary>
        /// What the service still owns: it hands the storage layer the key it was given, and it
        /// projects the row it gets back.
        ///
        /// <para>The PREDICATE and the ORDERING moved into
        /// <c>IStorageBroker.SelectApprovalByEntityAsync</c> so the probe could be awaited with
        /// the caller's token instead of executed by a synchronous terminal operator. Which row
        /// they pick — the unfiltered match that lets a tombstone occupy the key, the half-key
        /// non-match, the live-before-deleted preference — is a question about SQL now, and is
        /// answered against a real catalogue in <c>ApprovalEntityProbeReadTests</c>.</para>
        /// </summary>
        [Fact]
        public async Task ShouldProbeTheGivenKeyAndProjectTheRowItOccupiesAsync()
        {
            // given
            var expectedMatchId = Guid.Parse("2f0c1b8d-7a34-4c96-b0e5-13d8f92a6c47");

            Approval storageApproval = CreateLookupStorageApproval(
                approvalId: expectedMatchId,
                entityType: LookupProbeEntityType,
                entityId: LookupProbeEntityId,
                approvalStatus: ApprovalStatus.Approved,
                isDeleted: false,
                updatedWhen: GetRandomDateTimeOffset());

            // keyed on the probed values, so a service that passed anything else through would
            // fall to the unstubbed default and fail on the projection below
            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByEntityAsync(
                    LookupProbeEntityType, LookupProbeEntityId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            // when
            ApprovalEntityMatch? actualMatch =
                await this.approvalService.FindApprovalByEntityAsync(
                    LookupProbeEntityType,
                    LookupProbeEntityId,
                    TestContext.Current.CancellationToken);

            // then: the projection only - id, status and the soft-delete flag, no row body
            actualMatch.Should().NotBeNull();
            actualMatch!.Id.Should().Be(expectedMatchId);
            actualMatch.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
            actualMatch.IsDeleted.Should().BeFalse();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    LookupProbeEntityType, LookupProbeEntityId, It.IsAny<CancellationToken>()),
                Times.Once);

            // the probe is a read: it neither consults the cross-entity amendment decision
            // nor writes, stamps or publishes anything
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// A free key answers null rather than throwing: the caller inserts on it, and a
        /// not-found error here would turn "nothing to reinstate" into a failure.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNullWhenTheKeyIsUnoccupiedAsync()
        {
            // given
            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Approval?)null);

            // when
            ApprovalEntityMatch? actualMatch =
                await this.approvalService.FindApprovalByEntityAsync(
                    LookupProbeEntityType,
                    LookupProbeEntityId,
                    TestContext.Current.CancellationToken);

            // then
            actualMatch.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The token reaches the database call, which is the point of the narrow read: the shape
        /// it replaced threaded a token through every method here and then ran the query without
        /// it.
        /// </summary>
        [Fact]
        public async Task ShouldPassTheCancellationTokenToTheStorageBrokerOnFindByEntityAsync()
        {
            // given
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Approval?)null);

            // when
            await this.approvalService.FindApprovalByEntityAsync(
                LookupProbeEntityType,
                LookupProbeEntityId,
                inputCancellationToken);

            // then
            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    LookupProbeEntityType, LookupProbeEntityId, inputCancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindByEntityIfEntityIdIsInvalidAndLogItAsync()
        {
            // given: an unresolved subject would key the probe off Guid.Empty and report every
            // such key as free, inviting an insert that collides with whatever really occupies it
            var invalidEntityId = Guid.Empty;

            var invalidApprovalException = new InvalidApprovalException(
                message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.UpsertDataList(
                key: nameof(Approval.EntityId),
                value: "Id is required");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalException);

            // when
            ValueTask<ApprovalEntityMatch?> findApprovalByEntityTask =
                this.approvalService.FindApprovalByEntityAsync(
                    LookupProbeEntityType,
                    invalidEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    findApprovalByEntityTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindByEntityIfEntityTypeIsInvalidAndLogItAsync()
        {
            // given: an undefined member is not a key any index can be probed on
            var invalidEntityType = (EntityType)999;

            var invalidApprovalException = new InvalidApprovalException(
                message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.UpsertDataList(
                key: nameof(Approval.EntityType),
                value: "Value is not a supported entity type");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalException);

            // when
            ValueTask<ApprovalEntityMatch?> findApprovalByEntityTask =
                this.approvalService.FindApprovalByEntityAsync(
                    invalidEntityType,
                    LookupProbeEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    findApprovalByEntityTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnFindByEntityIfSqlErrorOccursAndLogItAsync()
        {
            // given
            SqlException sqlException = GetSqlException();

            var failedStorageApprovalException = new FailedStorageApprovalException(
                message: "Failed approval storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedApprovalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: failedStorageApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(sqlException);

            // when
            ValueTask<ApprovalEntityMatch?> findApprovalByEntityTask =
                this.approvalService.FindApprovalByEntityAsync(
                    LookupProbeEntityType,
                    LookupProbeEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    findApprovalByEntityTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyException))),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnFindByEntityIfServiceErrorOccursAndLogItAsync()
        {
            // given
            var serviceException = new Exception();

            // the probe's own wording — the read overload says "contact support." where the
            // write overloads say "please contact support."
            var failedApprovalServiceException = new FailedApprovalServiceException(
                message: "Failed approval service error occurred, contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedApprovalServiceException = new ApprovalServiceException(
                message: "Approval service error occurred, contact support.",
                innerException: failedApprovalServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<ApprovalEntityMatch?> findApprovalByEntityTask =
                this.approvalService.FindApprovalByEntityAsync(
                    LookupProbeEntityType,
                    LookupProbeEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalServiceException actualApprovalServiceException =
                await Assert.ThrowsAsync<ApprovalServiceException>(
                    findApprovalByEntityTask.AsTask);

            // then
            actualApprovalServiceException.Should().BeEquivalentTo(
                expectedApprovalServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalServiceException))),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnFindByEntityIfCancellationRequestedAsync()
        {
            // given: the probe checks the token before it builds the request envelope
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ApprovalEntityMatch?> findApprovalByEntityTask =
                this.approvalService.FindApprovalByEntityAsync(
                    LookupProbeEntityType,
                    LookupProbeEntityId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                findApprovalByEntityTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRethrowOperationCanceledExceptionOnFindByEntityWhenItsTokenIsCancelledAsync()
        {
            // given: a genuine cancellation surfacing from storage. It carries a token that IS
            // cancelled, so the timeout guard clause declines it and the plain rethrow catches
            // it — cancellation must reach the caller unwrapped and unlogged, never dressed up
            // as a dependency failure. The probe is called with a LIVE token so the entry guard
            // does not fire first.
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var operationCanceledException =
                new OperationCanceledException(cancellationTokenSource.Token);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalEntityMatch?> findApprovalByEntityTask =
                this.approvalService.FindApprovalByEntityAsync(
                    LookupProbeEntityType,
                    LookupProbeEntityId,
                    TestContext.Current.CancellationToken);

            OperationCanceledException actualOperationCanceledException =
                await Assert.ThrowsAsync<OperationCanceledException>(
                    findApprovalByEntityTask.AsTask);

            // then: the very same instance, not a re-created or wrapped one
            actualOperationCanceledException.Should().BeSameAs(operationCanceledException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnFindByEntityIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given: no token was cancelled, so this cancellation is the store giving up — a
            // timeout, which categorizes as a dependency failure rather than reaching the caller
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutApprovalException =
                new TimeoutApprovalException(
                    message: "Failed approval timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedApprovalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: timeoutApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ApprovalEntityMatch?> findApprovalByEntityTask =
                this.approvalService.FindApprovalByEntityAsync(
                    LookupProbeEntityType,
                    LookupProbeEntityId,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualApprovalDependencyException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(
                    findApprovalByEntityTask.AsTask);

            // then
            actualApprovalDependencyException.Should().BeEquivalentTo(
                expectedApprovalDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyException))),
                Times.Once);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
