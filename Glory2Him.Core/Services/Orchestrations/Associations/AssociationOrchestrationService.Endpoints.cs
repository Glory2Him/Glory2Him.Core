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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        // What an endpoint resolves to: the group id the effective id keys on, the content type
        // (only a ContentItem has one — it is the authorization input of §5), and the scope its
        // publication model implies. A versioned entity (ContentItem) defaults to AllVersions and
        // keys on its group; a non-versioned one keys on its own id under ThisVersionOnly.
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
            switch (entityType)
            {
                case EntityType.ContentItem:
                    // the one versioned, content-typed endpoint: its group and content type are
                    // derived from the resolved row, and it defaults to AllVersions (§7.5.1)
                    ContentItem contentItem =
                        await this.contentItemService.RetrieveContentItemByIdAsync(
                            keyId, cancellationToken);

                    return new ResolvedEndpoint(
                        groupId: contentItem.ContentItemGroupId,
                        contentType: contentItem.ContentType,
                        scope: Scope.AllVersions);

                case EntityType.Tag:
                    await this.tagService.RetrieveTagByIdAsync(keyId, cancellationToken);
                    return NonVersionedEndpoint(keyId);

                case EntityType.Reaction:
                    await this.reactionService.RetrieveReactionByIdAsync(keyId, cancellationToken);
                    return NonVersionedEndpoint(keyId);

                case EntityType.BibleReference:
                    await this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                        keyId, cancellationToken);
                    return NonVersionedEndpoint(keyId);

                case EntityType.Comment:
                    await this.commentService.RetrieveCommentByIdAsync(keyId, cancellationToken);
                    return NonVersionedEndpoint(keyId);

                case EntityType.Link:
                    await this.linkService.RetrieveLinkByIdAsync(keyId, cancellationToken);
                    return NonVersionedEndpoint(keyId);

                default:
                    // Attachment has no foundation service yet, and an association endpoint
                    // pointing at another association is not a supported shape.
                    throw new InvalidAssociationOrchestrationException(
                        message: $"Entity type {entityType} is not supported as an association endpoint.");
            }
        }

        // A non-versioned entity has exactly one row, so its group id is its own key id, it
        // carries no content type, and ThisVersionOnly and AllVersions mean the same thing —
        // ThisVersionOnly is the honest default.
        private static ResolvedEndpoint NonVersionedEndpoint(Guid keyId) =>
            new ResolvedEndpoint(
                groupId: keyId,
                contentType: null,
                scope: Scope.ThisVersionOnly);

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
