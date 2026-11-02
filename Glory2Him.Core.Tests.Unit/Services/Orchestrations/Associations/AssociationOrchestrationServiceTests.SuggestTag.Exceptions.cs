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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnSuggestTagIfCancellationRequestedAsync()
        {
            // given: a caller who has already cancelled is answered with their own cancellation,
            // asked before the null check, as the upsert asks it (§1 rule 5)
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    cancellationTokenSource.Token);

            // then: nothing is minted, read or logged
            await Assert.ThrowsAsync<OperationCanceledException>(suggestTagTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnSuggestTagIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given: a dependency call is cancelled by a token other than the caller's, which is a
            // timeout, not the caller's cancellation (§1 rule 5)
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            SetupSuggestionForThePost(tagSuggestion);

            this.tagServiceMock.Setup(service =>
                service.FindOrAddTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new OperationCanceledException());

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
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    suggestTagTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private const string TheInsert = "the insert";

        // Each refusal of the association foundation on either probe and on the insert, the
        // insert's dependency validation one per inner exception it raises, for a tag the caller
        // can see and for one they cannot. The insert's refusal of a pair already held is driven
        // for a tag the caller can see alone: for one they cannot, it is answered Created
        // (criterion 5), and every other refusal is thrown on for both (§1 rule 4).
        public static TheoryData<string, Xeption, bool> AssociationRefusalsOnASuggestion()
        {
            var refusals = new TheoryData<string, Xeption, bool>();

            foreach (bool isTagVisible in new[] { true, false })
            {
                foreach (string probe in new[] { ExactPairProbe, OverlapProbe })
                {
                    refusals.Add(
                        probe,
                        CreateAssociationValidationException(new Xeption(GetRandomString())),
                        isTagVisible);

                    refusals.Add(
                        probe,
                        CreateAssociationDependencyValidationException(new Xeption(GetRandomString())),
                        isTagVisible);
                }

                refusals.Add(
                    TheInsert,
                    CreateAssociationValidationException(new InvalidAssociationException(GetRandomString())),
                    isTagVisible);

                refusals.Add(
                    TheInsert,
                    CreateAssociationDependencyValidationException(
                        new InvalidAssociationReferenceException(
                            message: GetRandomString(),
                            innerException: new Exception(),
                            data: new Exception().Data)),
                    isTagVisible);

                refusals.Add(
                    TheInsert,
                    CreateAssociationDependencyValidationException(
                        new LockedAssociationException(
                            message: GetRandomString(),
                            innerException: new Exception(),
                            data: new Exception().Data)),
                    isTagVisible);
            }

            refusals.Add(TheInsert, CreatePairAlreadyHeldException(), true);

            return refusals;
        }

        [Theory]
        [MemberData(nameof(AssociationRefusalsOnASuggestion))]
        public async Task ShouldThrowDependencyValidationExceptionOnSuggestTagIfAssociationValidationErrorOccursAndLogItAsync(
            string failingCall,
            Xeption foundationException,
            bool isTagVisible)
        {
            // given: the foundation refuses a probe or the insert, which surfaces as this service's
            // dependency validation exception carrying the foundation's own inner (§1 rule 4)
            TagSuggestion tagSuggestion = SetupSuggestionOfAFoundTag(isTagVisible);
            SetupAssociationCallToThrow(failingCall, foundationException);

            var expectedDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (foundationException.InnerException as Xeption)!);

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyValidationException>(
                    suggestTagTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Each failure of the association foundation on either probe and on the insert, for a tag
        // the caller can see and for one they cannot, none of which is answered Created (§1 rule 4).
        public static TheoryData<string, Xeption, bool> AssociationFailuresOnASuggestion()
        {
            var failures = new TheoryData<string, Xeption, bool>();

            foreach (bool isTagVisible in new[] { true, false })
            {
                foreach (string call in new[] { ExactPairProbe, OverlapProbe, TheInsert })
                {
                    failures.Add(
                        call,
                        new AssociationDependencyException(
                            message: GetRandomString(),
                            innerException: new Xeption(GetRandomString())),
                        isTagVisible);

                    failures.Add(
                        call,
                        new AssociationServiceException(
                            message: GetRandomString(),
                            innerException: new Xeption(GetRandomString())),
                        isTagVisible);
                }
            }

            return failures;
        }

        [Theory]
        [MemberData(nameof(AssociationFailuresOnASuggestion))]
        public async Task ShouldThrowDependencyExceptionOnSuggestTagIfAssociationDependencyErrorOccursAndLogItAsync(
            string failingCall,
            Xeption foundationException,
            bool isTagVisible)
        {
            // given
            TagSuggestion tagSuggestion = SetupSuggestionOfAFoundTag(isTagVisible);
            SetupAssociationCallToThrow(failingCall, foundationException);

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    suggestTagTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private const string TheFindOrAdd = "the find-or-add";
        private const string TheTagsRead = "the tag's read";
        private const string ThePostsRead = "the post's read";

        // Every exception of the find-or-add's, the tag's read's and the post's read's families
        // but the two not-founds the flow answers — the post's, a refusal, and the tag's, an
        // answer — each a fault beneath the orchestration (§1 rule 4; TagSuggestions.md rule 11).
        // The find-or-add's dependency validation is driven again with the name taken and not
        // found on its second read.
        public static TheoryData<string, Xeption> TagAndContentItemErrorsOnASuggestion()
        {
            var errors = new TheoryData<string, Xeption>();

            foreach (string tagCall in new[] { TheFindOrAdd, TheTagsRead })
            {
                errors.Add(tagCall, new TagValidationException(
                    message: GetRandomString(),
                    innerException: new InvalidTagException(GetRandomString())));

                errors.Add(tagCall, new TagDependencyValidationException(
                    message: GetRandomString(),
                    innerException: new Xeption(GetRandomString())));

                errors.Add(tagCall, new TagDependencyException(
                    message: GetRandomString(),
                    innerException: new Xeption(GetRandomString())));

                errors.Add(tagCall, new TagServiceException(
                    message: GetRandomString(),
                    innerException: new Xeption(GetRandomString())));
            }

            errors.Add(TheFindOrAdd, new TagDependencyValidationException(
                message: GetRandomString(),
                innerException: new AlreadyExistsTagException(
                    message: GetRandomString(),
                    innerException: new Exception(),
                    data: new Exception().Data)));

            errors.Add(ThePostsRead, new ContentItemDependencyValidationException(
                message: GetRandomString(),
                innerException: new Xeption(GetRandomString())));

            errors.Add(ThePostsRead, new ContentItemDependencyException(
                message: GetRandomString(),
                innerException: new Xeption(GetRandomString())));

            errors.Add(ThePostsRead, new ContentItemServiceException(
                message: GetRandomString(),
                innerException: new Xeption(GetRandomString())));

            return errors;
        }

        [Theory]
        [MemberData(nameof(TagAndContentItemErrorsOnASuggestion))]
        public async Task ShouldThrowDependencyExceptionOnSuggestTagIfATagOrContentItemErrorOccursAndLogItAsync(
            string failingCall,
            Xeption foundationException)
        {
            // given: the find-or-add, the tag's read or the post's read throws, the calls before
            // it answering as the happy path has them
            TagSuggestion tagSuggestion = SetupSuggestionOfAFoundTag(isTagVisible: true);

            switch (failingCall)
            {
                case TheFindOrAdd:
                    this.tagServiceMock.Setup(service =>
                        service.FindOrAddTagAsync(
                            It.IsAny<Tag>(),
                            It.IsAny<CancellationToken>()))
                                .ThrowsAsync(foundationException);

                    break;

                case TheTagsRead:
                    this.tagServiceMock.Setup(service =>
                        service.RetrieveTagByIdAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<CancellationToken>()))
                                .ThrowsAsync(foundationException);

                    break;

                default:
                    this.contentItemServiceMock.Setup(service =>
                        service.RetrieveContentItemByIdAsync(
                            tagSuggestion.ContentItemId,
                            It.IsAny<CancellationToken>()))
                                .ThrowsAsync(foundationException);

                    break;
            }

            var expectedDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (foundationException.InnerException as Xeption)!);

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationDependencyException>(
                    suggestTagTask.AsTask);

            // then: nothing is written
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private static AssociationValidationException CreateAssociationValidationException(
            Xeption innerException) =>
            new AssociationValidationException(
                message: "Association validation errors occurred, please try again.",
                innerException: innerException);

        private static AssociationDependencyValidationException CreateAssociationDependencyValidationException(
            Xeption innerException) =>
            new AssociationDependencyValidationException(
                message: "Association dependency validation occurred, fix the errors and try again.",
                innerException: innerException);

        // A contributor's suggestion, as the happy path has it, of a name the find-or-add finds,
        // the tag one the caller can see or one its read answers them not found.
        private TagSuggestion SetupSuggestionOfAFoundTag(bool isTagVisible)
        {
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            SetupSuggestionForThePost(tagSuggestion);

            Guid foundTagId =
                SetupFindOrAddOf(tagSuggestion.Name, TagNameOutcome.Found);

            SetupTagReadFor(foundTagId, isTagVisible);

            return tagSuggestion;
        }

        // The named call fails; a probe before it, left unset, answers no row.
        private void SetupAssociationCallToThrow(string call, Xeption foundationException)
        {
            switch (call)
            {
                case ExactPairProbe:
                    this.associationServiceMock.Setup(service =>
                        service.FindAssociationByPairAsync(
                            It.IsAny<Association>(),
                            It.IsAny<CancellationToken>()))
                                .ThrowsAsync(foundationException);

                    break;

                case OverlapProbe:
                    this.associationServiceMock.Setup(service =>
                        service.FindOverlappingAssociationAsync(
                            It.IsAny<Association>(),
                            It.IsAny<Guid?>(),
                            It.IsAny<CancellationToken>()))
                                .ThrowsAsync(foundationException);

                    break;

                default:
                    this.associationServiceMock.Setup(service =>
                        service.AddAssociationAsync(
                            It.IsAny<Association>(),
                            It.IsAny<CancellationToken>()))
                                .ThrowsAsync(foundationException);

                    break;
            }
        }
    }
}
