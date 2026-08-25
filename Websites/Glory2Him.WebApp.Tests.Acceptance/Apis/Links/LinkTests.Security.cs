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
using Glory2Him.WebApp.Tests.Acceptance.Models.Links;
using RESTFulSense.Exceptions;
using CoreLink = Glory2Him.Core.Models.Foundations.Links.Link;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Links
{
    /// <summary>
    /// What the gates turn away, over real HTTP. The unit suite proves the attributes say what
    /// the design says; these prove the middleware and the foundation act on them — a gate that
    /// is present but never evaluated passes the former and fails here.
    /// </summary>
    public partial class LinkApiTests
    {
        [Fact]
        public async Task ShouldAllowAnonymousToReadLinksAsync()
        {
            // given
            Link randomLink = await PostRandomLinkAsync();

            try
            {
                // when
                this.apiBroker.ActAsAnonymous();
                List<Link> actualLinks = await this.apiBroker.GetAllLinksAsync();

                // then
                actualLinks.Should().NotBeNull();
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreLinkByIdAsync(randomLink.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsAnonymousAsync()
        {
            // given
            Link randomLink = CreateRandomLink();
            this.apiBroker.ActAsAnonymous();

            // when
            var postLinkTask = this.apiBroker.PostLinkAsync(randomLink).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postLinkTask);
        }






        /// <summary>
        /// Past the attribute, the foundation decides ownership against the STORED row. A
        /// contributor who did not write the link is refused even though the coarse gate let them
        /// through — which is the whole point of enforcing at every layer (design §14.6).
        /// </summary>
        [Fact]
        public async Task ShouldReturnNotFoundOnPutIfCallerIsNotTheOwnerAsync()
        {
            // given
            Link randomLink = await PostRandomLinkAsync();
            Link modifiedLink = UpdateLinkWithRandomValues(randomLink);
            this.apiBroker.ActAsContributor();

            try
            {
                // when
                var putLinkTask = this.apiBroker.PutLinkAsync(modifiedLink).AsTask();

                // then
                // NOT-FOUND, not unauthorized, and the difference is the point. The posture A
                // foundations answer 401 here; LinkProcessingService answers not-found,
                // which is the stricter reading of the 14.5 denial posture - a caller who may not
                // see a row must not learn it exists by being told they may not edit it. Asserted
                // as the behaviour that actually ships rather than copied from the Tag suite.
                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => putLinkTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreLinkByIdAsync(randomLink.Id);
            }
        }

        /// <summary>
        /// The same contributor, on a link they DID write, succeeds — so the refusal above is
        /// ownership and not merely the absence of a role.
        /// </summary>
        [Fact]
        public async Task ShouldAllowContributorToModifyTheirOwnLinkAsync()
        {
            // given
            string contributorUserId = this.apiBroker.ActAsContributor();
            Link randomLink = await PostRandomLinkAsync();
            Link modifiedLink = UpdateLinkWithRandomValues(randomLink);

            try
            {
                // when
                Link actualLink = await this.apiBroker.PutLinkAsync(modifiedLink);

                // then
                actualLink.Name.Should().Be(modifiedLink.Name);
                actualLink.CreatedBy.Should().Be(contributorUserId);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreLinkByIdAsync(randomLink.Id);
            }
        }

        /// <summary>
        /// The review tier is owner-OR-role, so a reviewer may write a link they did not create.
        /// Both tiers are exercised — the global <c>Reviewer</c> and the entity-scoped
        /// <c>Link-Reviewer</c> — because the foundation tests for both and seeding only one
        /// would leave half the rule dead.
        /// </summary>
        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.LinkReviewer)]
        public async Task ShouldAllowReviewerToModifyAnotherUsersLinkAsync(string reviewRoleName)
        {
            // given
            Link randomLink = await PostRandomLinkAsync();
            Link modifiedLink = UpdateLinkWithRandomValues(randomLink);
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), reviewRoleName);

            try
            {
                // when
                Link actualLink = await this.apiBroker.PutLinkAsync(modifiedLink);

                // then
                actualLink.Name.Should().Be(modifiedLink.Name);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreLinkByIdAsync(randomLink.Id);
            }
        }

        /// <summary>
        /// Removal is owner-or-Admin, deliberately narrower than modify: a Reviewer holds write
        /// permission on someone else's link but may not delete it.
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsNeitherOwnerNorAdministratorAsync()
        {
            // given
            Link randomLink = await PostRandomLinkAsync();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.LinkReviewer);

            try
            {
                // when
                var deleteLinkTask = this.apiBroker.DeleteLinkByIdAsync(randomLink.Id).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => deleteLinkTask);
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();
                await this.apiBroker.RemoveCoreLinkByIdAsync(randomLink.Id);
            }
        }

        /// <summary>
        /// The block tier (design §18.6): "assigned to users who misbehave, takes precedence over
        /// every other role". Both the global and the link-scoped block are refused, and the
        /// refusal survives the caller also holding Admin — precedence is the whole point.
        /// </summary>
        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.LinkReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfCallerIsBlockedAsync(string blockRoleName)
        {
            // given
            Link randomLink = CreateRandomLink();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), blockRoleName);

            // when
            var postLinkTask = this.apiBroker.PostLinkAsync(randomLink).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postLinkTask);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.LinkReadOnly)]
        public async Task ShouldReturnUnauthorizedOnPostIfBlockedCallerAlsoHoldsAdminAsync(
            string blockRoleName)
        {
            // given
            Link randomLink = CreateRandomLink();
            this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Admin, blockRoleName);

            // when
            var postLinkTask = this.apiBroker.PostLinkAsync(randomLink).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postLinkTask);
        }


        [Fact]
        public async Task ShouldReturnNotFoundOnGetByIdIfLinkDoesNotExistAsync()
        {
            // given
            Guid randomId = Guid.NewGuid();

            // when
            var getLinkTask = this.apiBroker.GetLinkByIdAsync(randomId).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => getLinkTask);
        }



        // NO CONFLICT AND NO HARD-DELETE TESTS HERE, and both absences are decisions.
        //
        // Link has no natural key. §3.4.2's duplicate rule is keyed on (ContentType,
        // ContentHash) and answers a duplicate ADD with a polite acknowledgement rather than
        // creating — so there is no 409 for this entity to provoke through an add at all.
        //
        // Hard delete, submit and approve are not on this exposer: the processing service has no
        // public member for the first two, and the approve command arrives as an event so the
        // publication swap can order two writes on one call stack. The controller's XML doc
        // records which of those three is a gap (#316) and which is the design.
    }
}
