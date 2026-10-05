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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        // The Association foundation refusing either of the withdrawal's two calls — the lookup
        // or the soft delete — with each of its validation families.
        public static TheoryData<string, Xeption> FoundationRefusalsOnRemoveByPair()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);
            var foundationRefusals = new TheoryData<string, Xeption>();

            foreach (string failingCall in new[] { "lookup", "soft delete" })
            {
                foundationRefusals.Add(
                    failingCall,
                    new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                        .AssociationValidationException(message: randomMessage, innerException: innerException));

                foundationRefusals.Add(
                    failingCall,
                    new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                        .AssociationDependencyValidationException(message: randomMessage, innerException: innerException));
            }

            return foundationRefusals;
        }

        [Theory]
        [MemberData(nameof(FoundationRefusalsOnRemoveByPair))]
        public async Task ShouldThrowDependencyValidationExceptionOnRemoveByPairIfTheFoundationRefusesAndLogItAsync(
            string failingCall,
            Xeption foundationException)
        {
            // given: the foundation refuses the lookup or the soft delete, which surfaces as this
            // service's dependency validation exception carrying the foundation's own inner —
            // never the foundation's exception type itself
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            SetupFailingWithdrawal(removalRequest, readerUserId, failingCall, foundationException);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)!);

            // when
            ValueTask<AssociationRemovalResult> removeTask =
                this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // The Association foundation failing either of the withdrawal's two calls with each of its
        // dependency and service families.
        public static TheoryData<string, Xeption> FoundationFailuresOnRemoveByPair()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);
            var foundationFailures = new TheoryData<string, Xeption>();

            foreach (string failingCall in new[] { "lookup", "soft delete" })
            {
                foundationFailures.Add(
                    failingCall,
                    new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                        .AssociationDependencyException(message: randomMessage, innerException: innerException));

                foundationFailures.Add(
                    failingCall,
                    new Glory2Him.Core.Models.Foundations.Associations.Exceptions
                        .AssociationServiceException(message: randomMessage, innerException: innerException));
            }

            return foundationFailures;
        }

        [Theory]
        [MemberData(nameof(FoundationFailuresOnRemoveByPair))]
        public async Task ShouldThrowDependencyExceptionOnRemoveByPairIfTheFoundationFailsAndLogItAsync(
            string failingCall,
            Xeption foundationException)
        {
            // given: the foundation fails the lookup or the soft delete
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId);

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            SetupFailingWithdrawal(removalRequest, readerUserId, failingCall, foundationException);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            // when
            ValueTask<AssociationRemovalResult> removeTask =
                this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRemoveByPairIfCancellationRequestedAsync()
        {
            // given: a genuine caller cancellation propagates as it is, never masked as a timeout
            // or wrapped in an orchestration exception, and nothing is read
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<AssociationRemovalResult> removeTask =
                this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(removeTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRemoveByPairIfServiceErrorOccursAndLogItAsync()
        {
            // given: an unexpected failure, here while the envelope is minted
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());

            Association removalRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Reaction);

            var serviceException = new Exception(GetRandomString());

            var failedAssociationOrchestrationServiceException =
                new FailedAssociationOrchestrationServiceException(
                    message: "Failed content item association orchestration service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedServiceException =
                new AssociationOrchestrationServiceException(
                    message: "Content item association orchestration service error occurred, contact support.",
                    innerException: failedAssociationOrchestrationServiceException);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(removalRequest))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<AssociationRemovalResult> removeTask =
                this.associationOrchestrationService.RemoveAssociationByPairAsync(
                    removalRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationServiceException>(removeTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.reactionServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A reader's withdrawal whose lookup, or whose soft delete of the live row the lookup
        // finds, throws the given exception.
        private void SetupFailingWithdrawal(
            Association removalRequest,
            string readerUserId,
            string failingCall,
            Exception foundationException)
        {
            Association expectedLookupPair = SetupReadersWithdrawal(removalRequest, readerUserId);

            if (failingCall == "lookup")
            {
                this.associationServiceMock.Setup(service =>
                    service.FindPersonalAssociationAsync(
                        It.Is(SameAssociationAs(expectedLookupPair)),
                        TestContext.Current.CancellationToken))
                            .ThrowsAsync(foundationException);

                return;
            }

            var readersRow = new PersonalAssociationMatch
            {
                Id = Guid.NewGuid(),
                EntityBKeyId = GetNamedReactionId(removalRequest),
                IsDeleted = false,
            };

            this.associationServiceMock.Setup(service =>
                service.FindPersonalAssociationAsync(
                    It.Is(SameAssociationAs(expectedLookupPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(readersRow);

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    readersRow.Id,
                    null,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(foundationException);
        }
    }
}
