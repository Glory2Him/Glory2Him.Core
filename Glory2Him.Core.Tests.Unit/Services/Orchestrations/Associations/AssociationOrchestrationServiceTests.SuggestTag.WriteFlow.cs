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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        // THE WRITE FLOW'S REFUSALS HOLD ON THE SUGGESTION TOO (AssociationOrchestrationService
        // .TagSuggestions.md §1 rule 8). The suggestion is neither of the write flow's two doors,
        // and reaches the rules that decide a pairing through the members that hold them, so each
        // refusal the seam lists is driven through the upsert, for the post paired with a tag the
        // caller may see, and through the suggestion, for the same post, and both must refuse it
        // alike. An entry about an endpoint is driven on the post, endpoint A on both. An entry
        // the suggestion cannot meet is excepted here by name, with its reason, and an entry this
        // theory neither drives nor excepts fails it, so a rule the flow gains later reaches the
        // suggestion or is excepted in the open.
        [Theory]
        [MemberData(nameof(WriteFlowRefusals))]
        public async Task ShouldRefuseTheWriteFlowsRefusalsOnTheSuggestionAsTheUpsertDoesAsync(
            string refusal)
        {
            // given
            TagSuggestion tagSuggestion = CreateTagSuggestion();

            Association upsertRequest =
                CreateRawUpsertRequestBetween(EntityType.ContentItem, EntityType.Tag);

            upsertRequest.EntityAKeyId = tagSuggestion.ContentItemId;
            SecurityContext callerContext = CreateReaderSecurityContext(GetRandomString());
            ApprovalStatus postApprovalStatus = ApprovalStatus.Draft;
            bool isThePostFound = true;

            switch (refusal)
            {
                case "an unauthenticated caller":
                    callerContext = new SecurityContext { IsAuthenticated = false };
                    break;

                case "a globally blocked caller":
                    callerContext = CreateReaderSecurityContext(GetRandomString(), Roles.ReadOnly);
                    break;

                case "a caller blocked from an endpoint's content type":
                    callerContext = CreateReaderSecurityContext(
                        GetRandomString(),
                        Roles.ReadOnlyFor(EntityType.ContentItem, ContentType.Story));

                    break;

                case "an endpoint that does not resolve":
                    isThePostFound = false;
                    break;

                case "a rejected endpoint":
                    postApprovalStatus = ApprovalStatus.Rejected;
                    break;

                case "a missing endpoint key":
                    // EXCEPTED: the suggestion is handed no tag's key, the find-or-add answering
                    // it, and its own fields, the post's id among them, are its validation's
                    // (§1 rule 1, step 3). Held here as the reason, so the exception goes the day
                    // the suggestion carries a key of its own.
                    typeof(TagSuggestion).GetProperties().Select(property => property.Name)
                        .Should().BeEquivalentTo(
                            nameof(TagSuggestion.ContentItemId),
                            nameof(TagSuggestion.Name));

                    return;

                default:
                    Assert.Fail(
                        $"The write flow's refusal \"{refusal}\" is neither driven on the " +
                        "suggestion nor excepted from it.");

                    break;
            }

            this.ambientSecurityContext = callerContext;
            SetupSuggestionEnvelopeFor(tagSuggestion);

            SetupMethodPathEndpointReadsAt(
                upsertRequest,
                entityAApprovalStatus: postApprovalStatus,
                entityBApprovalStatus: ApprovalStatus.Draft);

            if (isThePostFound is false)
            {
                this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemByIdAsync(
                        tagSuggestion.ContentItemId,
                        TestContext.Current.CancellationToken))
                            .ThrowsAsync(new ContentItemValidationException(
                                message: "Content item validation error occurred, fix the errors and try again.",
                                innerException: new NotFoundContentItemException(
                                    message: $"Couldn't find content item with id: {tagSuggestion.ContentItemId}.")));
            }

            // when
            ValueTask<AssociationSuggestionResult> upsertTask =
                this.associationOrchestrationService.UpsertAssociationAsync(
                    upsertRequest,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException upsertException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    upsertTask.AsTask);

            ValueTask<AssociationSubmissionResult> suggestTagTask =
                this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            AssociationOrchestrationValidationException suggestionException =
                await Assert.ThrowsAsync<AssociationOrchestrationValidationException>(
                    suggestTagTask.AsTask);

            // then: refused alike, and neither writes
            suggestionException.Should().BeEquivalentTo(upsertException);

            this.tagServiceMock.Verify(service =>
                service.FindOrAddTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.VerifyNoOtherCalls();
        }
    }
}
