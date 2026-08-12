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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        // A stored row a submit may act on: Draft and not deleted.
        private static BibleReference CreateSubmittableStorageBibleReference()
        {
            BibleReference bibleReference = CreateRandomBibleReference();
            bibleReference.IsDeleted = false;
            bibleReference.ApprovalStatus = ApprovalStatus.Draft;
            bibleReference.IsPublished = false;
            bibleReference.PublishDate = null;

            return bibleReference;
        }

        // A stored row an approve may act on: Submitted and not deleted.
        private static BibleReference CreateApprovableStorageBibleReference()
        {
            BibleReference bibleReference = CreateSubmittableStorageBibleReference();
            bibleReference.ApprovalStatus = ApprovalStatus.Submitted;

            return bibleReference;
        }

        // The caller's copy on approve carries only the outcome and its publication fields; the
        // do-work reads nothing else off it.
        private static BibleReference CreateApprovalDecision(Guid bibleReferenceId) =>
            new BibleReference
            {
                Id = bibleReferenceId,
                ApprovalStatus = ApprovalStatus.Approved,
                IsPublished = true,
                PublishDate = GetRandomDateTimeOffset(),
            };

        private static BibleReference CreateRejectionDecision(Guid bibleReferenceId) =>
            new BibleReference
            {
                Id = bibleReferenceId,
                ApprovalStatus = ApprovalStatus.Rejected,
                IsPublished = false,
                PublishDate = null,
            };

        private void SetupBibleReferenceStorageRead(BibleReference storageBibleReference) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    storageBibleReference.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageBibleReference);

        private void SetupAccessBrokerToPermit() =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(PermittedVerdict());

        private void SetupAccessBrokerToPermitByBypass() =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(PermittedBypassVerdict());

        // The refusing verdict's Explanation is a distinct token ("refused") so the leak guard
        // can assert it never reaches the caller and the log test can assert it does reach the
        // warning.
        private void SetupAccessBrokerToRefuse(AccessDenialReason denialReason) =>
            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AccessVerdict
                        {
                            IsPermitted = false,
                            DenialReason = denialReason,
                            IsBypassUsed = false,
                            BypassedBlockReason = AccessDenialReason.None,
                            Explanation = "refused",
                        });

        // Runs a permitted approve end to end and hands back a snapshot of the row that reached
        // the storage broker. The snapshot is taken INSIDE the callback: the service copies onto
        // the instance the storage read handed it, so reading that instance after the act would
        // compare the row with itself and pass however the operation behaved.
        private async ValueTask<BibleReference> CaptureSavedBibleReferenceOnApproveAsync(
            BibleReference storageBibleReference,
            BibleReference inputBibleReference)
        {
            BibleReference savedBibleReference = null;

            SetupBibleReferenceStorageRead(storageBibleReference);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((BibleReference entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<BibleReference, CancellationToken>(
                            (entity, _) => savedBibleReference = entity.DeepClone())
                        .ReturnsAsync((BibleReference entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    It.IsAny<BibleReferenceEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                            new EventPublishResult<BibleReference>()));

            await this.bibleReferenceService.ApproveBibleReferenceAsync(
                inputBibleReference,
                TestContext.Current.CancellationToken);

            return savedBibleReference;
        }

        // Runs a permitted approve end to end and hands back the query the service gave the
        // access broker. Permitted rather than refused because the whole operation should run:
        // the query is built before the verdict is read, so this is the query a real approve
        // sends.
        private async ValueTask<ApprovalDecisionQuery> CaptureApprovalDecisionQueryAsync(
            BibleReference storageBibleReference,
            BibleReference inputBibleReference)
        {
            ApprovalDecisionQuery actualQuery = null;

            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ApprovalDecisionQuery, CancellationToken>(
                            (approvalDecisionQuery, _) => actualQuery = approvalDecisionQuery)
                        .ReturnsAsync(PermittedVerdict());

            SetupBibleReferenceStorageRead(storageBibleReference);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((BibleReference entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(
                    It.IsAny<BibleReference>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((BibleReference entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    It.IsAny<BibleReferenceEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                            new EventPublishResult<BibleReference>()));

            await this.bibleReferenceService.ApproveBibleReferenceAsync(
                inputBibleReference,
                TestContext.Current.CancellationToken);

            return actualQuery;
        }

        // Everything a caller could read off what was thrown: every message in the chain and
        // every key and value in every Data dictionary. The leak guard asserts against this
        // rather than against the message alone, because Data surfaces outward too.
        private static string FlattenExceptionText(Exception exception)
        {
            var builder = new StringBuilder();

            for (Exception current = exception;
                current is not null;
                current = current.InnerException)
            {
                builder.AppendLine(current.Message);

                foreach (DictionaryEntry entry in current.Data)
                {
                    builder.AppendLine(Convert.ToString(entry.Key));

                    if (entry.Value is IEnumerable<string> values)
                    {
                        builder.AppendLine(string.Join(" ", values));

                        continue;
                    }

                    builder.AppendLine(Convert.ToString(entry.Value));
                }
            }

            return builder.ToString();
        }
    }
}
