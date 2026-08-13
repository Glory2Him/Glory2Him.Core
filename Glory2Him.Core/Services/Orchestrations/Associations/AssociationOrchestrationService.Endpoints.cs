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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        // What an endpoint resolves to: the group id the effective id keys on, the content type
        // (only a ContentItem has one — it is the authorization input of §5), and the scope its
        // publication model implies. A versioned entity (ContentItem, Link, ...) defaults to
        // AllVersions and keys on its group; a non-versioned one keys on its own id under
        // ThisVersionOnly.
        private readonly struct ResolvedEndpoint
        {
            public ResolvedEndpoint(Guid groupId, ContentType? contentType, Scope scope)
            {
                GroupId = groupId;
                ContentType = contentType;
                Scope = scope;
            }

            public Guid GroupId { get; }
            public ContentType? ContentType { get; }
            public Scope Scope { get; }
        }

        private async ValueTask ResolveEndpointAsync(
            EntityType entityType,
            Guid keyId,
            Action<ResolvedEndpoint> onResolved,
            string endpointName,
            CancellationToken cancellationToken)
        {
            try
            {
                ResolvedEndpoint resolved = await ResolveEndpointCoreAsync(
                    entityType, keyId, cancellationToken);

                onResolved(resolved);
            }
            catch (Xeption endpointException) when (IsEndpointNotFound(endpointException))
            {
                // The endpoint's own service reports a missing or non-visible row as a validation
                // failure; to the association it means the endpoint could not be resolved. The
                // real reason has already been logged inside that service.
                throw new NotFoundAssociationOrchestrationException(
                    message: $"The {endpointName} endpoint was not found.");
            }
        }

        private async ValueTask<ResolvedEndpoint> ResolveEndpointCoreAsync(
            EntityType entityType,
            Guid keyId,
            CancellationToken cancellationToken)
        {
            // Every branch reads its endpoint (which confirms it exists and is visible, and
            // surfaces a not-found), then derives from the resolved row. A versioned entity hands
            // its IVersion group to DeriveEndpoint; a non-versioned one passes null and keys on its
            // own id. Only a ContentItem carries a content type. The versioned/scope decision is
            // NEVER made here — DeriveEndpoint reads it from EntityTypeVersioning, the same source
            // of truth the foundation derives from, so the two cannot drift (design §7.5.1 warns
            // against probing the entity for IVersion to answer this).
            switch (entityType)
            {
                case EntityType.ContentItem:
                    ContentItem contentItem =
                        await this.contentItemService.RetrieveContentItemByIdAsync(
                            keyId, cancellationToken);

                    return DeriveEndpoint(entityType, keyId, contentItem, contentItem.ContentType);

                case EntityType.Link:
                    Link link =
                        await this.linkService.RetrieveLinkByIdAsync(keyId, cancellationToken);

                    return DeriveEndpoint(entityType, keyId, link, contentType: null);

                case EntityType.Tag:
                    await this.tagService.RetrieveTagByIdAsync(keyId, cancellationToken);
                    return DeriveEndpoint(entityType, keyId, versionedEndpoint: null, contentType: null);

                case EntityType.Reaction:
                    await this.reactionService.RetrieveReactionByIdAsync(keyId, cancellationToken);
                    return DeriveEndpoint(entityType, keyId, versionedEndpoint: null, contentType: null);

                case EntityType.BibleReference:
                    await this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                        keyId, cancellationToken);
                    return DeriveEndpoint(entityType, keyId, versionedEndpoint: null, contentType: null);

                case EntityType.Comment:
                    await this.commentService.RetrieveCommentByIdAsync(keyId, cancellationToken);
                    return DeriveEndpoint(entityType, keyId, versionedEndpoint: null, contentType: null);

                default:
                    // Attachment has no foundation service yet, and an association endpoint
                    // pointing at another association is not a supported shape.
                    throw new InvalidAssociationOrchestrationException(
                        message: $"Entity type {entityType} is not supported as an association endpoint.");
            }
        }

        // The publication model is decided by EntityTypeVersioning (design §7.5.1) — the SAME
        // source of truth the foundation's ApplyDerivedEndpointFields uses — so a versioned type
        // (ContentItem, Link, ...) keys on its group under AllVersions and a non-versioned one keys
        // on its own id under ThisVersionOnly, and this can never disagree with the foundation on
        // which is which. A versioned endpoint must supply its resolved IVersion row so its group
        // id can be read from it.
        private static ResolvedEndpoint DeriveEndpoint(
            EntityType entityType,
            Guid keyId,
            IVersion? versionedEndpoint,
            ContentType? contentType)
        {
            bool isVersioned = EntityTypeVersioning.IsVersioned(entityType);

            return new ResolvedEndpoint(
                groupId: isVersioned ? versionedEndpoint!.ContentItemGroupId : keyId,
                contentType: contentType,
                scope: EntityTypeVersioning.DefaultScopeFor(entityType));
        }

        // A missing/non-visible endpoint arrives as the entity's own *ValidationException (which
        // wraps its NotFound), never a dependency or service exception. Distinguished by the
        // suffix so a genuine dependency failure still propagates as a dependency error rather
        // than being mistaken for a not-found endpoint.
        private static bool IsEndpointNotFound(Xeption exception)
        {
            string exceptionName = exception.GetType().Name;

            return exceptionName.EndsWith("ValidationException", StringComparison.Ordinal)
                && exceptionName.EndsWith("DependencyValidationException", StringComparison.Ordinal) is false;
        }
    }
}
