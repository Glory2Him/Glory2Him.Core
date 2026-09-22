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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Processings.ContentItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.ContentItems
{
    /// <summary>
    /// The contentItem exposure point (design §12.6). Thin by construction: it authenticates through
    /// middleware, hands the request to <see cref="IContentItemProcessingService"/>, and maps the service's typed
    /// exceptions onto HTTP status codes. It carries no business logic and builds no
    /// <c>SecurityContext</c>, <c>RequestContext</c> or <c>EventEnvelope&lt;T&gt;</c> — those are
    /// created only inside the service (design §10.12).
    ///
    /// <para><b>It binds to the PROCESSING service, and that is mandatory rather than a
    /// preference.</b> <c>ContentItem</c> is Versioned (§7.5.1), and §10.17 rule 1 makes a service
    /// above the foundation a hard prerequisite for a Versioned approvable entity — a fork must
    /// emit exactly one fact per completed amend, which a foundation cannot promise. §10.17 rule 3
    /// states the consequence for exposers directly: a write made against a foundation service
    /// bypasses approval invalidation, so an approvable entity must be exposed through its
    /// top-layer service. <c>IContentItemService</c> has more members and is the wrong
    /// dependency; binding to it would let an HTTP caller amend an item without the approval
    /// workflow ever hearing about it.</para>
    ///
    /// <para><b>All six reads are <c>[AllowAnonymous]</c>, each for its own documented reason</b>
    /// — the service interface states the posture per member and this controller does not restate
    /// it. What matters here is that two of them are NOT interchangeable: <c>Get</c> widens with
    /// the caller (owner sees their own drafts, a review role sees everything) while
    /// <c>GetPublicContentItems</c> consults no security context at all. The first is a moderation
    /// surface, the second is the public one.</para>
    ///
    /// <para><b>Submit and hard removal are absent, and it is a gap rather than a design.</b>
    /// <c>IContentItemProcessingService</c> has neither; both exist only on
    /// <c>IContentItemService</c>. The consequence is worth stating plainly: <b>a content item
    /// cannot be submitted for approval over HTTP.</b> No other route reaches it either —
    /// <c>ModifyContentItemAsync</c> treats <c>ApprovalStatus</c> as a control field (§12.4.1 rule
    /// 6) so the <c>Draft</c> ↔ <c>Submitted</c> carve-out is unavailable on this path, and
    /// <c>ApprovalOrchestrationService.ProcessEntityModifiedAsync</c> explicitly refuses to move
    /// the status because "submitting is somebody's decision to offer the content rather than a
    /// side effect of editing it". A draft created here stays a draft. Lifting
    /// <c>SubmitContentItemByIdAsync</c> onto the processing service is the fix when the approval
    /// round needs to be enterable through the API (#316).</para>
    ///
    /// <para><b>Approve is absent for a different reason, and that one IS a design.</b> §12.4.1
    /// rule 10 addresses the approval command for <c>ContentItem</c> to this service — but as an
    /// <i>event</i>, <c>OnApprovingContentItemAsync</c>, because the publication swap must demote
    /// the incumbent before promoting the new row and the filtered unique index refuses the other
    /// order. Two rows in a guaranteed order is a call stack, not a delivery. The HTTP route in
    /// already exists: <c>POST api/Approvals/{entityType}/{entityId}/Decision</c> reaches
    /// <c>ApprovalOrchestrationService.DecideApprovalAsync</c>, which publishes the command. An
    /// <c>Approve</c> endpoint here would be a second, unordered path to the same write.</para>
    ///
    /// <para><b>Routes follow <c>[Route("api/[controller]")]</c>, not §17.1's kebab-case.</b> The
    /// exposer skill's <c>contracts.json</c> requires that token and every built controller uses
    /// it; §17.1 predates the skill. <c>PUT</c> takes the model in the body with no <c>{id}</c>
    /// segment, matching its siblings. <c>api/ContentItems/Public</c> resolves to the literal
    /// route rather than <c>{contentItemId}</c> because attribute routing ranks a literal segment
    /// above a parameter — the acceptance suite exercises it rather than trusting it.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ContentItemsController : RESTFulController
    {
        private readonly IContentItemProcessingService contentItemProcessingService;

        public ContentItemsController(IContentItemProcessingService contentItemProcessingService) =>
            this.contentItemProcessingService = contentItemProcessingService;

        [HttpPost]
        [Authorize]
        public async ValueTask<ActionResult<ContentItem>> PostContentItemAsync(
            [FromBody] ContentItem contentItem,
            CancellationToken cancellationToken)
        {
            try
            {
                // §3.4.2 rule 6: a duplicate contribution is ACKNOWLEDGED here, not refused. The
                // service creates no row and answers with one composed exactly as the persisted
                // one would have been, so this returns the same 201 and the same body it returns
                // for a genuine add — there is deliberately no branch to find, because a branch
                // is what a probe would look for. Only the modify arm refuses a duplicate, which
                // is where PutContentItemAsync's already-exists mapping comes from.
                ContentItem addedContentItem =
                    await this.contentItemProcessingService.AddContentItemAsync(contentItem, cancellationToken);

                return Created(addedContentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is UnauthorizedContentItemProcessingException)
            {
                return Unauthorized(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        [HttpGet]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<ContentItem>>> Get(CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<ContentItem> retrievedContentItems =
                    await this.contentItemProcessingService.RetrieveAllContentItemsAsync(cancellationToken);

                return Ok(retrievedContentItems);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        [HttpGet("{contentItemId}")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<ContentItem>> GetContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem contentItem = await this.contentItemProcessingService.RetrieveContentItemByIdAsync(contentItemId, cancellationToken);

                return Ok(contentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        [HttpPut]
        [Authorize]
        public async ValueTask<ActionResult<ContentItem>> PutContentItemAsync(
            [FromBody] ContentItem contentItem,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem modifiedContentItem =
                    await this.contentItemProcessingService.ModifyContentItemAsync(contentItem, cancellationToken);

                return Ok(modifiedContentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is UnauthorizedContentItemProcessingException)
            {
                return Unauthorized(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException is AlreadyExistsContentItemProcessingException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// Soft removal (design §14.6): the row is marked deleted and keeps its audit trail.
        /// The optional reason is carried through to <c>DeletionReason</c>.
        /// </summary>
        [HttpDelete("{contentItemId}")]
        [Authorize]
        public async ValueTask<ActionResult<ContentItem>> DeleteContentItemByIdAsync(
            Guid contentItemId,
            [FromQuery] string? deletionReason,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem deletedContentItem =
                    await this.contentItemProcessingService.RemoveContentItemByIdAsync(contentItemId, deletionReason, cancellationToken);

                return Ok(deletedContentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException is UnauthorizedContentItemProcessingException)
            {
                return Unauthorized(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException is AlreadyExistsContentItemProcessingException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }

            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// Permanent removal. Design §14.6 restricts hard removal to <c>Administrators</c>; the attribute
        /// below is the coarse half of that and the foundation re-decides it against the row.
        /// </summary>

        /// <summary>
        /// Exactly the canonically visible versions (§14.1: not deleted, <c>Approved</c>,
        /// <c>IsPublished</c>, and <c>PublishDate</c> null or past).
        ///
        /// <para><b>Caller-INDEPENDENT, and that is the whole reason it exists beside
        /// <see cref="Get"/>.</b> No security context is consulted, so a privileged caller
        /// receives exactly what an anonymous visitor would. <see cref="Get"/> widens with the
        /// caller — an owner also sees their own drafts, a review role sees everything — which is
        /// correct for a moderation surface and wrong for a public one. Wiring this route to that
        /// member would leak drafts to anonymous visitors and no attribute test would catch it,
        /// which is why the unit suite asserts the two call different members.</para>
        /// </summary>
        [HttpGet("Public")]
        [EnableQuery]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IQueryable<ContentItem>>> GetPublicContentItems(
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<ContentItem> retrievedContentItems =
                    await this.contentItemProcessingService
                        .RetrieveAllPublicContentItemsAsync(cancellationToken);

                return Ok(retrievedContentItems);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// THE FEED (§DOM11.3) — the front page's default listing, and a different read from
        /// <see cref="GetPublicContentItems"/> rather than a view over it. It excludes
        /// <c>Topic</c> and <c>Series</c> (§DOM3.8 rule 2), which the public read must not do:
        /// applying that exclusion there would make every topic unsearchable.
        ///
        /// <para><b>No <see cref="EnableQueryAttribute"/>, and its absence is the contract.</b>
        /// The page is an ARGUMENT of the read, composed into the same SQL as the predicate and
        /// the order, so the two are mutually exclusive on one route: the attribute would apply
        /// <c>$skip</c> a second time to an already-paged list. With the OData surface go both
        /// of the traps the sibling <c>Groups/{groupId}</c> route has to manage — the ordinal
        /// in-memory <c>$filter</c>, and <c>EnsureStableOrdering</c> discarding the order the
        /// read applied. A caller-supplied query option here is off-surface and ignored, as it
        /// is on every other non-OData route in the solution.</para>
        ///
        /// <para><b>The page travels as two OPTIONAL parameters, and neither carries
        /// <c>[BindRequired]</c>.</b> <c>Program.cs</c> reserves that attribute for a parameter
        /// that must be present to ADDRESS the operation — the cases where the framework's zero
        /// value is harmful, an absent <c>decision</c> binding to <c>Approve</c> and an absent
        /// <c>isResolved</c> un-resolving a comment. This is the counter-case the same comment
        /// names on <c>isBypassRequested</c>: absent means the first page of a read, nothing is
        /// mutated, and the safe reading is the obvious one.</para>
        ///
        /// <para><b>They are nullable for a reason that is not stylistic.</b> A non-nullable
        /// <c>int take</c> binds an absent parameter to <c>0</c>, and a <c>take</c> of zero is
        /// a validation failure — so plain <c>int</c> would collapse "asked for nothing" into
        /// "said nothing" and turn the bare URL into a 400. The nulls pass through UNTOUCHED:
        /// defaulting is a decision and this layer holds none, so the processing service is
        /// what resolves them to the first page at the cap.</para>
        /// </summary>
        [HttpGet("Feed")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IReadOnlyList<ContentItem>>> GetContentItemFeed(
            [FromQuery] int? skip,
            [FromQuery] int? take,
            CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<ContentItem> feedContentItems =
                    await this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                        skip: skip,
                        take: take,
                        cancellationToken: cancellationToken);

                return Ok(feedContentItems);
            }
            // A BAD PAGE IS THE CALLER'S, so it answers 400. skip and take are the only things
            // a caller supplies to this read - a take of zero, a take above the cap, a negative
            // skip - and without these two arms the service's validation exception escapes the
            // action and ASP.NET files a server fault for bad input.
            //
            // A skip past the end is deliberately NOT among them: that is a valid page which
            // happens to be empty, the same answer an unknown group id gets on the sibling
            // route. There is no not-found arm here for the same reason.
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// Every version of one group (§17.1 <c>/groups/{groupId}</c>), under the same per-caller
        /// filter as <see cref="Get"/>.
        /// </summary>
        ///
        /// <remarks>
        /// <para><b>Why this route allows less than <see cref="Get"/> does.</b> The service hands
        /// back a materialised set, so <see cref="EnableQueryAttribute"/> composes over
        /// LINQ-to-Objects rather than pushing into SQL, and two options change meaning in that
        /// move:</para>
        ///
        /// <para><c>$filter</c> compares ORDINALLY in memory - OData binds <c>eq</c> to
        /// <c>Expression.Equal</c> and <c>contains</c> to <c>string.Contains(string)</c> - where
        /// the catalogue's <c>SQL_Latin1_General_CP1_CI_AS</c> collation is case-INSENSITIVE. The
        /// same filter that matches on <see cref="Get"/> would silently match nothing here.</para>
        ///
        /// <para><c>$orderby</c> is NOT ordinal, and an earlier version of this comment said it
        /// was. In memory it is <c>Comparer&lt;string&gt;.Default</c>, i.e. the SERVER'S current
        /// culture through ICU - which is a different divergence, not an absent one. It still
        /// disagrees with the catalogue (measured: <c>Coop</c> and <c>co-op</c> swap, and the CI
        /// collation treats <c>Alpha</c> and <c>alpha</c> as equal where ICU orders lowercase
        /// first), and it additionally makes the answer depend on the HOST'S culture
        /// configuration. Both are reasons to refuse it.</para>
        ///
        /// <para><b>This is an ALLOW-LIST, so it refuses more than those two.</b> Also rejected
        /// with 400: <c>$select</c>, <c>$expand</c>, <c>$search</c>, <c>$compute</c>,
        /// <c>$apply</c>, <c>$format</c>, <c>$skiptoken</c> and <c>$deltatoken</c>. Of these only
        /// <c>$select</c> is safe on the merits - it projects rather than compares, and its
        /// property-name resolution is culture-invariant (verified under <c>tr-TR</c>) - but
        /// allowing it would serve a DIFFERENTLY-CASED payload from this one route, because OData
        /// serialises a projection through its own converter in PascalCase and bypasses the
        /// application's camelCase policy, and it silently returns empty objects for nested
        /// members. It stays out until a client is ready for that; the reason is presentation,
        /// not correctness.</para>
        ///
        /// <para><b>Paging.</b> <c>$top</c> and <c>$skip</c> are positional and carry no
        /// comparison, so they stay - but the order they page is NOT free. OData's
        /// <c>EnsureStableOrdering</c> would impose its own ordering by the entity key, which for
        /// a version lineage means Guid order, and it DISCARDS any ordering applied upstream. So
        /// it is turned off here, and the foundation orders by <c>Version</c> instead; the two
        /// belong together, because either alone leaves this route unordered or meaninglessly
        /// ordered. <c>$count</c> is allowed but inert on a bare JSON array - no
        /// <c>@odata.count</c> is emitted, and page truncation emits no <c>@odata.nextLink</c>.</para>
        /// </remarks>
        [HttpGet("Groups/{groupId}")]
        [EnableQuery(
            EnsureStableOrdering = false,
            AllowedQueryOptions =
                AllowedQueryOptions.Top | AllowedQueryOptions.Skip | AllowedQueryOptions.Count)]
        [AllowAnonymous]
        public async ValueTask<ActionResult<IReadOnlyList<ContentItem>>> GetContentItemsByGroupId(
            Guid groupId,
            CancellationToken cancellationToken)
        {
            try
            {
                // A MATERIALISED set, not a live queryable: the read executes in the service with
                // the caller's token instead of on this thread when the response is serialised.
                //
                // The cost, stated plainly: SQL now sees WHERE GroupId = @g and no TOP, so the
                // whole lineage crosses the wire and is filtered and paged HERE. A group is one
                // content item's version lineage, seeked on the unique (GroupId, Version) index —
                // small in practice, but no invariant bounds it.
                IReadOnlyList<ContentItem> retrievedContentItems =
                    await this.contentItemProcessingService
                        .RetrieveContentItemsByGroupIdAsync(groupId, cancellationToken);

                return Ok(retrievedContentItems);
            }
            // A BAD GROUP ID IS THE CALLER'S, so it answers 400. Without these two arms the
            // validation exception escaped the action and ASP.NET turned it into a 500, filing a
            // server-fault log for a malformed route parameter - while the sibling
            // Groups/{groupId}/Latest route below answered 400 for the same input.
            //
            // No NotFound arm, and that is not an omission: an unknown group is an EMPTY LIST
            // here, not an error, so a collection read has no not-found case to report. Nor can it
            // conflict or lock, which is why the sibling's remaining arms are absent too.
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// The group's edit tip — the highest non-deleted <c>Version</c>, which may still be an
        /// unapproved draft. Answers not-found rather than unauthorized so an unprivileged probe
        /// cannot tell a non-public tip from a missing group (§14.5).
        /// </summary>
        [HttpGet("Groups/{groupId}/Latest")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<ContentItem>> GetLatestContentItemByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem contentItem = await this.contentItemProcessingService
                    .RetrieveLatestContentItemByGroupIdAsync(groupId, cancellationToken);

                return Ok(contentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException
                    is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }

        /// <summary>
        /// The row the public currently reads, which stays published while a newer draft is in
        /// review (§3.4.1). A published row scheduled in the future is visible only to its owner
        /// or a review role; everyone else gets not-found, as does every caller when the group has
        /// no published row.
        /// </summary>
        [HttpGet("Groups/{groupId}/Published")]
        [AllowAnonymous]
        public async ValueTask<ActionResult<ContentItem>> GetPublishedContentItemByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken)
        {
            try
            {
                ContentItem contentItem = await this.contentItemProcessingService
                    .RetrievePublishedContentItemByGroupIdAsync(groupId, cancellationToken);

                return Ok(contentItem);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
                when (contentItemProcessingValidationException.InnerException
                    is NotFoundContentItemProcessingException)
            {
                return NotFound(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingValidationException contentItemProcessingValidationException)
            {
                return BadRequest(contentItemProcessingValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is NotFoundContentItemException)
            {
                return NotFound(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is UnauthorizedContentItemException)
            {
                return Unauthorized(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is AlreadyExistsContentItemException)
            {
                return Conflict(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
                when (contentItemProcessingDependencyValidationException.InnerException
                    is LockedContentItemException)
            {
                return Locked(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyValidationException contentItemProcessingDependencyValidationException)
            {
                return BadRequest(contentItemProcessingDependencyValidationException.InnerException);
            }
            catch (ContentItemProcessingDependencyException contentItemProcessingDependencyException)
            {
                return FailedDependency(contentItemProcessingDependencyException.InnerException);
            }
            catch (ContentItemProcessingServiceException contentItemProcessingServiceException)
            {
                return InternalServerError(contentItemProcessingServiceException);
            }
        }
    }
}
