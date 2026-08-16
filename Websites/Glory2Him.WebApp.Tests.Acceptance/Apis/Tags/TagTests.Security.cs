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
using Glory2Him.WebApp.Tests.Acceptance.Models.Tags;
using RESTFulSense.Exceptions;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Tags
{
    /// <summary>
    /// What the gates turn away, over real HTTP. The unit suite proves the attributes say what
    /// the design says; these prove the middleware and the foundation act on them — a gate that
    /// is present but never evaluated passes the former and fails here.
    /// </summary>
    public partial class TagApiTests
    {
        [Fact]
        public async Task ShouldAllowAnonymousToReadTagsAsync()
        {
            // given
            Tag randomTag = await PostRandomTagAsync();

            try
            {
                // when
                this.apiBroker.ActAsAnonymous();
                List<Tag> actualTags = await this.apiBroker.GetAllTagsAsync();

                // then
                actualTags.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreTagByIdAsync(randomTag.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsAnonymousAsync()
        {
            // given
            Tag randomTag = CreateRandomTag();
            this.apiBroker.ActAsAnonymous();

            // when
            var postTagTask = this.apiBroker.PostTagAsync(randomTag).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTagTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnSubmitIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var submitTagTask = this.apiBroker.SubmitTagByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => submitTagTask);
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfCallerIsAnonymousAsync()
        {
            // given
            this.apiBroker.ActAsAnonymous();

            // when
            var hardDeleteTask = this.apiBroker.HardDeleteTagByIdAsync(Guid.NewGuid()).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);
        }

        /// <summary>
        /// A signed-in contributor is not an Admin. Design §14.7 posture A rule 3 restricts hard
        /// removal to Admin, and the attribute must turn the caller away before the service is
        /// reached — so the row is still there afterwards.
        /// </summary>
        [Fact]
        public async Task ShouldReturnForbiddenOnHardDeleteIfCallerIsNotAdministratorAsync()
        {
            // given
            Tag randomTag = await PostRandomTagAsync();
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var hardDeleteTask =
                    this.apiBroker.HardDeleteTagByIdAsync(randomTag.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => hardDeleteTask);

                this.apiBroker.ActAsSeededAdministrator();
                CoreTag survivingTag = await this.apiBroker.GetCoreTagByIdAsync(randomTag.Id);
                survivingTag.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreTagByIdAsync(randomTag.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnForbiddenOnApproveIfCallerIsNotInThePublisherTierAsync()
        {
            // given
            Tag randomTag = CreateRandomTag();
            this.apiBroker.ActAsContributor();

            // when
            var approveTagTask = this.apiBroker.ApproveTagAsync(randomTag).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => approveTagTask);
        }

        /// <summary>
        /// A reviewer is not a publisher (HR-3). Tag-Reviewer clears no part of the approve
        /// gate, and the attribute must not admit it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnForbiddenOnApproveIfCallerIsOnlyAReviewerAsync()
        {
            // given
            Tag randomTag = CreateRandomTag();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.TagReviewer);

            // when
            var approveTagTask = this.apiBroker.ApproveTagAsync(randomTag).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseForbiddenException>(() => approveTagTask);
        }

        /// <summary>
        /// Past the attribute, the foundation decides ownership against the STORED row. A
        /// contributor who did not write the tag is refused even though the coarse gate let them
        /// through — which is the whole point of enforcing at every layer (design §14.6).
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPutIfCallerIsNotTheOwnerAsync()
        {
            // given
            Tag randomTag = await PostRandomTagAsync();
            Tag modifiedTag = UpdateTagWithRandomValues(randomTag);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var putTagTask = this.apiBroker.PutTagAsync(modifiedTag).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => putTagTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreTagByIdAsync(randomTag.Id);
            }
        }

        /// <summary>
        /// The same contributor, on a tag they DID write, succeeds — so the refusal above is
        /// ownership and not merely the absence of a role.
        /// </summary>
        [Fact]
        public async Task ShouldAllowContributorToModifyTheirOwnTagAsync()
        {
            // given
            string contributorUserId = this.apiBroker.ActAsContributor();
            Tag randomTag = await PostRandomTagAsync();
            Tag modifiedTag = UpdateTagWithRandomValues(randomTag);

            try
            {
                // when
                Tag actualTag = await this.apiBroker.PutTagAsync(modifiedTag);

                // then
                actualTag.Name.Should().Be(modifiedTag.Name);
                actualTag.CreatedBy.Should().Be(contributorUserId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreTagByIdAsync(randomTag.Id);
            }
        }

        /// <summary>
        /// Removal is owner-or-Admin, deliberately narrower than modify: a Reviewer holds write
        /// permission on someone else's tag but may not delete it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsNeitherOwnerNorAdministratorAsync()
        {
            // given
            Tag randomTag = await PostRandomTagAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.TagReviewer);

            try
            {
                // when
                var deleteTagTask = this.apiBroker.DeleteTagByIdAsync(randomTag.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteTagTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreTagByIdAsync(randomTag.Id);
            }
        }

        /// <summary>
        /// The block tier (design §18.6): "assigned to users who misbehave, takes precedence over
        /// every other role". Both the global and the tag-scoped block are refused, and the
        /// refusal survives the caller also holding Admin — precedence is the whole point.
        /// </summary>
        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.TagReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsBlockedAsync(string blockRoleName)
        {
            // given
            Tag randomTag = CreateRandomTag();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), blockRoleName);

            // when
            var postTagTask = this.apiBroker.PostTagAsync(randomTag).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTagTask);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.TagReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfBlockedCallerAlsoHoldsAdminAsync(
            string blockRoleName)
        {
            // given
            Tag randomTag = CreateRandomTag();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Admin, blockRoleName);

            // when
            var postTagTask = this.apiBroker.PostTagAsync(randomTag).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postTagTask);
        }

        /// <summary>
        /// Hard delete is the one write whose coarse gate is a role list, so a blocked Admin
        /// clears the attribute and is stopped by the foundation instead.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnHardDeleteIfBlockedCallerAlsoHoldsAdminAsync()
        {
            // given
            Tag randomTag = await PostRandomTagAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Admin, Roles.TagReadOnly);

            try
            {
                // when
                var hardDeleteTask =
                    this.apiBroker.HardDeleteTagByIdAsync(randomTag.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => hardDeleteTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreTagByIdAsync(randomTag.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfTagDoesNotExistAsync()
        {
            // given
            Guid randomId = Guid.NewGuid();

            // when
            var getTagTask = this.apiBroker.GetTagByIdAsync(randomId).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getTagTask);
        }

        [Fact]
        public async Task ShouldReturnConflictOnPostIfTagNameAlreadyExistsAsync()
        {
            // given
            Tag existingTag = await PostRandomTagAsync();
            Tag duplicateTag = CreateRandomTag();
            duplicateTag.Name = existingTag.Name;

            try
            {
                // when
                var postTagTask = this.apiBroker.PostTagAsync(duplicateTag).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseConflictException>(() => postTagTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreTagByIdAsync(existingTag.Id);
                await this.apiBroker.RemoveCoreTagByIdAsync(duplicateTag.Id);
            }
        }
    }
}
