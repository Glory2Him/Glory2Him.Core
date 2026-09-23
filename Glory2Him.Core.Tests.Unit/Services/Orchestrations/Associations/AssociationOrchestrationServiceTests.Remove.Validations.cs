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
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowUnauthorizedOnRemoveBeforeReadingTheAssociationIfCallerIsGloballyBlockedAsync()
        {
            // given: removal is handed an ID, not an association, so the endpoint half of the
            // veto cannot run until the row is loaded. Authentication and the global block still
            // run FIRST — which is what stops this surface being used to probe which association
            // ids exist — and refuse UNAUTHORIZED: a write denial, not a not-found (§SEC14.7
            // posture A′ rule 4).
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.ReadOnly);

            Guid someAssociationId = Guid.NewGuid();

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is blocked from contributing content item associations.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<Association> removeTask =
                this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    someAssociationId,
                    deletionReason: GetRandomString(),
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // NOT ONE read against the Associations table — neither the removal itself nor a
            // read to decide it
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnRemoveBeforeReadingTheAssociationIfCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            Guid someAssociationId = Guid.NewGuid();

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is not authenticated.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<Association> removeTask =
                this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    someAssociationId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveIfCallerIsNeitherOwnerNorAdministratorAsync()
        {
            // given: the owner-or-Administrators test is composed from the STORED row, so it is
            // the foundation's half of the split gate (§SEC14.7 posture A′ rule 4) — and its
            // refusal therefore reaches the caller as a DEPENDENCY VALIDATION failure rather than
            // as the orchestration's own validation exception. Both are a 4xx and neither is a
            // 424; which layer refused is what the family records.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid someAssociationId = Guid.NewGuid();

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is not permitted to remove this content item association.");

            var associationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error " +
                        "occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    someAssociationId,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(associationValidationException);

            // when
            ValueTask<Association> removeTask =
                this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    someAssociationId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            // the orchestration issued NO second read to duplicate the foundation's half
            this.associationServiceMock.Verify(service =>
                service.RetrieveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveIfIdIsEmptyAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.UpsertDataList(
                key: nameof(Association.Id),
                value: "Id is required");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<Association> removeTask =
                this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    Guid.Empty,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
