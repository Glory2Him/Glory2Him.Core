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
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        public static TheoryData<string[]> NonAdministratorRoleSets() =>
            new TheoryData<string[]>
            {
                // signed in and holding nothing
                Array.Empty<string>(),

                // the publisher tier clears every ordinary write and still does not clear this
                // one: hard removal is Administrators ALONE (§SEC14.7 posture A rule 3)
                new[] { Roles.Publishers },
                new[] { Roles.Reviewers },

                // a scoped role on an endpoint buys review of the pairing, never its
                // irreversible deletion
                new[] { Roles.PublishersFor(EntityType.ContentItem) },
                new[] { Roles.ReviewersFor(EntityType.Tag) },
            };

        [Theory]
        [MemberData(nameof(NonAdministratorRoleSets))]
        public async Task ShouldThrowUnauthorizedOnHardRemoveBeforeReadingTheAssociationIfCallerIsNotAnAdministratorAsync(
            string[] callerRoles)
        {
            // given: Administrators is decidable with NO row, so it joins authentication and the
            // global block in the orchestration's half of the gate and is answered before any
            // read (§SEC14.7 posture A′ rule 4). Refused with the orchestration's OWN validation
            // exception, and never a not-found: this is a write denial and revealing it leaks
            // nothing the caller did not already assert.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(callerRoles);
            Guid someAssociationId = Guid.NewGuid();

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is not permitted to permanently delete a content item association.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<Association> hardRemoveTask =
                this.associationOrchestrationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    hardRemoveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            // not one read against the Associations table
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnHardRemoveBeforeReadingTheAssociationIfCallerIsAnonymousAsync()
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
            ValueTask<Association> hardRemoveTask =
                this.associationOrchestrationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    hardRemoveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnHardRemoveIfAnAdministratorIsGloballyBlockedAsync()
        {
            // given: the veto is asked BEFORE any grant and is overridden by none of them,
            // Administrators included (§SEC18.6 rule 2). A blocked administrator is refused here,
            // before the row is ever read.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators, Roles.ReadOnly);

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
            ValueTask<Association> hardRemoveTask =
                this.associationOrchestrationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    hardRemoveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnHardRemoveIfAnEndpointBlocksTheAdministratorAsync()
        {
            // given: an Administrators clears this layer's half of the gate, and is STILL refused
            // one layer down when either endpoint blocks them — a block that stopped the
            // reversible takedown but not the irreversible one would be the wrong way round
            // (§SEC14.7 posture A′ rule 4), and §SEC18.6 rule 2 makes the veto overridable by
            // nobody, administrators included. It arrives as a DEPENDENCY VALIDATION failure,
            // because the endpoint half is composed from the stored row and so is the
            // foundation's.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            Guid someAssociationId = Guid.NewGuid();

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is blocked from contributing content item associations.");

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
                service.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(associationValidationException);

            // when
            ValueTask<Association> hardRemoveTask =
                this.associationOrchestrationService.HardRemoveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    hardRemoveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveIfIdIsEmptyAndLogItAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

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
            ValueTask<Association> hardRemoveTask =
                this.associationOrchestrationService.HardRemoveAssociationByIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    hardRemoveTask.AsTask);

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
