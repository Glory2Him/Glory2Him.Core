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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            var invalidAssociationId = Guid.Empty;

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.UpsertDataList(
                key: nameof(Association.Id),
                value: "Id is required");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidAssociationException);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    invalidAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    hardRemoveAssociationByIdTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfAssociationNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someAssociationId = Guid.NewGuid();
            Association noAssociation = null;

            var notFoundAssociationException = new NotFoundAssociationException(
                message: $"Content item association not found with id: {someAssociationId}.");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: notFoundAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noAssociation);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    hardRemoveAssociationByIdTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Guid someAssociationId = Guid.NewGuid();

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is not authenticated.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    hardRemoveAssociationByIdTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteAssociationAsync(
                    It.IsAny<Association>(),
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
            Guid someAssociationId = Guid.NewGuid();

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is not allowed to permanently remove " +
                    "this content item association.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<Association> hardRemoveAssociationByIdTask =
                this.associationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    hardRemoveAssociationByIdTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteAssociationAsync(
                    It.IsAny<Association>(),
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
