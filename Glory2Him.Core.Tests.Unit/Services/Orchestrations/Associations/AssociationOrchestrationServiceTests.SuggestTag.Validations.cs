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
    }
}
