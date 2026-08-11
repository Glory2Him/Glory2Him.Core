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
using FluentAssertions;
using Force.DeepCloner;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    /// <summary>
    /// Bypass-approve — approving OVER the unmet conditions (HR-4 route 3). It is its own verb
    /// rather than a flag on approve, and the pair of fields it writes is the reason it exists:
    /// the row has to say that the conditions were waived, and what was given as the excuse.
    ///
    /// <para>Both of those fields are DERIVED from the verdict and never read off the caller's
    /// entity. A caller able to write them is equally able to clear them, un-recording the one
    /// event they are here to capture — so every test below puts something else on the input
    /// and asserts that the derived value is what reached storage.</para>
    /// </summary>
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldBypassApproveAssociationAsync()
        {
            // given: a permitted bypass. The verdict reports that a standing rejection was
            // waived, so the row must carry the waiver and the reason the caller gave for it —
            // and the outcome goes out on Approved, because a bypass approval IS an approval to
            // every subscriber and the waiver travels on the row rather than in the fact's name.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();

            // pinned rather than drawn. The filler leaves this pair free, and a draw that came
            // back true would turn the assertion below into a tautology — and would let the
            // whole write be deleted without a test noticing.
            storageAssociation.IsApprovedByBypass = false;
            storageAssociation.ApprovedByBypassReason = null;

            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);
            string bypassReason = GetRandomString();

            // The service copies onto the instance the storage read handed back, so the stored
            // values are snapshotted before the act. Reading them off that instance afterwards
            // would compare the row with itself.
            Association expectedStorageAssociation = storageAssociation.DeepClone();

            SetupAccessBrokerToPermitByBypass(AccessDenialReason.BlockedByRejection);

            Association savedAssociation = null;

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            SetupStorageRead(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Association, CancellationToken>(
                            (entity, _) => savedAssociation = entity.DeepClone())
                        .ReturnsAsync((Association entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Approved))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            // when
            Association actualAssociation =
                await this.associationService.BypassApproveAssociationAsync(
                    inputAssociation,
                    bypassReason,
                    TestContext.Current.CancellationToken);

            // then
            savedAssociation.Should().NotBeNull();

            // the waiver, taken from the verdict, and the excuse, taken from the argument
            savedAssociation.IsApprovedByBypass.Should().BeTrue();
            savedAssociation.ApprovedByBypassReason.Should().Be(bypassReason);

            // the three IApproval members a decision writes still arrive from the caller — a
            // bypass changes who may decide, not what a decision records
            savedAssociation.ApprovalStatus.Should().Be(inputAssociation.ApprovalStatus);
            savedAssociation.IsPublished.Should().Be(inputAssociation.IsPublished);
            savedAssociation.PublishDate.Should().Be(inputAssociation.PublishDate);

            // and nothing the operation does not own moved
            savedAssociation.CreatedBy.Should().Be(expectedStorageAssociation.CreatedBy);
            savedAssociation.EntityAKeyId.Should().Be(expectedStorageAssociation.EntityAKeyId);
            savedAssociation.EntityBKeyId.Should().Be(expectedStorageAssociation.EntityBKeyId);

            actualAssociation.IsApprovedByBypass.Should().BeTrue();
            actualAssociation.ApprovedByBypassReason.Should().Be(bypassReason);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectAssociationByIdAsync(
                        inputAssociation.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<Association>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        It.IsAny<Association>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // Approved, and no fact of its own. Verifying only this operation leaves any other
            // publish unverified, which is what makes the VerifyNoOtherCalls below bite.
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishAssociationAsync(
                        It.IsAny<EventEnvelope<Association>>(),
                        AssociationEventOperation.Approved),
                Times.Once);

            // the inbound request and the outbound fact, both against the bypass receiver
            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .AssociationOnBypassApprovingAssociationSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.AtLeastOnce);

            // the waiver's audit entry — which condition was overridden, server-side
            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(It.Is<string>(message =>
                    message.Contains(AccessDenialReason.BlockedByRejection.ToString()))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldIgnoreTheCallersBypassRecordOnBypassApproveAsync()
        {
            // given: the caller's entity says the opposite of the decision on BOTH fields — no
            // bypass, and a different excuse. If either were read off the entity, the caller
            // would be writing its own audit record: a genuine bypass could be sent as
            // IsApprovedByBypass = false and leave no trace, which is precisely the erasure the
            // derivation exists to prevent.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();
            storageAssociation.IsApprovedByBypass = false;
            storageAssociation.ApprovedByBypassReason = null;

            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);
            inputAssociation.IsApprovedByBypass = false;
            inputAssociation.ApprovedByBypassReason = $"caller-{Guid.NewGuid()}";

            string bypassReason = $"argument-{Guid.NewGuid()}";

            SetupAccessBrokerToPermitByBypass(AccessDenialReason.BlockedByUnresolvedComment);

            // when
            Association savedAssociation = await CaptureSavedAssociationOnBypassApproveAsync(
                storageAssociation,
                inputAssociation,
                bypassReason);

            // then
            savedAssociation.Should().NotBeNull();

            // the verdict, not the entity's flag
            savedAssociation.IsApprovedByBypass.Should().BeTrue();

            // the argument, not the entity's field
            savedAssociation.ApprovedByBypassReason.Should().Be(bypassReason);

            savedAssociation.ApprovedByBypassReason.Should()
                .NotBe(inputAssociation.ApprovedByBypassReason);
        }

        [Fact]
        public async Task ShouldNotRecordABypassOnBypassApproveWhenTheDecisionWaivedNothingAsync()
        {
            // given: a bypass was ASKED for and the conditions turned out to be met, so the
            // decision permitted without waiving anything. Recording a bypass here would
            // manufacture an audit entry for a waiver that never happened, which is as
            // misleading as losing one — and it would leave a reason attached to a row that
            // never needed one.
            //
            // The stored row already carries an earlier bypass, so a build that simply never
            // writes the pair fails this too rather than passing on the stale value.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();
            storageAssociation.IsApprovedByBypass = true;
            storageAssociation.ApprovedByBypassReason = "an earlier bypass";

            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);
            inputAssociation.IsApprovedByBypass = true;
            inputAssociation.ApprovedByBypassReason = $"caller-{Guid.NewGuid()}";

            string bypassReason = $"argument-{Guid.NewGuid()}";

            SetupAccessBrokerToPermit();

            // when
            Association savedAssociation = await CaptureSavedAssociationOnBypassApproveAsync(
                storageAssociation,
                inputAssociation,
                bypassReason);

            // then
            savedAssociation.Should().NotBeNull();
            savedAssociation.IsApprovedByBypass.Should().BeFalse();

            // NOT the supplied string. A reason recorded beside "no bypass was used" describes
            // an event that did not happen.
            savedAssociation.ApprovedByBypassReason.Should().BeNull();

            // and the approve itself still went through
            savedAssociation.ApprovalStatus.Should().Be(inputAssociation.ApprovalStatus);
        }

        [Theory]
        [InlineData(AccessDenialReason.BlockedByUnresolvedComment)]
        [InlineData(AccessDenialReason.BlockedByRejection)]
        public async Task ShouldLogWhichConditionWasWaivedOnBypassApproveAsync(
            AccessDenialReason bypassedBlockReason)
        {
            // given: the row records THAT the conditions were waived — IsApprovedByBypass plus
            // the actor's free text — but not WHICH one, and that second question is the one an
            // auditor asks: waiving a standing rejection and waiving a short approval count are
            // the difference between an incident and a launch-day override. The verdict knows;
            // it was simply discarded.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);
            string bypassReason = $"argument-{Guid.NewGuid()}";

            SetupAccessBrokerToPermitByBypass(bypassedBlockReason);

            // when
            await CaptureSavedAssociationOnBypassApproveAsync(
                storageAssociation,
                inputAssociation,
                bypassReason);

            // then: server-side only. The verdict's explanation names resolved policy values,
            // so §14.5 keeps it out of anything that surfaces to a caller — a log is where it
            // belongs, and where it can be found later.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(It.Is<string>(message =>
                    message.Contains(storageAssociation.Id.ToString())
                        && message.Contains(bypassedBlockReason.ToString())
                        && message.Contains(bypassReason))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldNotLogAWaiverOnBypassApproveWhenTheDecisionWaivedNothingAsync()
        {
            // given: a bypass was ASKED for and the conditions turned out to be met, so nothing
            // was waived. The row already refuses to record a bypass here; the log must agree,
            // or the audit trail gains an entry for an event that never happened — the same
            // failure as losing one, pointing the other way.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);
            string bypassReason = $"argument-{Guid.NewGuid()}";

            SetupAccessBrokerToPermit();

            // when
            await CaptureSavedAssociationOnBypassApproveAsync(
                storageAssociation,
                inputAssociation,
                bypassReason);

            // then
            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(It.IsAny<string>()),
                Times.Never);
        }

        // Runs a permitted bypass-approve end to end and hands back a snapshot of the row that
        // reached the storage broker. The snapshot is taken INSIDE the callback: the service
        // copies onto the instance the storage read handed it, so reading that instance after
        // the act would compare the row with itself and pass however the operation behaved.
        private async ValueTask<Association> CaptureSavedAssociationOnBypassApproveAsync(
            Association storageAssociation,
            Association inputAssociation,
            string bypassReason)
        {
            Association savedAssociation = null;

            SetupStorageRead(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Association, CancellationToken>(
                            (entity, _) => savedAssociation = entity.DeepClone())
                        .ReturnsAsync((Association entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<AssociationEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            await this.associationService.BypassApproveAssociationAsync(
                inputAssociation,
                bypassReason,
                TestContext.Current.CancellationToken);

            return savedAssociation;
        }
    }
}
