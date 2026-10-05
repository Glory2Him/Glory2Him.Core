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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.Reactions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Theory]
        [MemberData(nameof(AssociationDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnRetrieveReactionSummariesIfTheAssociationFoundationRefusesAndLogItAsync(
            Xeption foundationException)
        {
            // given
            SetupEverySummaryReadToAnswer();
            SetupSummaryFoundationToThrow(nameof(IAssociationService), foundationException);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)!);

            // when
            ValueTask<IReadOnlyList<ContentItemReactionSummary>> retrieveTask =
                this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    [Guid.NewGuid()],
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        public static TheoryData<string, Xeption> SummaryEndpointFoundationRefusals()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<string, Xeption>
            {
                {
                    nameof(IContentItemService),
                    new ContentItemValidationException(message: randomMessage, innerException)
                },
                {
                    nameof(IContentItemService),
                    new ContentItemDependencyValidationException(message: randomMessage, innerException)
                },
                {
                    nameof(IReactionService),
                    new ReactionValidationException(message: randomMessage, innerException)
                },
                {
                    nameof(IReactionService),
                    new ReactionDependencyValidationException(message: randomMessage, innerException)
                },
            };
        }

        [Theory]
        [MemberData(nameof(SummaryEndpointFoundationRefusals))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveReactionSummariesIfAnotherFoundationRefusesAndLogItAsync(
            string refusingFoundation,
            Xeption foundationException)
        {
            // given: this read validated the ids it hands them, so a refusal beneath it is a fault
            // there, not the caller's (AssociationOrchestrationService.md §4 rule 9)
            SetupEverySummaryReadToAnswer();
            SetupSummaryFoundationToThrow(refusingFoundation, foundationException);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            // when
            ValueTask<IReadOnlyList<ContentItemReactionSummary>> retrieveTask =
                this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    [Guid.NewGuid()],
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        public static TheoryData<string, Xeption> SummaryFoundationFailures()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<string, Xeption>
            {
                {
                    nameof(IAssociationService),
                    new AssociationDependencyException(message: randomMessage, innerException)
                },
                {
                    nameof(IAssociationService),
                    new AssociationServiceException(message: randomMessage, innerException)
                },
                {
                    nameof(IContentItemService),
                    new ContentItemDependencyException(message: randomMessage, innerException)
                },
                {
                    nameof(IContentItemService),
                    new ContentItemServiceException(message: randomMessage, innerException)
                },
                {
                    nameof(IReactionService),
                    new ReactionDependencyException(message: randomMessage, innerException)
                },
                {
                    nameof(IReactionService),
                    new ReactionServiceException(message: randomMessage, innerException)
                },
            };
        }

        [Theory]
        [MemberData(nameof(SummaryFoundationFailures))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveReactionSummariesIfAFoundationFailsAndLogItAsync(
            string failingFoundation,
            Xeption foundationException)
        {
            // given
            SetupEverySummaryReadToAnswer();
            SetupSummaryFoundationToThrow(failingFoundation, foundationException);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            // when
            ValueTask<IReadOnlyList<ContentItemReactionSummary>> retrieveTask =
                this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    [Guid.NewGuid()],
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrieveReactionSummariesIfTheSettingReadFailsAndLogItAsync()
        {
            // given: the access broker fails. A broker raises no family of its own, so its raw
            // failure reaches this service's closing catch, and no summary is answered on a
            // setting nobody could read (§ARC16.2.1, the gate never falls open)
            SetupEverySummaryReadToAnswer();
            var serviceException = new Exception(GetRandomString());

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.IsAny<IReadOnlyList<ContentItemSettingKey>>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

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

            // when
            ValueTask<IReadOnlyList<ContentItemReactionSummary>> retrieveTask =
                this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    [Guid.NewGuid()],
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationServiceException>(
                    retrieveTask.AsTask);

            // then: nothing is counted after the setting read failed
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.RetrieveContentItemReactionCountsAsync(
                    It.IsAny<IReadOnlyList<Guid>>(),
                    It.IsAny<IReadOnlyList<Guid>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrieveReactionSummariesIfCancellationRequestedAsync()
        {
            // given: a genuine caller cancellation propagates as it is, never masked as a timeout
            // or wrapped
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<IReadOnlyList<ContentItemReactionSummary>> retrieveTask =
                this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    [Guid.NewGuid()],
                    cancellationTokenSource.Token);

            // then: nothing is read
            await Assert.ThrowsAsync<OperationCanceledException>(retrieveTask.AsTask);

            this.loggingBrokerMock.VerifyNoOtherCalls();
            VerifyNothingIsReadForTheSummaries();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrieveReactionSummariesIfOperationCanceledOccursWithoutRequestAndLogItAsync()
        {
            // given: a dependency is cancelled without the caller asking, which is a timeout
            SetupEverySummaryReadToAnswer();
            var operationCanceledException = new OperationCanceledException();

            SetupSummaryFoundationToThrow(nameof(IContentItemService), operationCanceledException);

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
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: timeoutAssociationOrchestrationException);

            // when
            ValueTask<IReadOnlyList<ContentItemReactionSummary>> retrieveTask =
                this.associationOrchestrationService.RetrieveContentItemReactionSummariesAsync(
                    [Guid.NewGuid()],
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A signed-in reader, and a world in which every read the summary makes answers — one
        // item that shows reactions, given Love, the reader's own included — so that an exception
        // test breaks exactly the read it names and every other read is reached.
        private void SetupEverySummaryReadToAnswer()
        {
            this.ambientSecurityContext = CreateReaderSecurityContext(readerUserId: GetRandomString());
            PublicContentItemGroup host = CreatePublicContentItemGroup();
            Reaction love = CreatePublicReaction(name: "Love");

            this.contentItemServiceMock.Setup(service =>
                service.RetrievePublicContentItemGroupsAsync(
                    It.IsAny<IReadOnlyList<Guid>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<PublicContentItemGroup> { host });

            this.reactionServiceMock.Setup(service =>
                service.RetrievePublicReactionsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Reaction> { love });

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEffectiveContentItemSettingsAsync(
                    It.IsAny<IReadOnlyList<ContentItemSettingKey>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<EffectiveContentItemSetting>
                        {
                            CreateWinningSetting(host, showReactions: true),
                        });

            this.associationServiceMock.Setup(service =>
                service.RetrieveContentItemReactionCountsAsync(
                    It.IsAny<IReadOnlyList<Guid>>(),
                    It.IsAny<IReadOnlyList<Guid>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<AssociationPairCount> { CreatePairCount(host, love, count: 1) });

            this.associationServiceMock.Setup(service =>
                service.RetrieveCallerContentItemReactionsAsync(
                    It.IsAny<IReadOnlyList<Guid>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<AssociationPairKey> { CreatePairKey(host, love) });
        }

        // The named foundation's read throws: the public groups read, the vocabulary read, or the
        // association foundation's count read.
        private void SetupSummaryFoundationToThrow(string foundation, Exception exception)
        {
            switch (foundation)
            {
                case nameof(IContentItemService):
                    this.contentItemServiceMock.Setup(service =>
                        service.RetrievePublicContentItemGroupsAsync(
                            It.IsAny<IReadOnlyList<Guid>>(),
                            It.IsAny<CancellationToken>()))
                                .ThrowsAsync(exception);

                    break;

                case nameof(IReactionService):
                    this.reactionServiceMock.Setup(service =>
                        service.RetrievePublicReactionsAsync(It.IsAny<CancellationToken>()))
                            .ThrowsAsync(exception);

                    break;

                case nameof(IAssociationService):
                    this.associationServiceMock.Setup(service =>
                        service.RetrieveContentItemReactionCountsAsync(
                            It.IsAny<IReadOnlyList<Guid>>(),
                            It.IsAny<IReadOnlyList<Guid>>(),
                            It.IsAny<CancellationToken>()))
                                .ThrowsAsync(exception);

                    break;
            }
        }
    }
}
