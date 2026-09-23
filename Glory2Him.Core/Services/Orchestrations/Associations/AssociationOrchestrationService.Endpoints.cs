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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
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

        // The ADD's resolver: it is handed raw key ids and reads each endpoint to DERIVE the
        // scope, group and content type from it, so there is no stored scope to answer at yet.
        // The read paths' resolver is a different question and lives in the .Reads partial; both
        // go through the one conversion below.
        private ValueTask ResolveEndpointAsync(
            EntityType entityType,
            Guid keyId,
            Action<ResolvedEndpoint> onResolved,
            string endpointName,
            EventEnvelope<Association>? readEnvelope,
            CancellationToken cancellationToken) =>
            ConvertEndpointValidationFailureToNotFoundAsync(
                resolveEndpointAsync: async () =>
                    onResolved(await ResolveEndpointCoreAsync(
                        entityType, keyId, readEnvelope, cancellationToken)),
                endpointName: endpointName);

        // The conversion, written ONCE and shared by both resolvers. The endpoint's own service
        // reports a missing or non-visible row as a validation failure; to the association it
        // means the endpoint could not be resolved. The real reason has already been logged
        // inside that service (§SEC14.5 rules 5-7).
        //
        // Reaching the closing catch (Xeption) arm instead would answer "this endpoint is not
        // visible" with a 424 — a visibility rule reported as a failed dependency.
        private static async ValueTask ConvertEndpointValidationFailureToNotFoundAsync(
            Func<ValueTask> resolveEndpointAsync,
            string endpointName)
        {
            try
            {
                await resolveEndpointAsync();
            }
            catch (Xeption endpointException) when (IsEndpointNotFound(endpointException))
            {
                throw new NotFoundAssociationOrchestrationException(
                    message: $"The {endpointName} endpoint was not found.");
            }
        }

        // WHOSE read each branch makes is the one thing the entry path decides. With no read
        // envelope the endpoint's service mints its own, capturing the AMBIENT caller — right on
        // an HTTP request, where the ambient caller is the caller. The Association-Adding event
        // path hands the inbound envelope instead, so every branch reads as the SIGNED caller: a
        // delivery runs synchronously inside a publish and HttpContextAccessor flows on an
        // AsyncLocal, so a minted envelope there would inherit whoever PUBLISHED (§ARC12.5.2,
        // "a read whose answer depends on who is asking is passed the envelope it is being made
        // under", #631).
        private async ValueTask<ResolvedEndpoint> ResolveEndpointCoreAsync(
            EntityType entityType,
            Guid keyId,
            EventEnvelope<Association>? readEnvelope,
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
                    ContentItem contentItem = readEnvelope is null
                        ? await this.contentItemService.RetrieveContentItemByIdAsync(
                            keyId, cancellationToken)
                        : await this.contentItemService.RetrieveContentItemByIdAsync(
                            keyId, readEnvelope, cancellationToken);

                    return DeriveEndpoint(entityType, keyId, contentItem, contentItem.ContentType);

                case EntityType.Link:
                    Link link = readEnvelope is null
                        ? await this.linkService.RetrieveLinkByIdAsync(keyId, cancellationToken)
                        : await this.linkService.RetrieveLinkByIdAsync(
                            keyId, readEnvelope, cancellationToken);

                    return DeriveEndpoint(entityType, keyId, link, contentType: null);

                case EntityType.Tag:
                    _ = readEnvelope is null
                        ? await this.tagService.RetrieveTagByIdAsync(keyId, cancellationToken)
                        : await this.tagService.RetrieveTagByIdAsync(
                            keyId, readEnvelope, cancellationToken);

                    return DeriveEndpoint(entityType, keyId, versionedEndpoint: null, contentType: null);

                case EntityType.Reaction:
                    _ = readEnvelope is null
                        ? await this.reactionService.RetrieveReactionByIdAsync(keyId, cancellationToken)
                        : await this.reactionService.RetrieveReactionByIdAsync(
                            keyId, readEnvelope, cancellationToken);

                    return DeriveEndpoint(entityType, keyId, versionedEndpoint: null, contentType: null);

                case EntityType.BibleReference:
                    _ = readEnvelope is null
                        ? await this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                            keyId, cancellationToken)
                        : await this.bibleReferenceService.RetrieveBibleReferenceByIdAsync(
                            keyId, readEnvelope, cancellationToken);

                    return DeriveEndpoint(entityType, keyId, versionedEndpoint: null, contentType: null);

                case EntityType.Comment:
                    _ = readEnvelope is null
                        ? await this.commentService.RetrieveCommentByIdAsync(keyId, cancellationToken)
                        : await this.commentService.RetrieveCommentByIdAsync(
                            keyId, readEnvelope, cancellationToken);

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
                groupId: isVersioned ? versionedEndpoint!.GroupId : keyId,
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
