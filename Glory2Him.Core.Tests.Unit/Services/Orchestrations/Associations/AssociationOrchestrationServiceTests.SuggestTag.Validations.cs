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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        // Each caller the contribution gate refuses before anything is read, and the message the
        // upsert's gate gives the same caller: an anonymous caller in both shapes, and one holding
        // the global ReadOnly (§SEC14.7 posture A′ rule 1).
        public static TheoryData<SecurityContext, string> CallersWhoMayNotSuggestATag() =>
            new TheoryData<SecurityContext, string>
            {
                { null, "The current user is not authenticated." },

                {
                    new SecurityContext { IsAuthenticated = false, Roles = Array.Empty<string>() },
                    "The current user is not authenticated."
                },

                {
                    CreateReaderSecurityContext(GetRandomString(), Roles.ReadOnly),
                    "The current user is blocked from contributing content item associations."
                },
            };

        [Theory]
        [MemberData(nameof(CallersWhoMayNotSuggestATag))]
        public async Task ShouldThrowValidationExceptionOnSuggestTagIfUserMayNotContributeAndLogItAsync(
            SecurityContext refusedSecurityContext,
            string expectedMessage)
        {
            // given: the caller may not contribute. The post, its setting and the find-or-add are
            // set up to answer, so a flow that read past the gate would be answered rather than
            // fail (§1 rule 1, step 2).
            this.ambientSecurityContext = refusedSecurityContext;
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            SetupSuggestionForThePost(tagSuggestion);
            SetupFindOrAddOf(tagSuggestion.Name, TagNameOutcome.Added);

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(message: expectedMessage);

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    suggestTagTask.AsTask);

            // then: refused, and neither the post, nor the setting, nor the tag is read
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // The pairing's endpoint veto over a post, a Story, and a tag: either end of the post's
        // two tiers, and the tag's, which is the tag add's veto as well (§SEC14.7 posture A′ rule
        // 1; TagSuggestions.md rule 9, step 4).
        public static TheoryData<string> EndpointBlocksOnASuggestion() =>
            new TheoryData<string>
            {
                Roles.ReadOnlyFor(EntityType.ContentItem),
                Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Story),
                Roles.ReadOnlyFor(EntityType.Tag),
            };

        [Theory]
        [MemberData(nameof(EndpointBlocksOnASuggestion))]
        public async Task ShouldThrowValidationExceptionOnSuggestTagIfUserIsBlockedFromAnEndpointAndLogItAsync(
            string blockingRole)
        {
            // given: a signed-in caller holding a block on one of the pair's endpoints. The post is
            // read, as the veto is composed from the type its read derives, and the refusal comes
            // before the setting or the tag is read (§1 rule 1, step 6).
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString(), blockingRole);
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            SetupSuggestionForThePost(tagSuggestion);
            SetupFindOrAddOf(tagSuggestion.Name, TagNameOutcome.Added);

            var unauthorizedAssociationOrchestrationException =
                new UnauthorizedAssociationOrchestrationException(
                    message: "The current user is blocked from contributing content item associations.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAssociationOrchestrationException);

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    suggestTagTask.AsTask);

            // then: the post alone is read, and nothing is written
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    tagSuggestion.ContentItemId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSuggestTagIfThePostIsNotFoundAndLogItAsync()
        {
            // given: the post's read refuses it as not found, which it answers alike for a post
            // that does not exist and one the caller may not see (§SEC14.5 rule 1), so one case
            // stands for both. The post is endpoint A, and resolution names it so, as it does for
            // the upsert's endpoint A (§1 rule 1, step 4).
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            SetupSuggestionEnvelopeFor(tagSuggestion);
            SetupFindOrAddOf(tagSuggestion.Name, TagNameOutcome.Added);

            var contentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: new NotFoundContentItemException(
                        message: $"Couldn't find content item with id: {tagSuggestion.ContentItemId}."));

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    tagSuggestion.ContentItemId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(contentItemValidationException);

            var notFoundAssociationOrchestrationException =
                new NotFoundAssociationOrchestrationException(
                    message: "The A endpoint was not found.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundAssociationOrchestrationException);

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    suggestTagTask.AsTask);

            // then: nothing more is read
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    tagSuggestion.ContentItemId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSuggestTagIfSuggestionIsNullAndLogItAsync()
        {
            // given
            TagSuggestion nullTagSuggestion = null;

            var nullAssociationOrchestrationException =
                new NullAssociationOrchestrationException(
                    message: "Tag suggestion is null.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: nullAssociationOrchestrationException);

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    nullTagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    suggestTagTask.AsTask);

            // then: refused before the envelope is minted or anything is read
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // One case per field and rule: an empty post id; and a name that is null, empty or white
        // space, or longer than a tag's cap of thirty characters once trimmed, the white space
        // around it counting against no cap (TagSuggestions.md rule 2; §1 rule 1, step 3).
        public static TheoryData<bool, string, string, string> InvalidTagSuggestions() =>
            new TheoryData<bool, string, string, string>
            {
                { true, "Grace", nameof(TagSuggestion.ContentItemId), "Id is required" },
                { false, null, nameof(TagSuggestion.Name), "Text is required" },
                { false, "", nameof(TagSuggestion.Name), "Text is required" },
                { false, "   ", nameof(TagSuggestion.Name), "Text is required" },

                {
                    false,
                    $" {ThirtyCharacterTagName}s ",
                    nameof(TagSuggestion.Name),
                    "Text exceed max length of 30 characters"
                },
            };

        [Theory]
        [MemberData(nameof(InvalidTagSuggestions))]
        public async Task ShouldThrowValidationExceptionOnSuggestTagIfSuggestionIsInvalidAndLogItAsync(
            bool isContentItemIdEmpty,
            string suggestedName,
            string invalidParameter,
            string expectedMessage)
        {
            // given: a contributor allowed to suggest a tag, and a suggestion one field of which
            // breaks one rule. The post and the find-or-add are set up to answer, so a flow that
            // read past the fields would be answered rather than fail.
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            tagSuggestion.ContentItemId = isContentItemIdEmpty ? Guid.Empty : tagSuggestion.ContentItemId;
            tagSuggestion.Name = suggestedName;
            SetupSuggestionForThePost(tagSuggestion);
            SetupFindOrAddOf(suggestedName?.Trim() ?? string.Empty, TagNameOutcome.Added);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "Tag suggestion is invalid, fix the errors and try again.");

            invalidAssociationOrchestrationException.AddData(
                key: invalidParameter,
                values: expectedMessage);

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    suggestTagTask.AsTask);

            // then: refused, and nothing is read
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSuggestTagIfThePostIsRejectedAndLogItAsync()
        {
            // given: the post's read answers the version the suggestion names at Rejected. The
            // suggestion is refused with the write flow's own refusal of a rejected endpoint, asked
            // of endpoint A, from the status that read kept, so the post is read once
            // (RejectedEndpoints.md rules 1 and 10; §1 rule 1, step 7).
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            SetupSuggestionForThePost(tagSuggestion, ApprovalStatus.Rejected);
            SetupFindOrAddOf(tagSuggestion.Name, TagNameOutcome.Added);

            var invalidAssociationOrchestrationException =
                new InvalidAssociationOrchestrationException(
                    message: "The A endpoint is rejected, so it cannot be paired.");

            var expectedValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAssociationOrchestrationException);

            // when
            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    suggestTagTask.AsTask);

            // then: neither the setting nor the tag is read, and nothing is written
            actualException.Should().BeEquivalentTo(expectedValidationException);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    tagSuggestion.ContentItemId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedValidationException))),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
