// ─────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ─────────────────────────────────────────────────────────────────────────────────

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        /// <summary>
        /// Criterion 9's dependency row says "endpoint services included". Round 2 rewired which
        /// endpoint read this path calls — an <c>AllVersions</c> endpoint now goes to the
        /// group-keyed read — and a genuine downstream failure of THAT read must leave as a
        /// dependency exception rather than being mistaken for a not-found.
        ///
        /// <para>The distinction is <c>IsEndpointNotFound</c>'s and it is shared with the add
        /// path, so the risk is low; what was uncovered is the two new call sites. A conversion
        /// that widened to catch any <c>Xeption</c> would turn a broken database into "this
        /// association does not exist", which is a 4xx for a 500.</para>
        /// </summary>
        [Theory]
        [InlineData(EntityType.ContentItem)]
        [InlineData(EntityType.Link)]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfAnEndpointGroupReadFailsAsync(
            EntityType versionedType)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var versionedKeyId = Guid.NewGuid();
            var versionedGroupId = Guid.NewGuid();
            var partnerId = Guid.NewGuid();

            Association storedAssociation = BuildTestedEndpointAssociation(
                versionedType,
                versionedKeyId,
                versionedGroupId,
                EntityType.Tag,
                partnerId,
                testedIsOnSideA: true,
                testedScope: Scope.AllVersions);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storedAssociation);

            var innerException = new Xeption();

            if (versionedType == EntityType.ContentItem)
            {
                this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemsByGroupIdAsync(
                        versionedGroupId, It.IsAny<CancellationToken>()))
                            .ThrowsAsync(new ContentItemDependencyException(
                                message: "downstream failure",
                                innerException: innerException));
            }
            else
            {
                this.linkServiceMock.Setup(service =>
                    service.RetrieveLinksByGroupIdAsync(
                        versionedGroupId, It.IsAny<CancellationToken>()))
                            .ThrowsAsync(new LinkDependencyException(
                                message: "downstream failure",
                                innerException: innerException));
            }

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, " +
                        "contact support.",
                    innerException: innerException);

            // when
            ValueTask<Association> retrieveTask =
                this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then: a dependency failure, NOT a not-found
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AssociationDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRetrieveByIdIfTheFoundationDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: a ROUTINE refusal raised by the foundation - the endpoint veto, the
            // owner test, the Publishers tier, a terminal row, a uniqueness collision -
            // leaves as a dependency VALIDATION failure carrying the foundation's own
            // inner. It must NOT leave as a dependency error: that is the 424-for-a-400
            // shape the audit on ApprovalOrchestrationService found.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error " +
                        "occurred, fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)!);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<Association> retrieveByIdTask =
                this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    retrieveByIdTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AssociationDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfTheFoundationDoesAndLogItAsync(
            Xeption foundationException)
        {
            // given: a genuine downstream failure stays a dependency failure, and is
            // never re-surfaced as the foundation's own exception type.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, " +
                        "contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<Association> retrieveByIdTask =
                this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    retrieveByIdTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given: anything outside the recognised families leaves as a service
            // exception, wrapping FailedAssociationOrchestrationServiceException.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            var serviceException = new Exception("Service error occurred.");

            var failedAssociationOrchestrationServiceException =
                new FailedAssociationOrchestrationServiceException(
                    message: "Failed content item association orchestration service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedServiceException =
                new AssociationOrchestrationServiceException(
                    message: "Content item association orchestration service error occurred, " +
                        "contact support.",
                    innerException: failedAssociationOrchestrationServiceException);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<Association> retrieveByIdTask =
                this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationServiceException>(
                    retrieveByIdTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveByIdIfOperationCanceledOccursWithoutRequestAndLogItAsync()
        {
            // given: an OperationCanceled whose OWN token was not cancelled is a
            // dependency timeout rather than a caller cancellation, and becomes a timeout
            // dependency error.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutAssociationOrchestrationException =
                new TimeoutAssociationOrchestrationException(
                    message: "Failed content item association orchestration timeout error occurred, " +
                        "contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, " +
                        "contact support.",
                    innerException: timeoutAssociationOrchestrationException);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<Association> retrieveByIdTask =
                this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    retrieveByIdTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveByIdIfCancellationRequestedAsync()
        {
            // given: a token already cancelled throws BEFORE any dependency call, and is
            // rethrown rather than wrapped - a caller cancellation is never masked as a
            // timeout.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Guid someAssociationId = Guid.NewGuid();

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<Association> retrieveByIdTask =
                this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    someAssociationId,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(retrieveByIdTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
            this.commentServiceMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
