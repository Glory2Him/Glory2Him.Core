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
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Associations;
using KellermanSoftware.CompareNetObjects;
using Moq;
using Tynamix.ObjectFiller;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldPairTheTagTheFindOrAddAddedAsync()
        {
            // given: a contributor suggests a name no tag holds, for a post they can see whose
            // setting allows tags. The find-or-add adds a tag of that name at Submitted and
            // answers its id. That tag is the caller's own, so it is not read again, and its pair,
            // free at both probes, is inserted at Submitted (§1 rule 1, steps 5, 9, 11 and 13).
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            ContentItem suggestedPost = SetupSuggestionForThePost(tagSuggestion);

            Guid addedTagId =
                SetupFindOrAddOf(tagSuggestion.Name, TagNameOutcome.Added);

            Association expectedInsertedPair =
                CreateSuggestedPairFor(suggestedPost, addedTagId, ApprovalStatus.Submitted);

            Association insertedPair = expectedInsertedPair.DeepClone();
            insertedPair.Id = Guid.NewGuid();

            this.associationServiceMock.Setup(service =>
                service.AddAssociationAsync(
                    It.Is(SameAssociationAs(expectedInsertedPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(insertedPair);

            var expectedResult = new AssociationSubmissionResult
            {
                Status = AssociationSubmissionStatus.Created,
            };

            // when
            AssociationSubmissionResult actualResult =
                await this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            // then: Moq reads each argument as it stands once the call is through, and the probes
            // and the add are handed the one pair, so each probe is verified against what the add
            // received
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.tagServiceMock.Verify(service =>
                service.FindOrAddTagAsync(
                    It.Is(SameTagAs(CreateSuggestedTagNamed(tagSuggestion.Name))),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.Is(SameAssociationAs(expectedInsertedPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.FindOverlappingAssociationAsync(
                    It.Is(SameAssociationAs(expectedInsertedPair)),
                    (Guid?)null,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.associationServiceMock.Verify(service =>
                service.AddAssociationAsync(
                    It.Is(SameAssociationAs(expectedInsertedPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // A suggestion as a reader sends one: a post's id and a name a tag may carry.
        private static TagSuggestion CreateTagSuggestion() =>
            new TagSuggestion
            {
                ContentItemId = Guid.NewGuid(),
                Name = GetRandomTagName(),
            };

        private static string GetRandomTagName() =>
            new MnemonicString(wordCount: 1, wordMinLength: 3, wordMaxLength: 20).GetValue();

        // The suggestion's envelope, minted under the ambient caller over the suggestion as it was
        // handed in, and the post it names, read as the caller at the status the test names, a
        // Story in a group of its own, whose setting allows every facet.
        private ContentItem SetupSuggestionForThePost(
            TagSuggestion tagSuggestion,
            ApprovalStatus postApprovalStatus = ApprovalStatus.Draft)
        {
            SetupSuggestionEnvelopeFor(tagSuggestion);

            var suggestedPost = new ContentItem
            {
                Id = tagSuggestion.ContentItemId,
                GroupId = Guid.NewGuid(),
                ContentType = ContentType.Story,
                ApprovalStatus = postApprovalStatus,
            };

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    tagSuggestion.ContentItemId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(suggestedPost);

            SetupAllowingSettingFor(suggestedPost);

            return suggestedPost;
        }

        private EventEnvelope<TagSuggestion> SetupSuggestionEnvelopeFor(TagSuggestion tagSuggestion)
        {
            var suggestionEnvelope = new EventEnvelope<TagSuggestion>
            {
                Content = tagSuggestion,
                SecurityContext = this.ambientSecurityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(tagSuggestion))
                    .ReturnsAsync(suggestionEnvelope);

            return suggestionEnvelope;
        }

        // The find-or-add of a tag of the name, answering a tag's id and the outcome the test names.
        private Guid SetupFindOrAddOf(string tagName, TagNameOutcome tagNameOutcome)
        {
            var tagNameResult = new TagNameResult
            {
                Id = Guid.NewGuid(),
                Outcome = tagNameOutcome,
            };

            this.tagServiceMock.Setup(service =>
                service.FindOrAddTagAsync(
                    It.Is(SameTagAs(CreateSuggestedTagNamed(tagName))),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(tagNameResult);

            return tagNameResult.Id;
        }

        // The new tag the find-or-add is handed: the name, at Submitted, and nothing else
        // (TagSuggestions.md rule 14).
        private static Tag CreateSuggestedTagNamed(string tagName) =>
            new Tag
            {
                Name = tagName,
                ApprovalStatus = ApprovalStatus.Submitted,
            };

        // The pair a suggestion makes, as resolution derives it: the post on A at its group under
        // AllVersions with its content type, the tag on B at its own id under ThisVersionOnly with
        // none, no user, and the status the test names (§1 rule 1, step 5).
        private static Association CreateSuggestedPairFor(
            ContentItem suggestedPost,
            Guid tagId,
            ApprovalStatus approvalStatus) =>
            new Association
            {
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = suggestedPost.Id,
                EntityAGroupId = suggestedPost.GroupId,
                EntityAContentType = suggestedPost.ContentType,
                EntityAScope = Scope.AllVersions,
                EntityBType = EntityType.Tag,
                EntityBKeyId = tagId,
                EntityBGroupId = tagId,
                EntityBContentType = null,
                EntityBScope = Scope.ThisVersionOnly,
                UserId = null,
                ApprovalStatus = approvalStatus,
            };

        private static Expression<Func<Tag, bool>> SameTagAs(Tag expectedTag) =>
            actualTag => new CompareLogic().Compare(expectedTag, actualTag).AreEqual;
    }
}
