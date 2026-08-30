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
using FluentAssertions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Models.Reactions;
using RESTFulSense.Exceptions;
using CoreReaction = Glory2Him.Core.Models.Foundations.Reactions.Reaction;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Reactions
{
    /// <summary>
    /// What the gates turn away, over real HTTP. The unit suite proves the attributes say what
    /// the design says; these prove the middleware and the foundation act on them — a gate that
    /// is present but never evaluated passes the former and fails here.
    /// </summary>
    public partial class ReactionApiTests
    {
        [Fact]
        public async Task ShouldAllowAnonymousToReadReactionsAsync()
        {
            // given
            Reaction randomReaction = await PostRandomReactionAsync();

            try
            {
                // when
                this.apiBroker.ActAsAnonymous();
                List<Reaction> actualReactions = await this.apiBroker.GetAllReactionsAsync();

                // then
                actualReactions.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreReactionByIdAsync(randomReaction.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsAnonymousAsync()
        {
            // given
            Reaction randomReaction = CreateRandomReaction();
            this.apiBroker.ActAsAnonymous();

            // when
            var postReactionTask = this.apiBroker.PostReactionAsync(randomReaction).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postReactionTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnSubmitIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var submitReactionTask = this.apiBroker.SubmitReactionByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => submitReactionTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var hardDeleteTask = this.apiBroker.HardDeleteReactionByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);
        }

        /// <summary>
        /// A signed-in contributor is not an administrator. Design §14.7 posture A rule 3 restricts hard
        /// removal to Administrators, and the attribute must turn the caller away before the service is
        /// reached — so the row is still there afterwards.
        /// </summary>
        [Fact]
        public async Task ShouldReturnForbiddenOnHardDeleteIfCallerIsNotAdministratorAsync()
        {
            // given
            Reaction randomReaction = await PostRandomReactionAsync();
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var hardDeleteTask =
                    this.apiBroker.HardDeleteReactionByIdAsync(randomReaction.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => hardDeleteTask);

                this.apiBroker.ActAsSeededAdministrator();
                CoreReaction survivingReaction = await this.apiBroker.GetCoreReactionByIdAsync(randomReaction.Id);
                survivingReaction.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreReactionByIdAsync(randomReaction.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnForbiddenOnApproveIfCallerIsNotInThePublisherTierAsync()
        {
            // given
            Reaction randomReaction = CreateRandomReaction();
            this.apiBroker.ActAsContributor();

            // when
            var approveReactionTask = this.apiBroker.TransitionReactionApprovalAsync(randomReaction).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => approveReactionTask);
        }

        /// <summary>
        /// A reviewer is not a publisher (HR-3). Reaction-Reviewers clears no part of the approve
        /// gate, and the attribute must not admit it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnForbiddenOnApproveIfCallerIsOnlyAReviewerAsync()
        {
            // given
            Reaction randomReaction = CreateRandomReaction();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.ReactionReviewers);

            // when
            var approveReactionTask = this.apiBroker.TransitionReactionApprovalAsync(randomReaction).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => approveReactionTask);
        }

        /// <summary>
        /// Past the attribute, the foundation decides ownership against the STORED row. A
        /// contributor who did not write the reaction is refused even though the coarse gate let them
        /// through — which is the whole point of enforcing at every layer (design §14.6).
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPutIfCallerIsNotTheOwnerAsync()
        {
            // given
            Reaction randomReaction = await PostRandomReactionAsync();
            Reaction modifiedReaction = UpdateReactionWithRandomValues(randomReaction);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var putReactionTask = this.apiBroker.PutReactionAsync(modifiedReaction).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => putReactionTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreReactionByIdAsync(randomReaction.Id);
            }
        }

        /// <summary>
        /// The same contributor, on a reaction they DID write, succeeds — so the refusal above is
        /// ownership and not merely the absence of a role.
        /// </summary>
        [Fact]
        public async Task ShouldAllowContributorToModifyTheirOwnReactionAsync()
        {
            // given
            string contributorUserId = this.apiBroker.ActAsContributor();
            Reaction randomReaction = await PostRandomReactionAsync();
            Reaction modifiedReaction = UpdateReactionWithRandomValues(randomReaction);

            try
            {
                // when
                Reaction actualReaction = await this.apiBroker.PutReactionAsync(modifiedReaction);

                // then
                actualReaction.Name.Should().Be(modifiedReaction.Name);
                actualReaction.CreatedBy.Should().Be(contributorUserId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreReactionByIdAsync(randomReaction.Id);
            }
        }

        /// <summary>
        /// The review tier is owner-OR-role, so a reviewer may write a reaction they did not create.
        /// Both tiers are exercised — the global <c>Reviewers</c> and the entity-scoped
        /// <c>Reaction-Reviewers</c> — because the foundation tests for both and seeding only one
        /// would leave half the rule dead.
        /// </summary>
        [Theory]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.ReactionReviewers)]
        public async Task ShouldAllowReviewerToModifyAnotherUsersReactionAsync(string reviewRoleName)
        {
            // given
            Reaction randomReaction = await PostRandomReactionAsync();
            Reaction modifiedReaction = UpdateReactionWithRandomValues(randomReaction);
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), reviewRoleName);

            try
            {
                // when
                Reaction actualReaction = await this.apiBroker.PutReactionAsync(modifiedReaction);

                // then
                actualReaction.Name.Should().Be(modifiedReaction.Name);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreReactionByIdAsync(randomReaction.Id);
            }
        }

        /// <summary>
        /// Removal is owner-or-Admin, deliberately narrower than modify: a reviewer holds write
        /// permission on someone else's reaction but may not delete it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsNeitherOwnerNorAdministratorAsync()
        {
            // given
            Reaction randomReaction = await PostRandomReactionAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.ReactionReviewers);

            try
            {
                // when
                var deleteReactionTask = this.apiBroker.DeleteReactionByIdAsync(randomReaction.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteReactionTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreReactionByIdAsync(randomReaction.Id);
            }
        }

        /// <summary>
        /// The block tier (design §18.6): "assigned to users who misbehave, takes precedence over
        /// every other role". Both the global and the reaction-scoped block are refused, and the
        /// refusal survives the caller also holding Administrators — precedence is the whole point.
        /// </summary>
        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ReactionReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsBlockedAsync(string blockRoleName)
        {
            // given
            Reaction randomReaction = CreateRandomReaction();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), blockRoleName);

            // when
            var postReactionTask = this.apiBroker.PostReactionAsync(randomReaction).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postReactionTask);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ReactionReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfBlockedCallerAlsoHoldsAdminAsync(
            string blockRoleName)
        {
            // given
            Reaction randomReaction = CreateRandomReaction();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Administrators, blockRoleName);

            // when
            var postReactionTask = this.apiBroker.PostReactionAsync(randomReaction).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postReactionTask);
        }

        /// <summary>
        /// Hard delete is the one write whose coarse gate is a role list, so a blocked administrator
        /// clears the attribute and is stopped by the foundation instead.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfBlockedCallerAlsoHoldsAdminAsync()
        {
            // given
            Reaction randomReaction = await PostRandomReactionAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Administrators, Roles.ReactionReadOnly);

            try
            {
                // when
                var hardDeleteTask =
                    this.apiBroker.HardDeleteReactionByIdAsync(randomReaction.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreReactionByIdAsync(randomReaction.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfReactionDoesNotExistAsync()
        {
            // given
            Guid randomId = Guid.NewGuid();

            // when
            var getReactionTask = this.apiBroker.GetReactionByIdAsync(randomId).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getReactionTask);
        }

        [Fact]
        public async Task ShouldReturnConflictOnPostIfReactionNameAlreadyExistsAsync()
        {
            // given
            Reaction existingReaction = await PostRandomReactionAsync();
            Reaction duplicateReaction = CreateRandomReaction();
            duplicateReaction.Name = existingReaction.Name;

            try
            {
                // when
                var postReactionTask = this.apiBroker.PostReactionAsync(duplicateReaction).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseConflictException>(() => postReactionTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreReactionByIdAsync(existingReaction.Id);
                await this.apiBroker.RemoveCoreReactionByIdAsync(duplicateReaction.Id);
            }
        }

        /// <summary>
        /// Pins #201 rather than fixing it: a SOFT-DELETED reaction keeps its name reserved
        /// forever, so the name can never be used again by anybody.
        ///
        /// <para><c>IX_Reactions_Name</c> is unique and — unlike
        /// <c>UX_BibleReferences_USFM</c> — carries no <c>IsDeleted</c> term, so a taken-down
        /// row still occupies its key (design §12.3.1 rule 2a). Over HTTP that surfaces as a
        /// bare 409 with nothing to tell the caller that the name they are being refused is held
        /// by a row they cannot see; the API's delete is a soft delete, so an administrator who
        /// removes "Amen" and re-creates it hits this and has no way to diagnose it.</para>
        ///
        /// <para>The behaviour is asserted here rather than papered over in the controller,
        /// because the fix is a filtered index and belongs in #201. What this test buys is that
        /// the fix cannot land unnoticed: when the index gains its <c>IsDeleted</c> term, this
        /// test fails and says so.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnConflictOnPostIfReactionNameIsHeldByASoftDeletedRowAsync()
        {
            // given
            Reaction removedReaction = await PostRandomReactionAsync();
            await this.apiBroker.DeleteReactionByIdAsync(removedReaction.Id);

            Reaction reusedNameReaction = CreateRandomReaction();
            reusedNameReaction.Name = removedReaction.Name;

            try
            {
                // when
                var postReactionTask =
                    this.apiBroker.PostReactionAsync(reusedNameReaction).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseConflictException>(() => postReactionTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreReactionByIdAsync(removedReaction.Id);
                await this.apiBroker.RemoveCoreReactionByIdAsync(reusedNameReaction.Id);
            }
        }
    }
}
