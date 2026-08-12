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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            var invalidApprovalId = Guid.Empty;

            var invalidApprovalException = new InvalidApprovalException(
                message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.UpsertDataList(
                key: nameof(Approval.Id),
                value: "Id is required");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalException);

            // when
            ValueTask<Approval> hardRemoveApprovalByIdTask =
                this.approvalService.HardRemoveApprovalByIdAsync(
                    invalidApprovalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    hardRemoveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfApprovalNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someApprovalId = Guid.NewGuid();
            Approval noApproval = null;

            var notFoundApprovalException = new NotFoundApprovalException(
                message: $"Approval not found with id: {someApprovalId}.");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noApproval);

            // when
            ValueTask<Approval> hardRemoveApprovalByIdTask =
                this.approvalService.HardRemoveApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    hardRemoveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Guid someApprovalId = Guid.NewGuid();

            var unauthorizedApprovalException = new UnauthorizedApprovalException(
                message: "The current user is not authenticated.");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalException);

            // when
            ValueTask<Approval> hardRemoveApprovalByIdTask =
                this.approvalService.HardRemoveApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    hardRemoveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            Guid someApprovalId = Guid.NewGuid();

            var unauthorizedApprovalException = new UnauthorizedApprovalException(
                message: "The current user is not allowed to permanently remove this approval.");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalException);

            // when
            ValueTask<Approval> hardRemoveApprovalByIdTask =
                this.approvalService.HardRemoveApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    hardRemoveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldBlockHardRemoveWhenTheCallerIsGloballyReadOnlyAndLogItAsync()
        {
            // given: the global ReadOnly ban outranks Admin, so a banned Admin is refused before
            // the row is even read — the destructive surface is not an exception to the site-wide
            // contribution ban.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin, Roles.ReadOnly);

            Guid someApprovalId = Guid.NewGuid();

            var unauthorizedApprovalException = new UnauthorizedApprovalException(
                message: "The current user is blocked from contributing approvals.");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalException);

            // when
            ValueTask<Approval> hardRemoveApprovalByIdTask =
                this.approvalService.HardRemoveApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    hardRemoveApprovalByIdTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
