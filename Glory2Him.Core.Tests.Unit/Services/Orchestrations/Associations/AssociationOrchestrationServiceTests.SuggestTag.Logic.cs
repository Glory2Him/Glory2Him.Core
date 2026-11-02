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
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldPairTheTagTheFindOrAddFoundAsync(bool isTagVisible)
        {
            // given: the find-or-add finds a tag that holds the name, one the caller can see or
            // one the tag's own read answers them not found, and neither probe finds a row for its
            // pair. Whether the caller can see the tag decides the answer, and never the insert, so
            // the pair is inserted either way (TagSuggestions.md rules 4 and 5; §1 rule 1, steps 9,
            // 11 and 13).
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            ContentItem suggestedPost = SetupSuggestionForThePost(tagSuggestion);

            Guid foundTagId =
                SetupFindOrAddOf(tagSuggestion.Name, TagNameOutcome.Found);

            SetupTagReadFor(foundTagId, isTagVisible);

            Association expectedInsertedPair =
                CreateSuggestedPairFor(suggestedPost, foundTagId, ApprovalStatus.Submitted);

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

            // then
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.tagServiceMock.Verify(service =>
                service.FindOrAddTagAsync(
                    It.Is(SameTagAs(CreateSuggestedTagNamed(tagSuggestion.Name))),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.tagServiceMock.Verify(service =>
                service.RetrieveTagByIdAsync(
                    foundTagId,
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

        // Thirty characters, a tag name's cap (TagSuggestions.md rule 2).
        private const string ThirtyCharacterTagName = "Grace And Truth Through Christ";

        // Each name as typed, and the name the find-or-add is to be handed: trimmed at both ends,
        // and nothing else changed, a hash, its letter case and its inner white space included.
        // A name of thirty characters once trimmed is not refused, though it is longer as typed.
        public static TheoryData<string, string> SuggestedNamesAndTheirTrimmedNames() =>
            new TheoryData<string, string>
            {
                { "  Grace  ", "Grace" },
                { "#Grace ", "#Grace" },
                { "gRace  Notes", "gRace  Notes" },
                { $"  {ThirtyCharacterTagName}  ", ThirtyCharacterTagName },
            };

        [Theory]
        [MemberData(nameof(SuggestedNamesAndTheirTrimmedNames))]
        public async Task ShouldTrimTheNameBeforeItIsHandedToTheFindOrAddAsync(
            string suggestedName,
            string expectedTagName)
        {
            // given: a contributor types a name with white space around it. The server trims it at
            // both ends and changes nothing else, and every step after reads the trimmed name
            // (TagSuggestions.md rule 2; §1 rule 1, step 3).
            this.ambientSecurityContext = CreateReaderSecurityContext(GetRandomString());
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            tagSuggestion.Name = suggestedName;
            ContentItem suggestedPost = SetupSuggestionForThePost(tagSuggestion);

            Guid addedTagId =
                SetupFindOrAddOf(expectedTagName, TagNameOutcome.Added);

            Association expectedInsertedPair =
                CreateSuggestedPairFor(suggestedPost, addedTagId, ApprovalStatus.Submitted);

            Association insertedPair = expectedInsertedPair.DeepClone();
            insertedPair.Id = Guid.NewGuid();

            this.associationServiceMock.Setup(service =>
                service.AddAssociationAsync(
                    It.Is(SameAssociationAs(expectedInsertedPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(insertedPair);

            // when
            AssociationSubmissionResult actualResult =
                await this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Status.Should().Be(AssociationSubmissionStatus.Created);

            this.tagServiceMock.Verify(service =>
                service.FindOrAddTagAsync(
                    It.Is(SameTagAs(CreateSuggestedTagNamed(expectedTagName))),
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

        private const string ExactPairProbe = "the exact-pair probe";
        private const string OverlapProbe = "the overlap probe";

        // Every row that can hold or cover a suggestion's pair, by the probe that finds it — the
        // exact-pair probe, or the overlap probe where that finds no row, which answers a live row
        // only — and the answer a caller who can see the tag is given for it (§1 rule 2). The
        // caller's soft-deleted rows are at Draft and Submitted, the statuses their live row
        // answers AlreadySuggestedByCaller at, so a member that left the deletion unasked would
        // answer them so too.
        private static IEnumerable<(
            string Probe,
            ApprovalStatus RowStatus,
            bool IsRowDeleted,
            bool IsCallersRow,
            AssociationSubmissionStatus Answer)> RowsHoldingOrCoveringThePair()
        {
            foreach (string probe in new[] { ExactPairProbe, OverlapProbe })
            {
                yield return (probe, ApprovalStatus.Draft, false, true,
                    AssociationSubmissionStatus.AlreadySuggestedByCaller);

                yield return (probe, ApprovalStatus.Submitted, false, true,
                    AssociationSubmissionStatus.AlreadySuggestedByCaller);

                yield return (probe, ApprovalStatus.Approved, false, true,
                    AssociationSubmissionStatus.PreviouslySuggestedByCaller);

                yield return (probe, ApprovalStatus.Rejected, false, true,
                    AssociationSubmissionStatus.PreviouslySuggestedByCaller);
            }

            yield return (ExactPairProbe, ApprovalStatus.Draft, true, true,
                AssociationSubmissionStatus.PreviouslySuggestedByCaller);

            yield return (ExactPairProbe, ApprovalStatus.Submitted, true, true,
                AssociationSubmissionStatus.PreviouslySuggestedByCaller);

            foreach (ApprovalStatus rowStatus in new[]
            {
                ApprovalStatus.Draft,
                ApprovalStatus.Submitted,
                ApprovalStatus.Approved,
                ApprovalStatus.Rejected,
            })
            {
                yield return (ExactPairProbe, rowStatus, false, false,
                    AssociationSubmissionStatus.AlreadySuggested);

                yield return (ExactPairProbe, rowStatus, true, false,
                    AssociationSubmissionStatus.AlreadySuggested);

                yield return (OverlapProbe, rowStatus, false, false,
                    AssociationSubmissionStatus.AlreadySuggested);
            }
        }

        public static TheoryData<string, ApprovalStatus, bool, bool, AssociationSubmissionStatus>
            RowsHoldingThePairAndTheirAnswers()
        {
            var rows = new TheoryData<string, ApprovalStatus, bool, bool, AssociationSubmissionStatus>();

            foreach (var row in RowsHoldingOrCoveringThePair())
            {
                rows.Add(row.Probe, row.RowStatus, row.IsRowDeleted, row.IsCallersRow, row.Answer);
            }

            return rows;
        }

        [Theory]
        [MemberData(nameof(RowsHoldingThePairAndTheirAnswers))]
        public async Task ShouldAnswerByWhoseRowHoldsThePairAsync(
            string probe,
            ApprovalStatus rowStatus,
            bool isRowDeleted,
            bool isCallersRow,
            AssociationSubmissionStatus expectedStatus)
        {
            // given: a contributor suggests a tag they can see, and a row already holds its pair,
            // or covers it at one of the post's versions, a publisher having re-scoped it. Nothing
            // is inserted, and the answer is whose the row is, its CreatedBy compared ordinally
            // with the envelope's subject, and, for the caller's own, whether it is still pending
            // (§1 rule 2; TagSuggestions.md rule 7).
            string callerUserId = CreateCallerUserId();
            this.ambientSecurityContext = CreateReaderSecurityContext(callerUserId);
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            ContentItem suggestedPost = SetupSuggestionForThePost(tagSuggestion);

            Guid foundTagId =
                SetupFindOrAddOf(tagSuggestion.Name, TagNameOutcome.Found);

            SetupTagReadFor(foundTagId, isTagVisible: true);

            Association expectedProbedPair =
                CreateSuggestedPairFor(suggestedPost, foundTagId, ApprovalStatus.Draft);

            AssociationPairMatch occupant =
                CreatePairMatchCreatedBy(
                    isCallersRow ? callerUserId : CreateAnotherCallersUserIdLike(callerUserId),
                    rowStatus,
                    isRowDeleted);

            SetupPairOccupant(expectedProbedPair, probe, occupant);

            var expectedResult = new AssociationSubmissionResult
            {
                Status = expectedStatus,
            };

            // when
            AssociationSubmissionResult actualResult =
                await this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            // then: answered by the row, and nothing written
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.tagServiceMock.Verify(service =>
                service.FindOrAddTagAsync(
                    It.Is(SameTagAs(CreateSuggestedTagNamed(tagSuggestion.Name))),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.tagServiceMock.Verify(service =>
                service.RetrieveTagByIdAsync(
                    foundTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            VerifyPairProbedUpTo(expectedProbedPair, probe);

            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private const string AnotherCallersInsert = "another caller's insert";

        // Every row criterion 4 names, by the probe that finds it, and the pair free at both probes
        // and then taken by another caller's insert, between the probes and this one.
        public static TheoryData<string, ApprovalStatus, bool, bool> HoldersOfThePairOfATagTheCallerCannotSee()
        {
            var holders = new TheoryData<string, ApprovalStatus, bool, bool>();

            foreach (var row in RowsHoldingOrCoveringThePair())
            {
                holders.Add(row.Probe, row.RowStatus, row.IsRowDeleted, row.IsCallersRow);
            }

            holders.Add(AnotherCallersInsert, ApprovalStatus.Submitted, false, false);

            return holders;
        }

        [Theory]
        [MemberData(nameof(HoldersOfThePairOfATagTheCallerCannotSee))]
        public async Task ShouldAnswerCreatedForATagTheCallerCannotSeeWhateverHoldsThePairAsync(
            string holder,
            ApprovalStatus rowStatus,
            bool isRowDeleted,
            bool isCallersRow)
        {
            // given: a contributor suggests a name another reader's hidden tag holds, so the tag's
            // own read answers them not found, and the pair is held by a row, whoever's, or is
            // taken between the probes and the insert. Every suggestion of a tag the caller cannot
            // see is answered Created, so nothing tells it from a tag they created (TagSuggestions.md
            // rule 5, "A withheld tag suggested twice", and rule 10, "Two readers pairing one tag at
            // once"). The not-found and the insert's refusal are steps of the flow: nothing is
            // thrown or logged for either.
            string callerUserId = CreateCallerUserId();
            this.ambientSecurityContext = CreateReaderSecurityContext(callerUserId);
            TagSuggestion tagSuggestion = CreateTagSuggestion();
            ContentItem suggestedPost = SetupSuggestionForThePost(tagSuggestion);

            Guid hiddenTagId =
                SetupFindOrAddOf(tagSuggestion.Name, TagNameOutcome.Found);

            SetupTagReadFor(hiddenTagId, isTagVisible: false);

            Association expectedProbedPair =
                CreateSuggestedPairFor(suggestedPost, hiddenTagId, ApprovalStatus.Draft);

            Association expectedInsertedPair =
                CreateSuggestedPairFor(suggestedPost, hiddenTagId, ApprovalStatus.Submitted);

            if (holder == AnotherCallersInsert)
            {
                SetupPairFreeAtBothProbes(expectedProbedPair);

                this.associationServiceMock.Setup(service =>
                    service.AddAssociationAsync(
                        It.Is(SameAssociationAs(expectedInsertedPair)),
                        TestContext.Current.CancellationToken))
                            .ThrowsAsync(CreatePairAlreadyHeldException());
            }
            else
            {
                AssociationPairMatch occupant =
                    CreatePairMatchCreatedBy(
                        isCallersRow ? callerUserId : CreateAnotherCallersUserIdLike(callerUserId),
                        rowStatus,
                        isRowDeleted);

                SetupPairOccupant(expectedProbedPair, holder, occupant);
            }

            var expectedResult = new AssociationSubmissionResult
            {
                Status = AssociationSubmissionStatus.Created,
            };

            // when
            AssociationSubmissionResult actualResult =
                await this.associationOrchestrationService.SuggestTagAsync(
                    tagSuggestion,
                    TestContext.Current.CancellationToken);

            // then: where a probe found the row, nothing is inserted
            actualResult.Should().BeEquivalentTo(expectedResult);

            this.tagServiceMock.Verify(service =>
                service.FindOrAddTagAsync(
                    It.Is(SameTagAs(CreateSuggestedTagNamed(tagSuggestion.Name))),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.tagServiceMock.Verify(service =>
                service.RetrieveTagByIdAsync(
                    hiddenTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            if (holder == AnotherCallersInsert)
            {
                // the probes and the add are handed the one pair, and Moq reads it as it stands
                // once the call is through
                VerifyPairProbedUpTo(expectedInsertedPair, OverlapProbe);

                this.associationServiceMock.Verify(service =>
                    service.AddAssociationAsync(
                        It.Is(SameAssociationAs(expectedInsertedPair)),
                        TestContext.Current.CancellationToken),
                    Times.Once);
            }
            else
            {
                VerifyPairProbedUpTo(expectedProbedPair, holder);
            }

            this.tagServiceMock.VerifyNoOtherCalls();
            this.associationServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private void SetupPairFreeAtBothProbes(Association expectedProbedPair)
        {
            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.Is(SameAssociationAs(expectedProbedPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((AssociationPairMatch)null);

            this.associationServiceMock.Setup(service =>
                service.FindOverlappingAssociationAsync(
                    It.Is(SameAssociationAs(expectedProbedPair)),
                    (Guid?)null,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((AssociationPairMatch)null);
        }

        // The add's refusal of a pair the editorial pair index already holds, as the foundation
        // raises it.
        private static AssociationDependencyValidationException CreatePairAlreadyHeldException()
        {
            var duplicateKeyException = new Exception(GetRandomString());

            return new AssociationDependencyValidationException(
                message: "Association dependency validation occurred, fix the errors and try again.",
                innerException: new AlreadyExistsAssociationException(
                    message: "Association with the same id already exists.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data));
        }

        // A caller's user id, in lower case, so another caller's can differ from it in case alone.
        private static string CreateCallerUserId() =>
            $"reader-{Guid.NewGuid()}";

        // Another caller's user id, unlike the caller's in letter case alone, so a comparison that
        // ignored case would claim their row as the caller's (§1 rule 2: compared ordinally).
        private static string CreateAnotherCallersUserIdLike(string callerUserId) =>
            callerUserId.ToUpperInvariant();

        private static AssociationPairMatch CreatePairMatchCreatedBy(
            string createdBy,
            ApprovalStatus approvalStatus,
            bool isDeleted)
        {
            AssociationPairMatch pairMatch = CreatePairMatch(approvalStatus, isDeleted);
            pairMatch.CreatedBy = createdBy;

            return pairMatch;
        }

        // The row the named probe finds for the pair: the exact-pair probe's, or, where that finds
        // none, the overlap probe's.
        private void SetupPairOccupant(
            Association expectedProbedPair,
            string probe,
            AssociationPairMatch occupant)
        {
            this.associationServiceMock.Setup(service =>
                service.FindAssociationByPairAsync(
                    It.Is(SameAssociationAs(expectedProbedPair)),
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(probe == ExactPairProbe ? occupant : null);

            if (probe == OverlapProbe)
            {
                this.associationServiceMock.Setup(service =>
                    service.FindOverlappingAssociationAsync(
                        It.Is(SameAssociationAs(expectedProbedPair)),
                        (Guid?)null,
                        TestContext.Current.CancellationToken))
                            .ReturnsAsync(occupant);
            }
        }

        // The exact-pair probe, and the overlap probe where the row is the overlap probe's.
        private void VerifyPairProbedUpTo(Association expectedProbedPair, string probe)
        {
            this.associationServiceMock.Verify(service =>
                service.FindAssociationByPairAsync(
                    It.Is(SameAssociationAs(expectedProbedPair)),
                    TestContext.Current.CancellationToken),
                Times.Once);

            if (probe == OverlapProbe)
            {
                this.associationServiceMock.Verify(service =>
                    service.FindOverlappingAssociationAsync(
                        It.Is(SameAssociationAs(expectedProbedPair)),
                        (Guid?)null,
                        TestContext.Current.CancellationToken),
                    Times.Once);
            }
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

        // Whether the caller can see the tag, as the tag's own read answers it under the caller:
        // the tag, or not found, which that read answers for a row the caller may not see and for
        // every soft-deleted one (§SEC14.7 posture A rule 4; §SEC14.5 rules 1 and 3).
        private void SetupTagReadFor(Guid tagId, bool isTagVisible)
        {
            if (isTagVisible)
            {
                this.tagServiceMock.Setup(service =>
                    service.RetrieveTagByIdAsync(tagId, TestContext.Current.CancellationToken))
                        .ReturnsAsync(new Tag { Id = tagId });

                return;
            }

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(tagId, TestContext.Current.CancellationToken))
                    .ThrowsAsync(CreateTagNotFoundException(tagId));
        }

        private static TagValidationException CreateTagNotFoundException(Guid tagId) =>
            new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: new NotFoundTagException(
                    message: $"Couldn't find tag with id: {tagId}."));

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
