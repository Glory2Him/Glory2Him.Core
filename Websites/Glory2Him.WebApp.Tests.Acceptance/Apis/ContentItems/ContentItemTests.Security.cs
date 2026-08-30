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
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems;
using RESTFulSense.Exceptions;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItems
{
    /// <summary>
    /// What the gates turn away, over real HTTP. The unit suite proves the attributes say what
    /// the design says; these prove the middleware and the foundation act on them — a gate that
    /// is present but never evaluated passes the former and fails here.
    /// </summary>
    public partial class ContentItemApiTests
    {
        [Fact]
        public async Task ShouldAllowAnonymousToReadContentItemsAsync()
        {
            // given
            ContentItem randomContentItem = await PostRandomContentItemAsync();

            try
            {
                // when
                this.apiBroker.ActAsAnonymous();
                List<ContentItem> actualContentItems = await this.apiBroker.GetAllContentItemsAsync();

                // then
                actualContentItems.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(randomContentItem.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsAnonymousAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            this.apiBroker.ActAsAnonymous();

            // when
            var postContentItemTask = this.apiBroker.PostContentItemAsync(randomContentItem).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postContentItemTask);
        }






        /// <summary>
        /// Past the attribute, the foundation decides ownership against the STORED row. A
        /// contributor who did not write the contentItem is refused even though the coarse gate let them
        /// through — which is the whole point of enforcing at every layer (design §14.6).
        /// </summary>
        [Fact]
        public async Task ShouldReturnNotFoundOnPutIfCallerIsNotTheOwnerAsync()
        {
            // given
            ContentItem randomContentItem = await PostRandomContentItemAsync();
            ContentItem modifiedContentItem = UpdateContentItemWithRandomValues(randomContentItem);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var putContentItemTask = this.apiBroker.PutContentItemAsync(modifiedContentItem).AsTask();

                // then
                // NOT-FOUND, not unauthorized, and the difference is the point. The posture A
                // foundations answer 401 here; ContentItemProcessingService answers not-found,
                // which is the stricter reading of the 14.5 denial posture - a caller who may not
                // see a row must not learn it exists by being told they may not edit it. Asserted
                // as the behaviour that actually ships rather than copied from the Tag suite.
                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => putContentItemTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(randomContentItem.Id);
            }
        }

        /// <summary>
        /// The same contributor, on a contentItem they DID write, succeeds — so the refusal above is
        /// ownership and not merely the absence of a role.
        /// </summary>
        [Fact]
        public async Task ShouldAllowContributorToModifyTheirOwnContentItemAsync()
        {
            // given
            string contributorUserId = this.apiBroker.ActAsContributor();
            ContentItem randomContentItem = await PostRandomContentItemAsync();
            ContentItem modifiedContentItem = UpdateContentItemWithRandomValues(randomContentItem);

            try
            {
                // when
                ContentItem actualContentItem = await this.apiBroker.PutContentItemAsync(modifiedContentItem);

                // then
                actualContentItem.Title.Should().Be(modifiedContentItem.Title);
                actualContentItem.CreatedBy.Should().Be(contributorUserId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(randomContentItem.Id);
            }
        }

        /// <summary>
        /// The review tier is owner-OR-role, so a reviewer may write a contentItem they did not create.
        /// Both tiers are exercised — the global <c>Reviewers</c> and the entity-scoped
        /// <c>ContentItem-Reviewers</c> — because the foundation tests for both and seeding only one
        /// would leave half the rule dead.
        /// </summary>
        [Theory]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.ContentItemReviewers)]
        public async Task ShouldAllowReviewerToModifyAnotherUsersContentItemAsync(string reviewRoleName)
        {
            // given
            ContentItem randomContentItem = await PostRandomContentItemAsync();
            ContentItem modifiedContentItem = UpdateContentItemWithRandomValues(randomContentItem);
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), reviewRoleName);

            try
            {
                // when
                ContentItem actualContentItem = await this.apiBroker.PutContentItemAsync(modifiedContentItem);

                // then
                actualContentItem.Title.Should().Be(modifiedContentItem.Title);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(randomContentItem.Id);
            }
        }

        /// <summary>
        /// Removal is owner-or-Administrators, deliberately narrower than modify: a reviewer holds write
        /// permission on someone else's contentItem but may not delete it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsNeitherOwnerNorAdministratorAsync()
        {
            // given
            ContentItem randomContentItem = await PostRandomContentItemAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.ContentItemReviewers);

            try
            {
                // when
                var deleteContentItemTask = this.apiBroker.DeleteContentItemByIdAsync(randomContentItem.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteContentItemTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreContentItemByIdAsync(randomContentItem.Id);
            }
        }

        /// <summary>
        /// The block tier (design §18.6): "assigned to users who misbehave, takes precedence over
        /// every other role". Both the global and the contentItem-scoped block are refused, and the
        /// refusal survives the caller also holding Administrators — precedence is the whole point.
        /// </summary>
        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsBlockedAsync(string blockRoleName)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), blockRoleName);

            // when
            var postContentItemTask = this.apiBroker.PostContentItemAsync(randomContentItem).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postContentItemTask);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfBlockedCallerAlsoHoldsAdminAsync(
            string blockRoleName)
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Administrators, blockRoleName);

            // when
            var postContentItemTask = this.apiBroker.PostContentItemAsync(randomContentItem).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postContentItemTask);
        }


        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfContentItemDoesNotExistAsync()
        {
            // given
            Guid randomId = Guid.NewGuid();

            // when
            var getContentItemTask = this.apiBroker.GetContentItemByIdAsync(randomId).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getContentItemTask);
        }



        // NO CONFLICT AND NO HARD-DELETE TESTS HERE, and both absences are decisions.
        //
        // ContentItem has no natural key. §3.4.2's duplicate rule is keyed on (ContentType,
        // ContentHash) and answers a duplicate ADD with a polite acknowledgement rather than
        // creating — so there is no 409 for this entity to provoke through an add at all.
        //
        // Hard delete, submit and approve are not on this exposer: the processing service has no
        // public member for the first two, and the approve command arrives as an event so the
        // publication swap can order two writes on one call stack. The controller's XML doc
        // records which of those three is a gap (#316) and which is the design.
    }
}
