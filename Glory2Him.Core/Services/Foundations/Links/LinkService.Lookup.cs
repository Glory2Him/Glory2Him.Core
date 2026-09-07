// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Services.Foundations.Links
{
    internal partial class LinkService
    {
        // The tip DERIVATION, asked as the boolean it is. Reading the whole group back to run
        // Any() in memory moved every column of every version across the wire on the modify hot
        // path to answer one bit.
        //
        // UNFILTERED, like the high-water mark above it and for the same reason: a version
        // question is structural. Answered from the caller-facing collection read - which is what
        // it used to be - a contributor who could not SEE a newer sibling was told their row was
        // the tip and edited it in place.
        public ValueTask<bool> CheckHigherLinkVersionExistsAsync(
            Guid groupId,
            int version,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var checkRequest = new Link { GroupId = groupId };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: checkRequest);

                ValidateUserIsAllowedToContribute(envelope.SecurityContext);
                ValidateOnFindHighestVersionInGroup(groupId);

                return await this.storageBroker.ExistsHigherLiveLinkVersionInGroupAsync(
                    groupId: groupId,
                    version: version,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<int> FindHighestVersionInGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            TryCatchVersion(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var findRequest = new Link { GroupId = groupId };

                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: findRequest);

                ValidateUserIsAllowedToContribute(envelope.SecurityContext);
                ValidateOnFindHighestVersionInGroup(groupId);

                // UNFILTERED, and for a different reason than the tip is filtered. Which row may
                // be EDITED is a question about live rows — nobody amends a tombstone. Which
                // version number is FREE is a question about every row that has ever existed,
                // because the unique index on (GroupId, Version) carries no IsDeleted filter: a
                // soft-deleted row still owns its number.
                //
                // Conflating the two was issue #271. The tip check skips tombstones, so a live v1
                // under a soft-deleted v2 looked like the tip; the fork then numbered its
                // successor v2 and collided, failing every fork in that group from then on.
                //
                // A lineage is not renumbered by removing a row from it — the same argument
                // §9.7.7 rule 7 records for the published slot.
                //
                // The projection is asked for, not composed here: narrowing the collection read
                // and calling ToList() on it blocked the request thread on a SQL round trip the
                // cancellation token never reached.
                List<int> groupVersions =
                    await this.storageBroker.SelectLinkVersionsInGroupAsync(
                        groupId: groupId,
                        cancellationToken: cancellationToken);

                return groupVersions.Count is 0
                    ? 0
                    : groupVersions.Max();
            });



        // The swap's single probe, replacing a caller-FILTERED read plus a group lookup. Gated
        // and unfiltered, it resolves the target's group off the stored row and returns the
        // incumbent holding that group's slot. The gate is the same contribution check its
        // sibling runs, and it admits the workflow's system identity because that check blocks
        // roles rather than requiring them (#291).
        public ValueTask<Guid?> FindPublishedSiblingLinkIdAsync(
            Guid linkId,
            EventEnvelope<Link> inboundEnvelope,
            CancellationToken cancellationToken = default) =>
            TryCatchIdentifier(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // The envelope is READ, not chained. Every other envelope-taking entry point
                // on this service calls CreateNextAsync first, because each of them writes and
                // publishes a fact that has to hang off the caller's causation link. This one
                // publishes nothing and records no ProcessedEvents row, so there is no link to
                // extend — minting a successor here would only produce an envelope used for the
                // SecurityContext already in hand.
                ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
                ValidateOnFindPublishedSiblingLink(linkId);

                // The group comes from the STORED row. A caller-supplied GroupId would let one
                // group's approval unpublish another group's live row.
                Link maybeLink = await this.storageBroker.SelectLinkByIdAsync(
                    linkId: linkId,
                    cancellationToken: cancellationToken);

                ValidateStorageLink(maybeLink, linkId);

                if (maybeLink.IsDeleted)
                {
                    throw new NotFoundLinkException(
                        message: $"Link not found with id: {linkId}.");
                }

                // UNFILTERED on the incumbent side too: a soft delete never clears
                // IsPublished and the slot index names that column alone, so a tombstone still
                // holds the slot. Skipping it would leave the group permanently unpublishable.
                //
                // One row asked for, with the token, rather than a predicate composed onto the
                // collection read's live queryable and executed synchronously.
                Link? publishedLink = await this.storageBroker.SelectPublishedLinkInGroupAsync(
                    groupId: maybeLink.GroupId,
                    excludedLinkId: linkId,
                    cancellationToken: cancellationToken);

                return publishedLink?.Id;
            });


        public ValueTask<IReadOnlyList<Link>> RetrieveLinksByGroupIdAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            TryCatchList(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnFindHighestVersionInGroup(groupId);

                // the envelope exists to capture the ambient security context the visibility
                // filter runs against — the request payload is empty, exactly as the unkeyed
                // collection read builds it
                EventEnvelope<Link> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: new Link());

                // THE GROUP'S SLICE, ASKED FOR AS A SLICE, with the token. The narrowing used to
                // be a Where composed onto the collection read's live queryable by whichever
                // caller needed it, which left them a synchronous terminal operator as the only
                // way to execute it.
                List<Link> groupLinks = await this.storageBroker.SelectLinksByGroupIdAsync(
                    groupId: groupId,
                    cancellationToken: cancellationToken);

                // THE SAME FILTER, not a second copy of it. The predicate is now evaluated in
                // memory rather than in SQL, which is the only difference and one no caller can
                // observe; writing an in-memory twin would give one visibility rule two homes to
                // drift between.
                IQueryable<Link> visibleLinks =
                    await ApplyCollectionReadVisibilityFilterAsync(
                        links: groupLinks.AsQueryable(),
                        securityContext: envelope.SecurityContext);

                // ORDERED BY VERSION, which is the lineage's own order and the only one meaningful
                // to a caller reading a group. It is imposed HERE because this is the layer that
                // declares the IReadOnlyList contract and already shapes this exact set in memory -
                // the visibility filter above runs over it - so ordering is the same kind of work
                // in the same place rather than a second home for one rule.
                //
                // It only takes effect because the exposer sets EnsureStableOrdering = false.
                // OData otherwise re-sorts by the entity key and discards this entirely, which is
                // measured, not assumed. The ThenBy is belt and braces: (GroupId, Version) is
                // unique so Version already totally orders a group, but with OData's own tiebreak
                // switched off nothing else would supply one if that ever changed.
                return visibleLinks
                    .OrderBy(link => link.Version)
                    .ThenBy(link => link.Id)
                    .ToList();
            });

        private static void ValidateOnFindPublishedSiblingLink(Guid linkId) =>
            Validate(
                message: "Link is invalid, fix the errors and try again.",
                (Rule: IsInvalid(linkId), Parameter: nameof(Link.Id)));
    }
}
