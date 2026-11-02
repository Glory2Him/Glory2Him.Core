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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    /// <summary>
    /// The find-or-add's read by name authors its condition as a query-shaping function and
    /// hands it to the storage broker (§ARC12.2.1 rule 3). The mocked broker APPLIES the
    /// function it is handed to the in-memory set each test seeds (rule 5), so these tests
    /// execute the condition rather than merely observing that one was passed.
    /// </summary>
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldFindTheTagThatHoldsTheNameAsync()
        {
            // given: the named row beside a row of another name, stored ahead of it, so a read
            // that takes the first row rather than the named one answers the wrong tag
            string inputName = GetRandomTagName();
            Tag namedTag = CreateRandomTag();
            namedTag.Name = inputName;
            Tag otherTag = CreateRandomTag();
            otherTag.Name = GetRandomTagName();
            var storageTags = new List<Tag> { otherTag, namedTag };
            Tag inputTag = CreateRandomTag();
            inputTag.Name = inputName;
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            var expectedTagNameResult = new TagNameResult
            {
                Id = namedTag.Id,
                Outcome = TagNameOutcome.Found
            };

            SetupTagNameReadOver(storageTags, inputCancellationToken);

            // when
            TagNameResult actualTagNameResult =
                await this.tagService.FindOrAddTagAsync(
                    inputTag,
                    inputCancellationToken);

            // then
            actualTagNameResult.Should().BeEquivalentTo(expectedTagNameResult);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputTag),
                Times.Once);

            VerifyTagNameReadAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(FoundTagStates))]
        public async Task ShouldFindTheTagWhateverItsStatusPublicationOrOwnerAsync(
            string tagState)
        {
            // given: one row under the name in a state the condition must not exclude, beside a
            // row of another name; a term the condition must not have turns its case red
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string inputName = GetRandomTagName();
            Tag inputTag = CreateRandomTag();
            inputTag.Name = inputName;
            Tag namedTag = CreateRandomTag();
            namedTag.Name = inputName;
            namedTag.CreatedBy = inputTag.CreatedBy;
            ApplyFoundTagState(namedTag, tagState, randomDateTimeOffset);
            Tag otherTag = CreateRandomTag();
            otherTag.Name = GetRandomTagName();
            var storageTags = new List<Tag> { otherTag, namedTag };
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            var expectedTagNameResult = new TagNameResult
            {
                Id = namedTag.Id,
                Outcome = TagNameOutcome.Found
            };

            SetupTagNameReadOver(storageTags, inputCancellationToken);

            // when
            TagNameResult actualTagNameResult =
                await this.tagService.FindOrAddTagAsync(
                    inputTag,
                    inputCancellationToken);

            // then
            actualTagNameResult.Should().BeEquivalentTo(expectedTagNameResult);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputTag),
                Times.Once);

            VerifyTagNameReadAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(WithheldTagStates))]
        public async Task ShouldWithholdARejectedOrSoftDeletedTagAndLogItAsync(
            ApprovalStatus approvalStatus,
            bool isDeleted,
            string withheldReason)
        {
            // given: the row under the name is rejected, soft-deleted, or both, beside a row of
            // another name. The soft-deleted cases also prove the condition has no IsDeleted
            // term: a row it excluded would be added over, not withheld.
            string inputName = GetRandomTagName();
            Tag inputTag = CreateRandomTag();
            inputTag.Name = inputName;
            Tag namedTag = CreateRandomTag();
            namedTag.Name = inputName;
            namedTag.ApprovalStatus = approvalStatus;
            namedTag.IsDeleted = isDeleted;
            Tag otherTag = CreateRandomTag();
            otherTag.Name = GetRandomTagName();
            var storageTags = new List<Tag> { otherTag, namedTag };
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            var expectedTagNameResult = new TagNameResult
            {
                Id = namedTag.Id,
                Outcome = TagNameOutcome.Withheld
            };

            string expectedLogMessage =
                $"Tag name match withheld. Tag {namedTag.Id} is {withheldReason}; it was " +
                    "withheld from the suggestion, which pairs nothing with it, and the caller " +
                    "is not told so.";

            SetupTagNameReadOver(storageTags, inputCancellationToken);

            // when
            TagNameResult actualTagNameResult =
                await this.tagService.FindOrAddTagAsync(
                    inputTag,
                    inputCancellationToken);

            // then
            actualTagNameResult.Should().BeEquivalentTo(expectedTagNameResult);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputTag),
                Times.Once);

            VerifyTagNameReadAsked(inputCancellationToken, Times.Once());

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(expectedLogMessage),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // every row the find-or-add withholds, with the reason its log entry names: a rejected
        // row live or soft-deleted, and a soft-deleted row at each other status (§3 rule 5)
        public static TheoryData<ApprovalStatus, bool, string> WithheldTagStates() =>
            new TheoryData<ApprovalStatus, bool, string>
            {
                { ApprovalStatus.Rejected, false, "rejected" },
                { ApprovalStatus.Rejected, true, "rejected and soft-deleted" },
                { ApprovalStatus.Draft, true, "soft-deleted" },
                { ApprovalStatus.Submitted, true, "soft-deleted" },
                { ApprovalStatus.Approved, true, "soft-deleted" }
            };

        // every state a row the find-or-add finds may be in: the three statuses short of a
        // verdict against it, an approved row that is unpublished or scheduled, and another
        // caller's row (§3 rule 4)
        public static TheoryData<string> FoundTagStates() =>
            new TheoryData<string>
            {
                "Draft",
                "Submitted",
                "Approved",
                "ApprovedUnpublished",
                "ApprovedScheduled",
                "AnotherCallers"
            };

        private static void ApplyFoundTagState(
            Tag tag,
            string tagState,
            DateTimeOffset currentDateTime)
        {
            switch (tagState)
            {
                case "Draft":
                    tag.ApprovalStatus = ApprovalStatus.Draft;
                    break;

                case "Submitted":
                    tag.ApprovalStatus = ApprovalStatus.Submitted;
                    break;

                case "Approved":
                    tag.ApprovalStatus = ApprovalStatus.Approved;
                    tag.IsPublished = true;
                    tag.PublishDate = null;
                    break;

                case "ApprovedUnpublished":
                    tag.ApprovalStatus = ApprovalStatus.Approved;
                    tag.IsPublished = false;
                    tag.PublishDate = null;
                    break;

                case "ApprovedScheduled":
                    tag.ApprovalStatus = ApprovalStatus.Approved;
                    tag.IsPublished = true;
                    tag.PublishDate = currentDateTime.AddDays(GetRandomNumber());
                    break;

                case "AnotherCallers":
                    tag.CreatedBy = Guid.NewGuid().ToString();
                    tag.UpdatedBy = tag.CreatedBy;
                    break;
            }
        }

        // a name the add accepts: at most 30 characters (§3 rule 3)
        private static string GetRandomTagName() =>
            GetRandomStringWithLengthOf(length: 20);

        private void SetupTagNameReadOver(
            IEnumerable<Tag> storageTags,
            CancellationToken cancellationToken) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    cancellationToken))
                        .ReturnsAsync((
                            Func<IQueryable<Tag>, IQueryable<TagNameMatch>> query,
                            CancellationToken _) =>
                                (IReadOnlyList<TagNameMatch>)
                                    query(storageTags.AsQueryable()).ToList());

        private void VerifyTagNameReadAsked(CancellationToken cancellationToken, Times times) =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    cancellationToken),
                times);
    }
}
