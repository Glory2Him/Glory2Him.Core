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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Securities;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Access
{
    /// <summary>
    /// The <c>ReadOnly</c> veto at tier 2 (design §18.6 rule 2, §12.5.3). This is where the
    /// SCOPED half of the block lives: an <c>Approval</c> carries an entity type and an entity
    /// id but no content type, and a foundation may not resolve the entity behind it (§14.3) —
    /// by the time a request reaches this decision function its <c>RoleSubject</c> list is
    /// already resolved, so the narrow name can be composed.
    ///
    /// <para>Two things every case here holds to. The veto is asked <b>before</b> eligibility,
    /// and unlike every tier check it <b>cannot be satisfied by a wider role</b> — the
    /// <c>Administrators</c> rows are the ones a future refactor is most likely to hoist into an
    /// early allow, so they are written out rather than assumed.</para>
    ///
    /// <para>The mirror half is asserted just as often: a block whose scope does not cover the
    /// subject is <b>silent</b>. Without those cases somebody could widen the composition to
    /// every content type and the suite would still pass.</para>
    /// </summary>
    public partial class AccessServiceTests
    {
        private const string ContentItemEntityType = "ContentItem";
        private const string QuoteContentType = "Quote";
        private const string StoryContentType = "Story";

        /// <summary>
        /// The three scopes a block can be spelled at, each paired with the subject it is asked
        /// about. All three cover a quote, so all three fire.
        /// </summary>
        public static TheoryData<string> BlocksCoveringAQuote() =>
            new TheoryData<string>
            {
                RoleNames.ReadOnly,
                RoleNames.ReadOnlyFor(ContentItemEntityType),
                RoleNames.ReadOnlyFor(ContentItemEntityType, QuoteContentType),
            };

        /// <summary>
        /// Every grant the veto has to outrank, widest last. None of them rescues the row.
        /// </summary>
        public static TheoryData<string> GrantsTheVetoOutranks() =>
            new TheoryData<string>
            {
                RoleNames.ReviewersFor(ContentItemEntityType, QuoteContentType),
                RoleNames.PublishersFor(ContentItemEntityType, QuoteContentType),
                RoleNames.ReviewersFor(ContentItemEntityType),
                RoleNames.PublishersFor(ContentItemEntityType),
                RoleNames.Reviewers,
                RoleNames.Publishers,
                RoleNames.Administrators,
            };

        private static IReadOnlyList<RoleSubject> QuoteSubject() =>
            new List<RoleSubject>
            {
                new RoleSubject
                {
                    EntityType = ContentItemEntityType,
                    ContentType = QuoteContentType,
                },
            };

        // ── The vote ─────────────────────────────────────────────────────────────────

        [Theory]
        [MemberData(nameof(GrantsTheVetoOutranks))]
        public async Task ShouldRefuseRecordingAReviewWhenTheNarrowBlockCoversTheSubjectAsync(
            string grantTheVetoOutranks)
        {
            // given
            AccessActor blockedActor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, QuoteContentType),
                    grantTheVetoOutranks,
                });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: blockedActor,
                roleSubjects: QuoteSubject());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Theory]
        [MemberData(nameof(BlocksCoveringAQuote))]
        public async Task ShouldRefuseRecordingAReviewAtEveryScopeTheBlockIsSpelledAtAsync(
            string blockCoveringAQuote)
        {
            // given: the same subject, the block written at each of its three widths in turn.
            // A gate that composed only one of them would leave the other two unenforced here.
            AccessActor blockedActor = CreateRandomAccessActor(
                roles: new List<string> { blockCoveringAQuote, RoleNames.Reviewers });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: blockedActor,
                roleSubjects: QuoteSubject());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Fact]
        public async Task ShouldPermitRecordingAReviewWhenTheBlockNamesADifferentContentTypeAsync()
        {
            // given: a Story block says nothing about a Quote. Not weakened, not outvoted —
            // simply not asked.
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, StoryContentType),
                    RoleNames.ReviewersFor(ContentItemEntityType, QuoteContentType),
                });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: actor,
                roleSubjects: QuoteSubject());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldPermitRecordingAReviewWhenTheBlockNamesADifferentEntityTypeAsync()
        {
            // given: the composed name carries the entity type as well, so a Tag block never
            // matches a ContentItem subject however its content type is spelled.
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor("Tag"),
                    RoleNames.ReadOnlyFor("Tag", QuoteContentType),
                    RoleNames.ReviewersFor(ContentItemEntityType, QuoteContentType),
                });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: actor,
                roleSubjects: QuoteSubject());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldRefuseChangingAStandingReviewOnceTheBlockAppliesAsync()
        {
            // given: the amendment path, which §7.7 rule 1 otherwise admits — a reviewer
            // revising their own standing verdict after a conversation is the normal case. The
            // veto governs what they may do NEXT: no new vote, and no change to the one they
            // hold.
            string reviewerUserId = GetRandomString();

            AccessActor blockedActor = CreateRandomAccessActor(
                userId: reviewerUserId,
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, QuoteContentType),
                    RoleNames.ReviewersFor(ContentItemEntityType, QuoteContentType),
                });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: blockedActor,
                roleSubjects: QuoteSubject(),
                existingReviews: new List<ReviewRecord>
                {
                    CreateRandomReviewRecord(createdBy: reviewerUserId),
                },
                isAmendingOwnReview: true);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Fact]
        public async Task ShouldStillCountAVoteCastBeforeTheBlockWasAppliedAsync()
        {
            // given: blocking somebody is NOT retroactive. A review they cast while eligible
            // remains a fact of that round and keeps counting toward its required reviews, and
            // nothing recomputes when a role is assigned — so no approval in flight silently
            // re-opens and there is no sweep to build.
            //
            // The conditions evaluation is where that is observable: it takes no actor at all,
            // which is the structural reason the block cannot reach backwards.
            //
            // Read this for what it is. ApprovalConditionsRequest has no Actor and no
            // RoleSubjects members, so nothing this branch wrote can regress it — it is a
            // STRUCTURAL pin, not a veto pin, and it holds only while that request type stays
            // actor-free. That is the property worth guarding: the day somebody threads an
            // actor into the conditions evaluation, retroactive sanctioning becomes expressible
            // and this case is where the argument against it is written down. The veto's own
            // half of the rule — no NEW or CHANGED vote once blocked — is pinned separately by
            // ShouldRefuseChangingAStandingReviewOnceTheBlockAppliesAsync above.
            string blockedReviewerUserId = GetRandomString();

            ApprovalPolicy policy = CreateRandomApprovalPolicy(
                entityType: ContentItemEntityType,
                contentType: QuoteContentType,
                requireApprovals: true,
                requiredNumberOfApprovals: 1);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateApprovalConditionsRequestFor(
                    approvalPolicy: policy,
                    reviews: new List<ReviewRecord>
                    {
                        CreateRandomReviewRecord(createdBy: blockedReviewerUserId),
                    });

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.ApprovalCount.Should().Be(1);
            actualVerdict.AreConditionsMet.Should().BeTrue();
        }

        // ── The decision ─────────────────────────────────────────────────────────────

        [Theory]
        [MemberData(nameof(GrantsTheVetoOutranks))]
        public async Task ShouldRefuseDecidingAnApprovalWhenTheNarrowBlockCoversTheSubjectAsync(
            string grantTheVetoOutranks)
        {
            // given
            AccessActor blockedActor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, QuoteContentType),
                    grantTheVetoOutranks,
                });

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: blockedActor,
                roleSubjects: QuoteSubject());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Fact]
        public async Task ShouldRefuseDecidingAnApprovalWithABypassWhenTheBlockCoversTheSubjectAsync()
        {
            // given: the bypass waives the §8.5 CONDITIONS, never the veto. A route that
            // survived a block would make the block advisory.
            AccessActor blockedActor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, QuoteContentType),
                    RoleNames.Administrators,
                });

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: blockedActor,
                roleSubjects: QuoteSubject(),
                isBypassRequested: true,
                bypassReason: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByReadOnlyRole);

            actualVerdict.IsBypassUsed.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldPermitDecidingAnApprovalWhenTheBlockNamesADifferentContentTypeAsync()
        {
            // given
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, StoryContentType),
                    RoleNames.PublishersFor(ContentItemEntityType, QuoteContentType),
                });

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: actor,
                roleSubjects: QuoteSubject());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        // ── The approval record ──────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldRefuseAmendingTheirOwnApprovalWhenTheBlockCoversTheSubjectAsync()
        {
            // given: the owner edge. §14.7 posture D rule 3 admits the submitter to their own
            // approval so they can resubmit it, and they hold no role by construction — but the
            // owner admit is a grant like any other and the veto outranks it, so the block is
            // asked BEFORE the ownership question.
            string submitterUserId = GetRandomString();

            AccessActor blockedSubmitter = CreateRandomAccessActor(
                userId: submitterUserId,
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, QuoteContentType),
                });

            AmendApprovalRequest amendApprovalRequest = CreateRandomAmendApprovalRequest(
                actor: blockedSubmitter,
                roleSubjects: QuoteSubject(),
                entityCreatedBy: submitterUserId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Fact]
        public async Task ShouldPermitAmendingTheirOwnApprovalWhenTheBlockNamesADifferentContentTypeAsync()
        {
            // given: the same submitter, a block that does not cover their quote.
            string submitterUserId = GetRandomString();

            AccessActor actor = CreateRandomAccessActor(
                userId: submitterUserId,
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, StoryContentType),
                });

            AmendApprovalRequest amendApprovalRequest = CreateRandomAmendApprovalRequest(
                actor: actor,
                roleSubjects: QuoteSubject(),
                entityCreatedBy: submitterUserId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalAsync(amendApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        // ── Associations: one endpoint admits, one endpoint bars ─────────────────────

        // ── The unresolved entity: unknown is not the same as absent ────────────────

        private static IReadOnlyList<RoleSubject> UnresolvedContentItemSubject() =>
            new List<RoleSubject>
            {
                new RoleSubject
                {
                    EntityType = ContentItemEntityType,
                    ContentType = null,
                    IsEntityUnresolved = true,
                },
            };

        [Fact]
        public async Task ShouldRefuseDecidingWhenTheEntityIsUnresolvedAndANarrowBlockIsHeldAsync()
        {
            // given: the content item behind this approval was hard-removed, so the gatherer
            // could not read its content type and the narrow name cannot be composed. Failing
            // open here would hand a narrowly sanctioned user an orphaned approval their coarse
            // tier never covered — the one direction a veto may not err.
            AccessActor blockedActor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, QuoteContentType),
                    RoleNames.Publishers,
                });

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: blockedActor,
                roleSubjects: UnresolvedContentItemSubject());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Fact]
        public async Task ShouldPermitDecidingWhenTheEntityIsUnresolvedAndNoBlockIsHeldAsync()
        {
            // given: the fail-closed branch fires only for somebody the narrow tier covers. An
            // unsanctioned publisher still clears up an orphaned approval, so this does not turn
            // a missing row into a refusal for everyone.
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.Publishers });

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: actor,
                roleSubjects: UnresolvedContentItemSubject());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldRefuseAnUnresolvedSubjectForAnyScopedBlockNotOnlyItsOwnEntityTypeAsync()
        {
            // given: the association fallback. When the association itself cannot be read the
            // gatherer can name no endpoint, so the subject it emits is UNNAMEABLE — a blank
            // entity type. That is the case where the scope itself is unknown, so any scoped
            // block the actor holds may cover it. (It used to borrow the name "Association",
            // an entity type that issues no scoped roles at all — which made the fail-closed
            // branch decoration, since it restricts to the named type's own roles.)
            AccessActor blockedActor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, QuoteContentType),
                    RoleNames.Publishers,
                });

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: blockedActor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject
                    {
                        EntityType = string.Empty,
                        ContentType = null,
                        IsEntityUnresolved = true,
                    },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Fact]
        public async Task ShouldRefuseAnUnresolvedNamedSubjectOnlyForItsOwnEntityTypesBlocksAsync()
        {
            // given: an association endpoint that IS a content item and whose content type
            // could not be decided. The entity type is KNOWN, so only ContentItem's own block
            // names can cover it — a Tag sanction provably cannot, and §18.6 rule 2 requires a
            // block to be silent outside its scope. Failing closed on every scoped name would
            // bar this actor permanently, because the endpoint's null content type is a stored
            // column pinned against storage on modify: nothing ever backfills it.
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor("Tag"),
                    RoleNames.ReadOnlyFor("Tag", QuoteContentType),
                    RoleNames.Publishers,
                });

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: actor,
                roleSubjects: UnresolvedContentItemSubject());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldNotRefuseAnUnresolvedSubjectForTheGlobalBlockSpellingAloneAsync()
        {
            // given: the bare global "ReadOnly" carries no hyphen, so the suffix match reaches
            // only the SCOPED names. An actor holding it is already refused by the first clause
            // of IsBlockedFromSubjects; an actor holding none of them must still get through,
            // or an unresolved subject would refuse everybody rather than only the sanctioned.
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.Publishers, RoleNames.Reviewers });

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: actor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject
                    {
                        EntityType = string.Empty,
                        ContentType = null,
                        IsEntityUnresolved = true,
                    },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldNotTreatAnAbsentContentTypeAsUnresolvedOnAnEntityThatCarriesNoneAsync()
        {
            // given: a Tag subject legitimately carries no content type, and the flag is what
            // separates that from a content item nobody could read. Without the flag this same
            // shape would bar a Tag reviewer holding an unrelated ContentItem sanction.
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, QuoteContentType),
                    RoleNames.ReviewersFor("Tag"),
                });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: actor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject { EntityType = "Tag", ContentType = null },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        public static TheoryData<string, string> BlockedEndpointAgainstGrantedEndpoint() =>
            new TheoryData<string, string>
            {
                { "Series", QuoteContentType },
                { QuoteContentType, "Series" },
            };

        [Theory]
        [MemberData(nameof(BlockedEndpointAgainstGrantedEndpoint))]
        public async Task ShouldRefuseRecordingAReviewWhenEitherAssociationEndpointIsBlockedAsync(
            string blockedContentType,
            string grantedContentType)
        {
            // given: an association names BOTH endpoints as subjects, and the same list serves
            // the grant and the block read opposite ways. One end admits; one end bars — so a
            // grant on the opposite end never rescues the pairing.
            AccessActor blockedActor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, blockedContentType),
                    RoleNames.ReviewersFor(ContentItemEntityType, grantedContentType),
                });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: blockedActor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject
                    {
                        EntityType = ContentItemEntityType,
                        ContentType = "Series",
                    },
                    new RoleSubject
                    {
                        EntityType = ContentItemEntityType,
                        ContentType = QuoteContentType,
                    },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Fact]
        public async Task ShouldPermitRecordingAReviewOnAnAssociationWhenNeitherEndpointIsBlockedAsync()
        {
            // given: the grant half is unchanged — one endpoint is enough to admit, and a block
            // on a content type neither end carries is silent.
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string>
                {
                    RoleNames.ReadOnlyFor(ContentItemEntityType, "Testimony"),
                    RoleNames.ReviewersFor(ContentItemEntityType, QuoteContentType),
                });

            RecordReviewRequest recordReviewRequest = CreateRandomRecordReviewRequest(
                actor: actor,
                roleSubjects: new List<RoleSubject>
                {
                    new RoleSubject
                    {
                        EntityType = ContentItemEntityType,
                        ContentType = "Series",
                    },
                    new RoleSubject
                    {
                        EntityType = ContentItemEntityType,
                        ContentType = QuoteContentType,
                    },
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalReviewAsync(recordReviewRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }
    }
}
