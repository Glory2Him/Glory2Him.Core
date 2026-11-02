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
using EFxceptions.Models.Exceptions;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
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

        [Fact]
        public async Task ShouldAddTheTagWhenNoTagHoldsTheNameAsync()
        {
            // given: rows of other names only, one of them a single character away from the
            // name, so a condition looser than equality finds it and adds nothing
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string inputName = GetRandomTagName();
            Tag inputTag = CreateTagFiller(randomDateTimeOffset).Create();
            inputTag.Name = inputName;
            inputTag.ApprovalStatus = ApprovalStatus.Submitted;
            Tag nearlyNamedTag = CreateRandomTag();

            nearlyNamedTag.Name =
                inputName.Substring(0, inputName.Length - 1)
                    + (inputName[^1] == 'a' ? 'b' : 'a');

            Tag otherTag = CreateRandomTag();
            otherTag.Name = GetRandomTagName();
            var storageTags = new List<Tag> { nearlyNamedTag, otherTag };
            Guid mintedId = Guid.NewGuid();
            Guid firstProcessedEventId = Guid.NewGuid();
            Guid secondProcessedEventId = Guid.NewGuid();
            Tag auditAppliedTag = inputTag.DeepClone();
            auditAppliedTag.Id = mintedId;
            Tag storageTag = auditAppliedTag.DeepClone();
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            var expectedTagNameResult = new TagNameResult
            {
                Id = mintedId,
                Outcome = TagNameOutcome.Added
            };

            SetupTagNameReadOver(storageTags, inputCancellationToken);

            // the add mints the row's Id, its first call to the identifier broker; the two
            // that follow are the processed-event records written after the insert
            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(mintedId)
                    .ReturnsAsync(firstProcessedEventId)
                    .ReturnsAsync(secondProcessedEventId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.Is<Tag>(tag => tag == inputTag && tag.Id == mintedId),
                    this.ambientSecurityContext))
                        .ReturnsAsync(auditAppliedTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(auditAppliedTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertTagAsync(auditAppliedTag, inputCancellationToken))
                    .ReturnsAsync(storageTag);

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

            // one Id for the new row, and one for each of the two processed-event records
            this.identifierBrokerMock.Verify(broker =>
                    broker.GetIdentifierAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(
                        It.Is<Tag>(tag => tag == inputTag && tag.Id == mintedId),
                        this.ambientSecurityContext),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertTagAsync(auditAppliedTag, inputCancellationToken),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishTagAsync(
                    It.Is<EventEnvelope<Tag>>(envelope =>
                        envelope.Content == storageTag
                            && envelope.Content.Id == mintedId),
                    TagEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.Id == firstProcessedEventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
                    inputCancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.Id == secondProcessedEventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
                    inputCancellationToken),
                Times.Once);

            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(TagStatesAnotherCallerAdded))]
        public async Task ShouldAnswerTheTagAnotherCallerAddedWhenTheNameIsTakenAsync(
            ApprovalStatus approvalStatus,
            TagNameOutcome expectedOutcome,
            string withheldReason)
        {
            // given: no row holds the name when it is first read, and by the insert another
            // caller has added it, so IX_Tags_Name refuses the write; the second read finds
            // their row
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string inputName = GetRandomTagName();
            Tag inputTag = CreateTagFiller(randomDateTimeOffset).Create();
            inputTag.Name = inputName;
            Tag otherTag = CreateRandomTag();
            otherTag.Name = GetRandomTagName();
            Tag anotherCallersTag = CreateRandomTag();
            anotherCallersTag.Name = inputName;
            anotherCallersTag.ApprovalStatus = approvalStatus;
            var firstStorageTags = new List<Tag> { otherTag };
            var secondStorageTags = new List<Tag> { otherTag, anotherCallersTag };
            Guid mintedId = Guid.NewGuid();
            Tag auditAppliedTag = inputTag.DeepClone();
            auditAppliedTag.Id = mintedId;
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            var duplicateKeyWithUniqueIndexException =
                new DuplicateKeyWithUniqueIndexException(message: GetRandomString());

            var expectedTagNameResult = new TagNameResult
            {
                Id = anotherCallersTag.Id,
                Outcome = expectedOutcome
            };

            string expectedLogMessage =
                $"Tag name match withheld. Tag {anotherCallersTag.Id} is {withheldReason}; it " +
                    "was withheld from the suggestion, which pairs nothing with it, and the " +
                    "caller is not told so.";

            SetupTagNameReadsOver(
                new[] { firstStorageTags, secondStorageTags },
                inputCancellationToken);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(mintedId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.Is<Tag>(tag => tag == inputTag && tag.Id == mintedId),
                    this.ambientSecurityContext))
                        .ReturnsAsync(auditAppliedTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(auditAppliedTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertTagAsync(auditAppliedTag, inputCancellationToken))
                    .ThrowsAsync(duplicateKeyWithUniqueIndexException);

            // when
            TagNameResult actualTagNameResult =
                await this.tagService.FindOrAddTagAsync(
                    inputTag,
                    inputCancellationToken);

            // then: the name is read again and answered as the first read would have answered
            // that row, and the insert is never tried twice
            actualTagNameResult.Should().BeEquivalentTo(expectedTagNameResult);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputTag),
                Times.Once);

            VerifyTagNameReadAsked(inputCancellationToken, Times.Exactly(2));

            this.identifierBrokerMock.Verify(broker =>
                    broker.GetIdentifierAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(
                        It.Is<Tag>(tag => tag == inputTag && tag.Id == mintedId),
                        this.ambientSecurityContext),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(this.ambientSecurityContext),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertTagAsync(auditAppliedTag, inputCancellationToken),
                Times.Once);

            if (expectedOutcome == TagNameOutcome.Withheld)
            {
                this.loggingBrokerMock.Verify(broker =>
                    broker.LogInformationAsync(expectedLogMessage),
                    Times.Once);
            }

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldShapeTheNameReadToOneRowAsync()
        {
            // given: two rows under the name, which IX_Tags_Name makes impossible in SQL and
            // the in-memory set allows, so the shaping of the query is what answers one
            string inputName = GetRandomTagName();
            Tag inputTag = CreateRandomTag();
            inputTag.Name = inputName;
            Tag firstNamedTag = CreateRandomTag();
            firstNamedTag.Name = inputName;
            Tag secondNamedTag = CreateRandomTag();
            secondNamedTag.Name = inputName;
            var storageTags = new List<Tag> { firstNamedTag, secondNamedTag };
            IReadOnlyList<TagNameMatch> actualTagNameMatches = null;
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    inputCancellationToken))
                        .ReturnsAsync((
                            Func<IQueryable<Tag>, IQueryable<TagNameMatch>> query,
                            CancellationToken _) =>
                                actualTagNameMatches =
                                    query(storageTags.AsQueryable()).ToList());

            // when
            await this.tagService.FindOrAddTagAsync(
                inputTag,
                inputCancellationToken);

            // then
            actualTagNameMatches.Should().ContainSingle();
            VerifyTagNameReadAsked(inputCancellationToken, Times.Once());
        }

        // the row another caller added between the read and the write, one the second read
        // finds and one it withholds (§3 rule 7)
        public static TheoryData<ApprovalStatus, TagNameOutcome, string> TagStatesAnotherCallerAdded() =>
            new TheoryData<ApprovalStatus, TagNameOutcome, string>
            {
                { ApprovalStatus.Submitted, TagNameOutcome.Found, string.Empty },
                { ApprovalStatus.Rejected, TagNameOutcome.Withheld, "rejected" }
            };

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

        // one stored set per read, in the order the reads are asked
        private void SetupTagNameReadsOver(
            IEnumerable<IEnumerable<Tag>> storageTagsPerRead,
            CancellationToken cancellationToken)
        {
            var storageTagsQueue = new Queue<IEnumerable<Tag>>(storageTagsPerRead);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    cancellationToken))
                        .ReturnsAsync((
                            Func<IQueryable<Tag>, IQueryable<TagNameMatch>> query,
                            CancellationToken _) =>
                                (IReadOnlyList<TagNameMatch>)
                                    query(storageTagsQueue.Dequeue().AsQueryable()).ToList());
        }

        private void VerifyTagNameReadAsked(CancellationToken cancellationToken, Times times) =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    cancellationToken),
                times);
    }
}
