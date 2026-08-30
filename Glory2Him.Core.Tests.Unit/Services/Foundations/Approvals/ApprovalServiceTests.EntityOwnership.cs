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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Force.DeepCloner;
using Glory2Him.Core.Models.Events.Foundations;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        /// <summary>
        /// The three ownership gates ask "is this caller the submitter", and on an approval that
        /// can only mean the author of the ENTITY. The workflow opens approval rows itself, so
        /// Approval.CreatedBy records the system and matches no human at all.
        ///
        /// <para>Each test below pins Approval.CreatedBy to the system sentinel and makes the
        /// actor the entity's author. Re-anchoring any gate on the approval's column turns these
        /// red — without the pinning they pass either way, because the fixtures hand both anchors
        /// the same string.</para>
        /// </summary>
        [Fact]
        public async Task ShouldAdmitTheEntityAuthorOnRetrieveByIdWithNoRolesAsync()
        {
            // given
            string authorUserId = GetRandomString();
            Approval randomApproval = CreateRandomApproval();
            randomApproval.IsDeleted = false;
            randomApproval.CreatedBy = SystemIdentity.UserId;
            Approval storageApproval = randomApproval;

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(authorUserId);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityAuthorAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(authorUserId);

            // when
            Approval actualApproval =
                await this.approvalService.RetrieveApprovalByIdAsync(
                    randomApproval.Id,
                    TestContext.Current.CancellationToken);

            // then: admitted as the submitter, holding no role at all
            actualApproval.Should().NotBeNull();

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEntityAuthorAsync(
                    storageApproval.EntityType,
                    storageApproval.EntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);
        }

        /// <summary>
        /// The token the CALLER supplied must reach the entity-author read, not
        /// default(CancellationToken). A dropped token means a cancelled request keeps a database
        /// round trip alive, and the compiler cannot catch it because the parameter is optional.
        /// </summary>
        [Fact]
        public async Task ShouldPassTheCallersTokenToTheEntityAuthorReadOnRetrieveByIdAsync()
        {
            // given
            string authorUserId = GetRandomString();
            Approval randomApproval = CreateRandomApproval();
            randomApproval.IsDeleted = false;
            randomApproval.CreatedBy = SystemIdentity.UserId;

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken callersToken = cancellationTokenSource.Token;

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(authorUserId);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityAuthorAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(authorUserId);

            // when
            await this.approvalService.RetrieveApprovalByIdAsync(
                randomApproval.Id,
                callersToken);

            // then
            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEntityAuthorAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    callersToken),
                        Times.Once);
        }

        /// <summary>
        /// The entity-author read is a database round trip inside a gate, and the two ways it can
        /// fail to answer look IDENTICAL at the catch site: both raise OperationCanceledException.
        /// What separates them is whose token was cancelled. This is the TIMEOUT half — the
        /// dependency gave up on its own, the caller is still waiting, and it categorises as a
        /// dependency failure the caller can be told about.
        /// </summary>
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfTheEntityAuthorReadTimesOutAsync()
        {
            // given
            Approval someApproval = CreateRandomApproval();
            someApproval.IsDeleted = false;
            someApproval.CreatedBy = SystemIdentity.UserId;

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            // NOT tied to the caller's token, which is what makes it a timeout rather than a
            // cancellation - the service tells them apart on IsCancellationRequested alone.
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
                broker.SelectApprovalByIdAsync(
                    someApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(someApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityAuthorAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Approval> retrieveTask =
                this.approvalService.RetrieveApprovalByIdAsync(
                    someApproval.Id,
                    TestContext.Current.CancellationToken);

            ApprovalDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalDependencyException>(retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalDependencyException))),
                Times.Once);
        }

        /// <summary>
        /// The CANCELLATION half of the same exception. The caller's own token was cancelled, so
        /// nothing failed and there is nobody left to tell — it propagates unwrapped rather than
        /// being dressed up as a dependency fault the caller would then log and alert on.
        /// </summary>
        [Fact]
        public async Task ShouldPropagateCancellationOnRetrieveByIdIfTheEntityAuthorReadIsCancelledAsync()
        {
            // given
            Approval someApproval = CreateRandomApproval();
            someApproval.IsDeleted = false;
            someApproval.CreatedBy = SystemIdentity.UserId;

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();
            CancellationToken cancelledToken = cancellationTokenSource.Token;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    someApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(someApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityAuthorAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new OperationCanceledException(cancelledToken));

            // when
            ValueTask<Approval> retrieveTask =
                this.approvalService.RetrieveApprovalByIdAsync(
                    someApproval.Id,
                    cancelledToken);

            // then: raw, not wrapped in a dependency exception
            await Assert.ThrowsAnyAsync<OperationCanceledException>(retrieveTask.AsTask);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The remove gate is owner-OR-ADMIN, so a plain author reaching it proves the anchor on
        /// its own: no role can be carrying them. Approval.CreatedBy is pinned to the system, as
        /// the workflow really writes it, so re-anchoring the gate there turns this red.
        /// </summary>
        [Fact]
        public async Task ShouldAdmitTheEntityAuthorOnRemoveByIdWithNoRolesAsync()
        {
            // given
            string authorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Approval randomApproval = CreateRandomApproval();
            randomApproval.IsDeleted = false;
            randomApproval.CreatedBy = SystemIdentity.UserId;
            Approval storageApproval = randomApproval;
            Approval auditAppliedApproval = storageApproval.DeepClone();
            auditAppliedApproval.IsDeleted = true;

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(authorUserId);

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityAuthorAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(authorUserId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<string>()))
                        .ReturnsAsync(auditAppliedApproval);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(auditAppliedApproval);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    It.IsAny<ApprovalEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Approval>>(
                            new EventPublishResult<Approval>()));

            // when
            Approval actualApproval =
                await this.approvalService.RemoveApprovalByIdAsync(
                    randomApproval.Id,
                    GetRandomString(),
                    TestContext.Current.CancellationToken);

            // then: admitted as the CONTENT's author, holding neither Administrators nor any other role
            actualApproval.Should().NotBeNull();

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveEntityAuthorAsync(
                    storageApproval.EntityType,
                    storageApproval.EntityId,
                    It.IsAny<CancellationToken>()),
                        Times.Once);
        }

    }
}
