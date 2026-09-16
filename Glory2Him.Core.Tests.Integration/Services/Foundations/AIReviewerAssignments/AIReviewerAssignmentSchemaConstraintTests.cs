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
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// Proves the AIReviewerAssignments table refuses to hold an assignment against a round that
    /// does not exist, and refuses to let such a round be destroyed underneath one.
    ///
    /// <para><b>Why the database and not the service.</b> The service is not the only way in.
    /// <c>AIReviewerAssignmentService</c> validates <c>ApprovalId</c> with <c>IsInvalid</c> only
    /// — it holds no <c>IAccessBroker</c> and cannot ask a second entity anything — so a
    /// fabricated identifier arriving on the <c>AIReviewerAssignment-Adding</c> substrate address
    /// passes every C# gate there is. Migrations, backfills and direct SQL bypass the service
    /// entirely. A constraint that exists only in C# is not a constraint, so every write below
    /// goes through the storage broker and asserts on what the DATABASE does — the same reasoning
    /// as <c>AssociationSchemaConstraintTests</c>.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class AIReviewerAssignmentSchemaConstraintTests : IDisposable
    {
        private const string ForeignKeyName = "FK_AIReviewerAssignments_Approvals_ApprovalId";

        private readonly NarrowReadQueryBroker broker;
        private readonly List<Approval> seededApprovals;
        private readonly List<AIReviewerAssignment> seededAIReviewerAssignments;

        public AIReviewerAssignmentSchemaConstraintTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededApprovals = new List<Approval>();
            this.seededAIReviewerAssignments = new List<AIReviewerAssignment>();
        }

        [Fact]
        public async Task ShouldRefuseAnAssignmentNamingARoundThatDoesNotExistAsync()
        {
            // given: an identifier no Approvals row carries — the shape a fabricated ApprovalId
            // arrives in on the substrate address, and the shape the hard-remove path leaves
            // behind
            Guid roundThatDoesNotExistId = Guid.NewGuid();

            AIReviewerAssignment orphanAssignment =
                CreateAIReviewerAssignment(roundThatDoesNotExistId, isDeleted: false);

            // registered for teardown BEFORE the attempt: if the refusal ever stops happening,
            // the row that should not exist is still cleared rather than left behind
            this.seededAIReviewerAssignments.Add(orphanAssignment);

            // when
            Exception outcome = await this.broker.TryInsertAsync(orphanAssignment);

            // then
            outcome.Should().BeOfType<ForeignKeyConstraintConflictException>(
                because: "SQL Server reports a foreign-key violation under error 547, which is "
                    + "the number EFxceptions maps to this type");

            outcome.Message.Should().Contain(ForeignKeyName);

            AIReviewerAssignment storedAssignment =
                await this.broker.ReadUntrackedAsync<AIReviewerAssignment>(orphanAssignment.Id);

            storedAssignment.Should().BeNull(
                because: "the write was refused, so nothing may be left in the table");
        }

        [Fact]
        public async Task ShouldAcceptAnAssignmentNamingARoundThatExistsWhetherLiveOrSoftDeletedAsync()
        {
            // given: two rounds that DO exist — one live, one soft-deleted. The soft-deleted
            // case is not an oversight and must never be "corrected" into a refusal: the key
            // enforces that the round EXISTS, and whether it is live or open stays with
            // IAccessBroker (§APR8.6.1). It is also the case the migration's anti-join must not
            // delete, which is why a repair narrowed with AND p.[IsDeleted] = 0 would contradict
            // this test.
            Approval liveRound = await SeedApprovalAsync(isDeleted: false);
            Approval softDeletedRound = await SeedApprovalAsync(isDeleted: true);

            AIReviewerAssignment assignmentOnLiveRound =
                CreateAIReviewerAssignment(liveRound.Id, isDeleted: false);

            AIReviewerAssignment assignmentOnSoftDeletedRound =
                CreateAIReviewerAssignment(softDeletedRound.Id, isDeleted: false);

            this.seededAIReviewerAssignments.Add(assignmentOnLiveRound);
            this.seededAIReviewerAssignments.Add(assignmentOnSoftDeletedRound);

            // when
            Exception liveOutcome = await this.broker.TryInsertAsync(assignmentOnLiveRound);

            Exception softDeletedOutcome =
                await this.broker.TryInsertAsync(assignmentOnSoftDeletedRound);

            // then
            liveOutcome.Should().BeNull(
                because: "the round exists, which is the whole of what the key asks");

            softDeletedOutcome.Should().BeNull(
                because: "the key does not read IsDeleted on the parent either — a withdrawn "
                    + "round still exists, and liveness is IAccessBroker's question");

            AIReviewerAssignment storedOnLiveRound =
                await this.broker.ReadUntrackedAsync<AIReviewerAssignment>(
                    assignmentOnLiveRound.Id);

            AIReviewerAssignment storedOnSoftDeletedRound =
                await this.broker.ReadUntrackedAsync<AIReviewerAssignment>(
                    assignmentOnSoftDeletedRound.Id);

            storedOnLiveRound.Should().BeEquivalentTo(assignmentOnLiveRound);
            storedOnSoftDeletedRound.Should().BeEquivalentTo(assignmentOnSoftDeletedRound);
        }

        [Fact]
        public async Task ShouldRefuseHardRemovingARoundThatStillCarriesAnAssignmentAsync()
        {
            // given: two rounds, one carrying a LIVE assignment and one carrying a WITHDRAWN
            // (soft-deleted) one. The second case is the one a reader gets wrong: withdrawing
            // the assignment frees UX_AIReviewerAssignments_ApprovalId's slot, but the foreign
            // key does not read IsDeleted, so it does not free the round.
            Approval roundWithLiveAssignment = await SeedApprovalAsync(isDeleted: false);
            Approval roundWithWithdrawnAssignment = await SeedApprovalAsync(isDeleted: false);

            AIReviewerAssignment liveAssignment =
                await SeedAIReviewerAssignmentAsync(roundWithLiveAssignment.Id, isDeleted: false);

            AIReviewerAssignment withdrawnAssignment =
                await SeedAIReviewerAssignmentAsync(
                    roundWithWithdrawnAssignment.Id, isDeleted: true);

            // when
            Exception liveOutcome = await this.broker.TryDeleteAsync(roundWithLiveAssignment);

            Exception withdrawnOutcome =
                await this.broker.TryDeleteAsync(roundWithWithdrawnAssignment);

            // then
            liveOutcome.Should().BeOfType<ForeignKeyConstraintConflictException>(
                because: "the round still carries an assignment, so the hard delete is refused");

            liveOutcome.Message.Should().Contain(ForeignKeyName);

            withdrawnOutcome.Should().BeOfType<ForeignKeyConstraintConflictException>(
                because: "the foreign key does not read IsDeleted — a withdrawn assignment "
                    + "still points at the round");

            withdrawnOutcome.Message.Should().Contain(ForeignKeyName);

            await AssertStillPresentAndUnmodifiedAsync(roundWithLiveAssignment);
            await AssertStillPresentAndUnmodifiedAsync(roundWithWithdrawnAssignment);
            await AssertStillPresentAndUnmodifiedAsync(liveAssignment);
            await AssertStillPresentAndUnmodifiedAsync(withdrawnAssignment);
        }

        private async Task AssertStillPresentAndUnmodifiedAsync(Approval approval)
        {
            Approval storedApproval =
                await this.broker.ReadUntrackedAsync<Approval>(approval.Id);

            storedApproval.Should().NotBeNull(
                because: "a refused delete leaves the round where it was");

            storedApproval.Should().BeEquivalentTo(approval);
        }

        private async Task AssertStillPresentAndUnmodifiedAsync(
            AIReviewerAssignment aiReviewerAssignment)
        {
            AIReviewerAssignment storedAssignment =
                await this.broker.ReadUntrackedAsync<AIReviewerAssignment>(
                    aiReviewerAssignment.Id);

            storedAssignment.Should().NotBeNull(
                because: "a refused delete touches nothing at all, least of all the child");

            storedAssignment.Should().BeEquivalentTo(aiReviewerAssignment);
        }

        private async Task<AIReviewerAssignment> SeedAIReviewerAssignmentAsync(
            Guid approvalId,
            bool isDeleted)
        {
            AIReviewerAssignment aiReviewerAssignment =
                CreateAIReviewerAssignment(approvalId, isDeleted);

            await this.broker.SeedAsync(aiReviewerAssignment);
            this.seededAIReviewerAssignments.Add(aiReviewerAssignment);

            return aiReviewerAssignment;
        }

        private async Task<Approval> SeedApprovalAsync(bool isDeleted)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approval = new Approval
            {
                Id = Guid.NewGuid(),

                // Association rather than the zero member, so a dropped key conjunct anywhere
                // downstream cannot match by defaulting to ContentItem.
                EntityType = EntityType.Association,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = ApprovalStatus.Submitted,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                IsDeleted = isDeleted,
                DeletedBy = isDeleted ? actorUserId : null,
                DeletedWhen = isDeleted ? now : null,
            };

            await this.broker.SeedAsync(approval);
            this.seededApprovals.Add(approval);

            return approval;
        }

        private static AIReviewerAssignment CreateAIReviewerAssignment(
            Guid approvalId,
            bool isDeleted)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new AIReviewerAssignment
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                IsAIReviewCompleted = false,
                IsAIReviewCommentsPresent = false,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                IsDeleted = isDeleted,
                DeletedBy = isDeleted ? actorUserId : null,
                DeletedWhen = isDeleted ? now : null,
                DeletionReason = isDeleted ? "seeded" : null,
            };
        }

        // Assignments first: they carry the FK, and the approvals cannot go while they point at
        // one.
        public void Dispose()
        {
            this.broker.ClearAsync(this.seededAIReviewerAssignments)
                .AsTask().GetAwaiter().GetResult();

            this.broker.ClearAsync(this.seededApprovals).AsTask().GetAwaiter().GetResult();
        }
    }
}
