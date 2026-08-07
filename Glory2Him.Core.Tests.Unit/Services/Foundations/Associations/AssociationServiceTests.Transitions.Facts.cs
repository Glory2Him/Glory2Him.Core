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
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        /// <summary>
        /// The acceptance criterion for the state-transition work, and the one failure this
        /// whole design exists to prevent.
        ///
        /// <para>The approval workflow subscribes to <c>Association-Modified</c> and causes
        /// <c>Association-Approved</c>. If any transition published <c>Modified</c> it would
        /// re-enter the handler that caused it. <c>ProcessedEvents</c> cannot break that — it
        /// is unique on <c>(EventId, ReceiverName)</c> and stops redeliveries of ONE event,
        /// whereas a write-back publishes on an envelope minted with a FRESH event id. Under
        /// the inline dispatch the repetition is synchronous re-entry inside the originating
        /// request, so the symptom is a stack overflow in a user's HTTP call, not a slow loop
        /// somebody notices in a log.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNeverPublishModifiedFromAnyStateTransitionAsync()
        {
            // given: every transition, driven end to end against one permissive caller, with
            // every published operation recorded
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Admin, Roles.Publisher, Roles.Reviewer);

            var publishedOperations = new List<AssociationEventOperation>();

            Association submittableAssociation = CreateSubmittableStorageAssociation();
            Association approvableAssociation = CreateApprovableStorageAssociation();
            Association sortableAssociation = CreateSubmittableStorageAssociation();
            Association scorableAssociation = CreateSubmittableStorageAssociation();
            Association scopableAssociation = CreateSubmittableStorageAssociation();
            Association anchorAssociation = CreateAnchorAssociation(sortOrder: 200);

            // the sorter and the scorer must not be the caller: sort requires the owner or an
            // Admin (satisfied by Admin), and set-confidence REFUSES the owner outright
            foreach (Association association in new[]
                {
                    submittableAssociation, approvableAssociation, sortableAssociation,
                    scorableAssociation, scopableAssociation, anchorAssociation
                })
            {
                association.CreatedBy = GetRandomString();
            }

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            foreach (Association association in new[]
                {
                    submittableAssociation, approvableAssociation, sortableAssociation,
                    scorableAssociation, scopableAssociation, anchorAssociation
                })
            {
                Association captured = association;

                this.storageBrokerMock.Setup(broker =>
                    broker.SelectAssociationByIdAsync(
                        captured.Id,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(captured);
            }

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association entity, CancellationToken _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Association>().AsQueryable());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<AssociationEventOperation>()))
                        .Returns((EventEnvelope<Association> _, AssociationEventOperation operation) =>
                        {
                            publishedOperations.Add(operation);

                            return new ValueTask<EventPublishResult<Association>>(
                                new EventPublishResult<Association>());
                        });

            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            // when: all four run

            await this.associationService.ApproveAssociationAsync(
                CreateApprovalDecision(approvableAssociation.Id), cancellationToken);

            await this.associationService.SortAssociationAsync(
                new Association { Id = sortableAssociation.Id },
                new Association { Id = anchorAssociation.Id },
                SortPosition.After,
                cancellationToken);

            await this.associationService.SetAssociationConfidenceAsync(
                CreateConfidenceDecision(scorableAssociation.Id), cancellationToken);

            await this.associationService.SetAssociationScopeAsync(
                scopableAssociation.Id,
                scopableAssociation.EntityAScope,
                scopableAssociation.EntityBScope,
                cancellationToken);

            // then
            publishedOperations.Should().HaveCount(4,
                because: "each transition publishes exactly one fact");

            publishedOperations.Should().NotContain(AssociationEventOperation.Modified,
                because: "the approval workflow subscribes to Modified and causes Approved — a "
                    + "transition publishing Modified re-enters the handler that caused it");

            publishedOperations.Should().BeEquivalentTo(new[]
            {
                AssociationEventOperation.Approved,
                AssociationEventOperation.Sorted,
                AssociationEventOperation.ConfidenceSet,
                AssociationEventOperation.Scoped
            });
        }
    }
}
