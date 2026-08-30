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
using Glory2Him.WebApp.Tests.Acceptance.Models.BibleReferences;
using RESTFulSense.Exceptions;
using CoreBibleReference = Glory2Him.Core.Models.Foundations.BibleReferences.BibleReference;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.BibleReferences
{
    /// <summary>
    /// What the gates turn away, over real HTTP. The unit suite proves the attributes say what
    /// the design says; these prove the middleware and the foundation act on them — a gate that
    /// is present but never evaluated passes the former and fails here.
    /// </summary>
    public partial class BibleReferenceApiTests
    {
        [Fact]
        public async Task ShouldAllowAnonymousToReadBibleReferencesAsync()
        {
            // given
            BibleReference randomBibleReference = await PostRandomBibleReferenceAsync();

            try
            {
                // when
                this.apiBroker.ActAsAnonymous();
                List<BibleReference> actualBibleReferences = await this.apiBroker.GetAllBibleReferencesAsync();

                // then
                actualBibleReferences.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(randomBibleReference.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsAnonymousAsync()
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            this.apiBroker.ActAsAnonymous();

            // when
            var postBibleReferenceTask = this.apiBroker.PostBibleReferenceAsync(randomBibleReference).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postBibleReferenceTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnSubmitIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var submitBibleReferenceTask = this.apiBroker.SubmitBibleReferenceByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => submitBibleReferenceTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var hardDeleteTask = this.apiBroker.HardDeleteBibleReferenceByIdAsync(Guid.NewGuid()).AsTask();

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
            BibleReference randomBibleReference = await PostRandomBibleReferenceAsync();
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var hardDeleteTask =
                    this.apiBroker.HardDeleteBibleReferenceByIdAsync(randomBibleReference.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => hardDeleteTask);

                this.apiBroker.ActAsSeededAdministrator();
                CoreBibleReference survivingBibleReference = await this.apiBroker.GetCoreBibleReferenceByIdAsync(randomBibleReference.Id);
                survivingBibleReference.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(randomBibleReference.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnForbiddenOnApproveIfCallerIsNotInThePublisherTierAsync()
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            this.apiBroker.ActAsContributor();

            // when
            var approveBibleReferenceTask = this.apiBroker.TransitionBibleReferenceApprovalAsync(randomBibleReference).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => approveBibleReferenceTask);
        }

        /// <summary>
        /// A reviewer is not a publisher (HR-3). BibleReference-Reviewers clears no part of the approve
        /// gate, and the attribute must not admit it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnForbiddenOnApproveIfCallerIsOnlyAReviewerAsync()
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.BibleReferenceReviewers);

            // when
            var approveBibleReferenceTask = this.apiBroker.TransitionBibleReferenceApprovalAsync(randomBibleReference).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => approveBibleReferenceTask);
        }

        /// <summary>
        /// Past the attribute, the foundation decides ownership against the STORED row. A
        /// contributor who did not write the bibleReference is refused even though the coarse gate let them
        /// through — which is the whole point of enforcing at every layer (design §14.6).
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPutIfCallerIsNotTheOwnerAsync()
        {
            // given
            BibleReference randomBibleReference = await PostRandomBibleReferenceAsync();
            BibleReference modifiedBibleReference = UpdateBibleReferenceWithRandomValues(randomBibleReference);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var putBibleReferenceTask = this.apiBroker.PutBibleReferenceAsync(modifiedBibleReference).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => putBibleReferenceTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(randomBibleReference.Id);
            }
        }

        /// <summary>
        /// The same contributor, on a bibleReference they DID write, succeeds — so the refusal above is
        /// ownership and not merely the absence of a role.
        /// </summary>
        [Fact]
        public async Task ShouldAllowContributorToModifyTheirOwnBibleReferenceAsync()
        {
            // given
            string contributorUserId = this.apiBroker.ActAsContributor();
            BibleReference randomBibleReference = await PostRandomBibleReferenceAsync();
            BibleReference modifiedBibleReference = UpdateBibleReferenceWithRandomValues(randomBibleReference);

            try
            {
                // when
                BibleReference actualBibleReference = await this.apiBroker.PutBibleReferenceAsync(modifiedBibleReference);

                // then
                actualBibleReference.Reference.Should().Be(modifiedBibleReference.Reference);
                actualBibleReference.CreatedBy.Should().Be(contributorUserId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(randomBibleReference.Id);
            }
        }

        /// <summary>
        /// The review tier is owner-OR-role, so a reviewer may write a bibleReference they did not create.
        /// Both tiers are exercised — the global <c>Reviewers</c> and the entity-scoped
        /// <c>BibleReference-Reviewers</c> — because the foundation tests for both and seeding only one
        /// would leave half the rule dead.
        /// </summary>
        [Theory]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.BibleReferenceReviewers)]
        public async Task ShouldAllowReviewerToModifyAnotherUsersBibleReferenceAsync(string reviewRoleName)
        {
            // given
            BibleReference randomBibleReference = await PostRandomBibleReferenceAsync();
            BibleReference modifiedBibleReference = UpdateBibleReferenceWithRandomValues(randomBibleReference);
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), reviewRoleName);

            try
            {
                // when
                BibleReference actualBibleReference = await this.apiBroker.PutBibleReferenceAsync(modifiedBibleReference);

                // then
                actualBibleReference.Reference.Should().Be(modifiedBibleReference.Reference);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(randomBibleReference.Id);
            }
        }

        /// <summary>
        /// Removal is owner-or-Administrators, deliberately narrower than modify: a reviewer holds write
        /// permission on someone else's bibleReference but may not delete it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsNeitherOwnerNorAdministratorAsync()
        {
            // given
            BibleReference randomBibleReference = await PostRandomBibleReferenceAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.BibleReferenceReviewers);

            try
            {
                // when
                var deleteBibleReferenceTask = this.apiBroker.DeleteBibleReferenceByIdAsync(randomBibleReference.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteBibleReferenceTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(randomBibleReference.Id);
            }
        }

        /// <summary>
        /// The block tier (design §18.6): "assigned to users who misbehave, takes precedence over
        /// every other role". Both the global and the bibleReference-scoped block are refused, and the
        /// refusal survives the caller also holding Administrators — precedence is the whole point.
        /// </summary>
        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.BibleReferenceReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsBlockedAsync(string blockRoleName)
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), blockRoleName);

            // when
            var postBibleReferenceTask = this.apiBroker.PostBibleReferenceAsync(randomBibleReference).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postBibleReferenceTask);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.BibleReferenceReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfBlockedCallerAlsoHoldsAdminAsync(
            string blockRoleName)
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Administrators, blockRoleName);

            // when
            var postBibleReferenceTask = this.apiBroker.PostBibleReferenceAsync(randomBibleReference).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postBibleReferenceTask);
        }

        /// <summary>
        /// Hard delete is the one write whose coarse gate is a role list, so a blocked administrator
        /// clears the attribute and is stopped by the foundation instead.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfBlockedCallerAlsoHoldsAdminAsync()
        {
            // given
            BibleReference randomBibleReference = await PostRandomBibleReferenceAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Administrators, Roles.BibleReferenceReadOnly);

            try
            {
                // when
                var hardDeleteTask =
                    this.apiBroker.HardDeleteBibleReferenceByIdAsync(randomBibleReference.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(randomBibleReference.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfBibleReferenceDoesNotExistAsync()
        {
            // given
            Guid randomId = Guid.NewGuid();

            // when
            var getBibleReferenceTask = this.apiBroker.GetBibleReferenceByIdAsync(randomId).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getBibleReferenceTask);
        }

        [Fact]
        public async Task ShouldReturnConflictOnPostIfUsfmAlreadyExistsAsync()
        {
            // given
            BibleReference existingBibleReference = await PostRandomBibleReferenceAsync();
            BibleReference duplicateBibleReference = CreateRandomBibleReference();
            duplicateBibleReference.USFM = existingBibleReference.USFM;

            try
            {
                // when
                var postBibleReferenceTask =
                    this.apiBroker.PostBibleReferenceAsync(duplicateBibleReference).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseConflictException>(() => postBibleReferenceTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(existingBibleReference.Id);
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(duplicateBibleReference.Id);
            }
        }

        /// <summary>
        /// The INVERSE of the Reaction case, and the reason this test is written rather than
        /// copied across.
        ///
        /// <para><c>IX_Reactions_Name</c> and <c>IX_Tags_Name</c> are unique and unfiltered, so a
        /// soft-deleted row reserves its key forever (#201). <c>UX_BibleReferences_USFM</c>
        /// carries <c>HasFilter("[IsDeleted] = 0")</c>, so a taken-down passage key is genuinely
        /// released and can be used again.</para>
        ///
        /// <para>Asserted rather than assumed, because the two behaviours are one index filter
        /// apart and a reader who knows the Reaction defect would reasonably expect this entity
        /// to share it — design §12.3.1 rule 2a puts them side by side for that reason. If the
        /// filter is ever dropped, this test fails.</para>
        /// </summary>
        [Fact]
        public async Task ShouldAllowPostWhenUsfmIsHeldOnlyByASoftDeletedRowAsync()
        {
            // given
            BibleReference removedBibleReference = await PostRandomBibleReferenceAsync();
            await this.apiBroker.DeleteBibleReferenceByIdAsync(removedBibleReference.Id);

            BibleReference reusedUsfmBibleReference = CreateRandomBibleReference();
            reusedUsfmBibleReference.USFM = removedBibleReference.USFM;

            try
            {
                // when
                BibleReference actualBibleReference =
                    await this.apiBroker.PostBibleReferenceAsync(reusedUsfmBibleReference);

                // then
                actualBibleReference.USFM.Should().Be(removedBibleReference.USFM);
            }
            finally
            {
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(removedBibleReference.Id);
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(reusedUsfmBibleReference.Id);
            }
        }

        /// <summary>
        /// USFM is immutable after creation — the foundation pins it against the stored row on
        /// modify (design §12.3.1 rule 2a, §7.5.1 rule 4). This is the rule that keeps
        /// <c>BibleReference</c> Single-Row rather than versioned: the natural key a fork would
        /// violate is precisely why versioning it was withdrawn.
        ///
        /// <para>It refuses as a 400, not a 409 — nothing is competing for the key, the caller is
        /// asking for a change the entity does not permit. <c>Tag</c> and <c>Reaction</c> have no
        /// equivalent test because both permit a rename today.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnBadRequestOnPutIfUsfmIsChangedAsync()
        {
            // given
            BibleReference existingBibleReference = await PostRandomBibleReferenceAsync();

            BibleReference renamedBibleReference =
                UpdateBibleReferenceWithRandomValues(existingBibleReference);

            renamedBibleReference.USFM = GetRandomUsfm();

            try
            {
                // when
                var putBibleReferenceTask =
                    this.apiBroker.PutBibleReferenceAsync(renamedBibleReference).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(
                    () => putBibleReferenceTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreBibleReferenceByIdAsync(existingBibleReference.Id);
            }
        }
    }
}
