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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        // A stored row a submit may act on: Draft and not deleted.
        private static Tag CreateSubmittableStorageTag()
        {
            Tag tag = CreateRandomTag();
            tag.IsDeleted = false;
            tag.ApprovalStatus = ApprovalStatus.Draft;
            tag.IsPublished = false;
            tag.PublishDate = null;

            return tag;
        }

        // A stored row an approve may act on: Submitted and not deleted.
        private static Tag CreateApprovableStorageTag()
        {
            Tag tag = CreateSubmittableStorageTag();
            tag.ApprovalStatus = ApprovalStatus.Submitted;

            return tag;
        }

        // The caller's copy on approve carries only the outcome and its publication fields; the
        // do-work reads nothing else off it.
        private static Tag CreateApprovalDecision(Guid tagId) =>
            new Tag
            {
                Id = tagId,
                ApprovalStatus = ApprovalStatus.Approved,
                IsPublished = true,
                PublishDate = GetRandomDateTimeOffset(),
            };

        private static Tag CreateRejectionDecision(Guid tagId) =>
            new Tag
            {
                Id = tagId,
                ApprovalStatus = ApprovalStatus.Rejected,
                IsPublished = false,
                PublishDate = null,
            };

        // The Administrators override's target: a terminal row re-opened for a second round. Publication
        // is not asked for — the validation refuses a published non-approved row, and the
        // do-work derives it off regardless.
        private static Tag CreateReopenDecision(Guid tagId) =>
            new Tag
            {
                Id = tagId,
                ApprovalStatus = ApprovalStatus.Submitted,
                IsPublished = false,
                PublishDate = null,
            };

        // A stored row in a terminal state, published as an approved one would be, so a test can
        // assert the override actually unpublishes it rather than finding it already false.
        private static Tag CreateTerminalStorageTag(ApprovalStatus terminalStatus)
        {
            Tag tag = CreateApprovableStorageTag();
            tag.ApprovalStatus = terminalStatus;
            tag.IsPublished = terminalStatus == ApprovalStatus.Approved;

            tag.PublishDate = tag.IsPublished
                ? GetRandomDateTimeOffset()
                : null;

            return tag;
        }

        // The context ApprovalOrchestrationService mints for the workflow's own writes. Roleless
        // on purpose: the flag is the whole of its authority, so a test that passes with roles
        // attached would not be proving the flag did anything.
        private static SecurityContext CreateSystemSecurityContext() =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = [],
                IsSystemIdentity = true,
            };

        // A bypass REQUEST: the caller asks, and the verdict decides what is recorded.
        private static Tag CreateBypassApprovalRequest(Guid tagId, string bypassReason)
        {
            Tag tag = CreateApprovalDecision(tagId);
            tag.IsApprovedByBypass = true;
            tag.ApprovedByBypassReason = bypassReason;

            return tag;
        }

        private void SetupTagStorageRead(Tag storageTag) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    storageTag.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageTag);

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
        private async ValueTask<Tag> CaptureSavedTagOnTransitionAsync(
            Tag storageTag,
            Tag inputTag)
        {
            Tag savedTag = null;

            SetupTagStorageRead(storageTag);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Tag entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Tag, CancellationToken>(
                            (entity, _) => savedTag = entity.DeepClone())
                        .ReturnsAsync((Tag entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    It.IsAny<TagEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(
                            new EventPublishResult<Tag>()));

            await this.tagService.TransitionTagApprovalAsync(
                inputTag,
                TestContext.Current.CancellationToken);

            return savedTag;
        }

        // The same capture, driven over the EVENT path instead of the in-process one, so a test
        // can assert what the workflow's command actually wrote. DeepClone because the service
        // mutates the storage row it was handed — comparing a field against the same object
        // afterwards would compare it with itself and pass whatever the service did.
        private async ValueTask<Tag> CaptureSavedTagOnEventTransitionAsync(
            Tag storageTag,
            EventEnvelope<Tag> requestEnvelope)
        {
            Tag savedTag = null;

            SetupTagStorageRead(storageTag);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Tag entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Tag, CancellationToken>(
                            (entity, _) => savedTag = entity.DeepClone())
                        .ReturnsAsync((Tag entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    It.IsAny<TagEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(
                            new EventPublishResult<Tag>()));

            await this.tagService.OnApprovingTagAsync(
                requestEnvelope,
                TestContext.Current.CancellationToken);

            return savedTag;
        }

        // Runs a permitted approve end to end and hands back the query the service gave the
        // access broker. Permitted rather than refused because the whole operation should run:
        // the query is built before the verdict is read, so this is the query a real approve
        // sends.
        private async ValueTask<ApprovalDecisionQuery> CaptureApprovalDecisionQueryAsync(
            Tag storageTag,
            Tag inputTag)
        {
            ApprovalDecisionQuery actualQuery = null;

            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalAsync(
                    It.IsAny<ApprovalDecisionQuery>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ApprovalDecisionQuery, CancellationToken>(
                            (approvalDecisionQuery, _) => actualQuery = approvalDecisionQuery)
                        .ReturnsAsync(PermittedVerdict());

            SetupTagStorageRead(storageTag);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Tag entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Tag entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    It.IsAny<TagEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(
                            new EventPublishResult<Tag>()));

            await this.tagService.TransitionTagApprovalAsync(
                inputTag,
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
