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

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        public async ValueTask<PersonalAssociationUpsert> UpsertPersonalAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default)
        {
            EventEnvelope<Association> envelope =
                await this.eventEnvelopeBroker.CreateAsync(content: association);

            return await DoUpsertPersonalAssociationAsync(
                association: association,
                inboundEnvelope: envelope,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<PersonalAssociationUpsert> DoUpsertPersonalAssociationAsync(
            Association association,
            EventEnvelope<Association> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            string callerUserId =
                await this.securityAuditBroker.GetUserIdAsync(inboundEnvelope.SecurityContext);

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

            readersRow.IsDeleted = false;
            readersRow.DeletedBy = null;
            readersRow.DeletedWhen = null;

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
                operation: AssociationEventOperation.Restored);

            return new PersonalAssociationUpsert
            {
                Outcome = PersonalAssociationUpsertOutcome.Restored,
                Association = updatedRow
            };
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
