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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        public ValueTask<PersonalAssociationUpsert> UpsertPersonalAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                ValidateAssociationIsNotNull(association);

                EventEnvelope<Association> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: association);

                return await DoUpsertPersonalAssociationAsync(
                    association: association,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<PersonalAssociationUpsert> DoUpsertPersonalAssociationAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // a reaction is the signed caller's own, so an anonymous caller has none to give
            ValidateUserIsAuthenticated(inboundEnvelope.SecurityContext);
            ValidateOnUpsertPersonalAssociation(association);

            string callerUserId =
                await this.securityAuditBroker.GetUserIdAsync(inboundEnvelope.SecurityContext);

            ValidateUserIsTheReader(association.UserId, callerUserId);

            // canonical order before the row is resolved and before any storage call: the personal
            // key holds the host on A, and CK_Association_CanonicalOrder refuses any other row
            // (§DOM4.4 rule 4)
            association = NormalizeEndpointOrder(association);

            Guid entityAEffectiveId = ResolveEffectiveId(
                association.EntityAScope,
                association.EntityAGroupId,
                association.EntityAKeyId);

            IReadOnlyList<Association> readersRows =
                await this.storageBroker.SelectAssociationsAsync(
                    query: associations =>
                        SelectPersonalAssociations(
                            associations,
                            entityAType: association.EntityAType,
                            entityAEffectiveId: entityAEffectiveId,
                            entityBType: association.EntityBType,
                            userId: callerUserId)
                                .Take(1),
                    cancellationToken: cancellationToken);

            Association? readersRow = readersRows.FirstOrDefault();

            if (readersRow is null)
            {
                Association auditedAssociation =
                    await this.securityAuditBroker.ApplyAddAuditValuesAsync(
                        entity: association,
                        securityContext: inboundEnvelope.SecurityContext);

                Association addedAssociation =
                    await this.storageBroker.InsertAssociationAsync(
                        auditedAssociation,
                        cancellationToken);

                await PublishPersonalUpsertFactAsync(
                    inboundEnvelope: inboundEnvelope,
                    association: addedAssociation,
                    operation: AssociationEventOperation.Added);

                return new PersonalAssociationUpsert
                {
                    Outcome = PersonalAssociationUpsertOutcome.Created,
                    Association = addedAssociation
                };
            }

            // "withdrawn by the reader" is DeletedBy equal to the row's UserId, and nothing else: a
            // takedown is never revived, whatever reaction is given (§DOM4.10 rule 7)
            if (readersRow.IsDeleted && readersRow.DeletedBy != readersRow.UserId)
            {
                return new PersonalAssociationUpsert
                {
                    Outcome = PersonalAssociationUpsertOutcome.TakenDown,
                    Association = readersRow
                };
            }

            bool isSameReaction = readersRow.EntityBKeyId == association.EntityBKeyId;

            if (readersRow.IsDeleted is false && isSameReaction)
            {
                return new PersonalAssociationUpsert
                {
                    Outcome = PersonalAssociationUpsertOutcome.Unchanged,
                    Association = readersRow
                };
            }

            if (readersRow.IsDeleted)
            {
                readersRow.IsDeleted = false;
                readersRow.DeletedBy = null;
                readersRow.DeletedWhen = null;
            }

            if (isSameReaction is false)
            {
                readersRow.EntityBKeyId = association.EntityBKeyId;
                readersRow.EntityBGroupId = association.EntityBGroupId;
            }

            Association auditedRow =
                await this.securityAuditBroker.ApplyModifyAuditValuesAsync(
                    entity: readersRow,
                    securityContext: inboundEnvelope.SecurityContext);

            Association updatedRow =
                await this.storageBroker.UpdateAssociationAsync(
                    auditedRow,
                    cancellationToken);

            await PublishPersonalUpsertFactAsync(
                inboundEnvelope: inboundEnvelope,
                association: updatedRow,
                operation: isSameReaction
                    ? AssociationEventOperation.Restored
                    : AssociationEventOperation.Repointed);

            return new PersonalAssociationUpsert
            {
                Outcome = isSameReaction
                    ? PersonalAssociationUpsertOutcome.Restored
                    : PersonalAssociationUpsertOutcome.Repointed,

                Association = updatedRow
            };
        }

        // A null UserId is an editorial row, which this member never reaches: the repoint
        // exception is personal-only (§ARC16.2.2).
        private static void ValidateOnUpsertPersonalAssociation(Association association) =>
            Validate(
                message: "Content item association is invalid, fix the errors and try again.",
                (Rule: IsInvalid(association.UserId), Parameter: nameof(Association.UserId)));

        // The upsert acts for the signed caller alone, so a request naming any other reader is
        // refused, whatever role the caller holds (§SEC14.7 posture A′ rule 2: acting for that
        // same UserId).
        private static void ValidateUserIsTheReader(string? userId, string callerUserId)
        {
            if (userId != callerUserId)
            {
                throw new UnauthorizedAssociationException(
                    message: "The current user is not allowed to write another user's " +
                        "personal content item association.");
            }
        }

        // the facts are published as the add publishes its own, with the written row as their
        // content (§ARC16.2.2)
        private async ValueTask PublishPersonalUpsertFactAsync(
            EventEnvelope<Association> inboundEnvelope,
            Association association,
            AssociationEventOperation operation)
        {
            EventEnvelope<Association> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: association);

            await this.eventBroker.PublishAssociationAsync(
                envelope: outboundEnvelope,
                operation: operation);
        }
    }
}
