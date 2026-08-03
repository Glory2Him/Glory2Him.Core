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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldModifyApprovalSettingReviewerRoleAndReplyOnModifyingApprovalSettingReviewerRoleEventAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomModifyApprovalSettingReviewerRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingReviewerRole inputApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            ApprovalSettingReviewerRole auditAppliedApprovalSettingReviewerRole = inputApprovalSettingReviewerRole.DeepClone();
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = auditAppliedApprovalSettingReviewerRole.DeepClone();
            storageApprovalSettingReviewerRole.UpdatedWhen = storageApprovalSettingReviewerRole.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            ApprovalSettingReviewerRole auditPreservedApprovalSettingReviewerRole = auditAppliedApprovalSettingReviewerRole.DeepClone();
            ApprovalSettingReviewerRole updatedApprovalSettingReviewerRole = auditPreservedApprovalSettingReviewerRole.DeepClone();
            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = updatedApprovalSettingReviewerRole.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = inputApprovalSettingReviewerRole,
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalSettingReviewerRole);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    auditAppliedApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedApprovalSettingReviewerRole,
                    storageApprovalSettingReviewerRole))
                        .ReturnsAsync(auditPreservedApprovalSettingReviewerRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingReviewerRoleAsync(auditPreservedApprovalSettingReviewerRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedApprovalSettingReviewerRole);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingReviewerRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingReviewerRole>>(),
                    ApprovalSettingReviewerRoleEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingReviewerRole>>(
                        new EventPublishResult<ApprovalSettingReviewerRole>()));

            // when
            EventEnvelope<ApprovalSettingReviewerRole>? actualReplyEnvelope =
                await this.approvalSettingReviewerRoleService.OnModifyingApprovalSettingReviewerRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    auditAppliedApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingReviewerRoleAsync(auditPreservedApprovalSettingReviewerRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingReviewerRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingReviewerRole>>(),
                    ApprovalSettingReviewerRoleEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipModifyAndReplyNullWhenModifyingApprovalSettingReviewerRoleEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = new ApprovalSettingReviewerRole { Id = Guid.NewGuid() },
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalSettingReviewerRole>? actualReplyEnvelope =
                await this.approvalSettingReviewerRoleService.OnModifyingApprovalSettingReviewerRoleAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
