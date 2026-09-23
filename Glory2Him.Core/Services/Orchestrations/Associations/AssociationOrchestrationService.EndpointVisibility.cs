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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        /// <summary>
        /// The resolved endpoint visibility the composite takes as an <b>input</b>. It does not
        /// decide how the sets were obtained, and that is the whole of why one evaluator serves
        /// two readers: §ARC16.8's reaction summary read resolves its hosts caller-INDEPENDENTLY,
        /// because it returns an aggregate whose counts must not move when a reader signs in,
        /// while the association reads resolve theirs through each endpoint's own caller-filtered
        /// collection read. One rule, two resolvers (§SEC14.3).
        /// </summary>
        private readonly struct ResolvedEndpointVisibility
        {
            public ResolvedEndpointVisibility(
                IQueryable<ContentItem> contentItems,
                IQueryable<Link> links,
                IQueryable<Tag> tags,
                IQueryable<Reaction> reactions,
                IQueryable<Comment> comments,
                IQueryable<BibleReference> bibleReferences)
            {
                ContentItems = contentItems;
                Links = links;
                Tags = tags;
                Reactions = reactions;
                Comments = comments;
                BibleReferences = bibleReferences;
            }

            public IQueryable<ContentItem> ContentItems { get; }

            public IQueryable<Link> Links { get; }

            public IQueryable<Tag> Tags { get; }

            public IQueryable<Reaction> Reactions { get; }

            public IQueryable<Comment> Comments { get; }

            public IQueryable<BibleReference> BibleReferences { get; }
        }

        // #310's resolver: each endpoint entity's OWN collection read, already visibility-filtered
        // for this caller by that entity's foundation — which is where §SEC14.3's endpoint term
        // gets its caller clause. No round trip is issued here; every one of these hands back a
        // live queryable the composite below composes into.
        private async ValueTask<ResolvedEndpointVisibility> ResolveCallerVisibleEndpointsAsync(
            CancellationToken cancellationToken) =>
            new ResolvedEndpointVisibility(
                contentItems: await this.contentItemService.RetrieveAllContentItemsAsync(
                    cancellationToken),
                links: await this.linkService.RetrieveAllLinksAsync(cancellationToken),
                tags: await this.tagService.RetrieveAllTagsAsync(cancellationToken),
                reactions: await this.reactionService.RetrieveAllReactionsAsync(cancellationToken),
                comments: await this.commentService.RetrieveAllCommentsAsync(cancellationToken),
                bibleReferences: await this.bibleReferenceService.RetrieveAllBibleReferencesAsync(
                    cancellationToken));

        /// <summary>
        /// §SEC14.3's composite, written <b>once</b> — rules 3 and 4, composed above the
        /// foundation's self-only filter over rules 1, 2 and 5. This is the single private
        /// evaluator §ARC16.8 records: both this service's association reads and #616's reaction
        /// summary read call it, and neither writes its own.
        ///
        /// <para><b>Rules 1, 2 and 5 are NOT re-tested here.</b> The shaping function
        /// <c>AssociationService</c> authors has already applied them, and re-testing them in
        /// memory after the read returns is expressly a finding.</para>
        ///
        /// <para><b>The composite carries no caller term of its own.</b> It applies to every
        /// caller unconditionally; the caller clause lives in the endpoint read that answered
        /// <paramref name="visibleEndpoints"/>. That is why a moderator keeps the pairing on the
        /// <c>Submitted</c> item they moderate — that item's own read admits them — and why a
        /// soft-deleted endpoint drops the pairing for everyone, <c>Administrators</c> included
        /// (§SEC14.5 rule 3).</para>
        ///
        /// <para><b>An endpoint type with no foundation read resolves to nothing and the row
        /// drops.</b> <c>Attachment</c> has no service yet and an association pointing at another
        /// association is not a supported shape, so neither can be shown to be visible, and
        /// §SEC14.5 rule 4 makes that a silent drop rather than an error.</para>
        /// </summary>
        private static IQueryable<Association> ApplyEndpointVisibilityComposite(
            IQueryable<Association> associations,
            ResolvedEndpointVisibility visibleEndpoints)
        {
            // Copied into locals so the expression tree closes over one queryable per endpoint
            // entity rather than over the struct that holds them.
            IQueryable<ContentItem> visibleContentItems = visibleEndpoints.ContentItems;
            IQueryable<Link> visibleLinks = visibleEndpoints.Links;
            IQueryable<Tag> visibleTags = visibleEndpoints.Tags;
            IQueryable<Reaction> visibleReactions = visibleEndpoints.Reactions;
            IQueryable<Comment> visibleComments = visibleEndpoints.Comments;
            IQueryable<BibleReference> visibleBibleReferences = visibleEndpoints.BibleReferences;

            // §SEC14.3 RULE 6 — the §DOM6.10 effective-setting term — BELONGS IN THIS METHOD AND
            // IS DELIBERATELY ABSENT. It is out of scope by ruling rather than by omission
            // (§ARC16.8, the "§SEC14.3 rules 3, 4 and 6" row), and it is not merely a later build
            // date: rules 3 and 4 are answered by each endpoint's own collection read and compose
            // INTO the query, while rule 6 is a §DOM6.4 SELECTION — the narrowest scope wins
            // outright — which no query composition can express and which an IAccessBroker arm
            // that does not exist yet must gather. Landing it adds a term here. It must not add a
            // second evaluator.
            //
            // Composed in System.Linq only, and never with Microsoft.EntityFrameworkCore: a
            // service importing EF to shape a query is a finding (§ARC12.2.1 rule 3). Nothing is
            // resolved into a closed-over id set either — a set-keyed resolution would need the
            // endpoint ids, which needs the association rows, which is the enumeration the
            // unenumerated-queryable requirement forbids.
            return associations.Where(association =>

                // ── endpoint A ────────────────────────────────────────────────────────────────
                ((association.EntityAType == EntityType.ContentItem
                        && visibleContentItems.Any(contentItem =>
                            (association.EntityAScope == Scope.AllVersions
                                && contentItem.GroupId == association.EntityAGroupId)
                            || (association.EntityAScope != Scope.AllVersions
                                && contentItem.Id == association.EntityAKeyId)))
                    || (association.EntityAType == EntityType.Link
                        && visibleLinks.Any(link =>
                            (association.EntityAScope == Scope.AllVersions
                                && link.GroupId == association.EntityAGroupId)
                            || (association.EntityAScope != Scope.AllVersions
                                && link.Id == association.EntityAKeyId)))
                    || (association.EntityAType == EntityType.Tag
                        && visibleTags.Any(tag => tag.Id == association.EntityAKeyId))
                    || (association.EntityAType == EntityType.Reaction
                        && visibleReactions.Any(reaction => reaction.Id == association.EntityAKeyId))
                    || (association.EntityAType == EntityType.Comment
                        && visibleComments.Any(comment => comment.Id == association.EntityAKeyId))
                    || (association.EntityAType == EntityType.BibleReference
                        && visibleBibleReferences.Any(bibleReference =>
                            bibleReference.Id == association.EntityAKeyId)))

                // ── AND endpoint B. The AND is the rule: an association is only as visible as
                // BOTH of its endpoints, and canonical ordering decides which entity lands on
                // which side, so neither side is the other's mirror. ────────────────────────────
                && ((association.EntityBType == EntityType.ContentItem
                        && visibleContentItems.Any(contentItem =>
                            (association.EntityBScope == Scope.AllVersions
                                && contentItem.GroupId == association.EntityBGroupId)
                            || (association.EntityBScope != Scope.AllVersions
                                && contentItem.Id == association.EntityBKeyId)))
                    || (association.EntityBType == EntityType.Link
                        && visibleLinks.Any(link =>
                            (association.EntityBScope == Scope.AllVersions
                                && link.GroupId == association.EntityBGroupId)
                            || (association.EntityBScope != Scope.AllVersions
                                && link.Id == association.EntityBKeyId)))
                    || (association.EntityBType == EntityType.Tag
                        && visibleTags.Any(tag => tag.Id == association.EntityBKeyId))
                    || (association.EntityBType == EntityType.Reaction
                        && visibleReactions.Any(reaction => reaction.Id == association.EntityBKeyId))
                    || (association.EntityBType == EntityType.Comment
                        && visibleComments.Any(comment => comment.Id == association.EntityBKeyId))
                    || (association.EntityBType == EntityType.BibleReference
                        && visibleBibleReferences.Any(bibleReference =>
                            bibleReference.Id == association.EntityBKeyId))));
        }
    }
}
