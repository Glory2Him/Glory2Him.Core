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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldModifyApprovalSettingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);
            ApprovalSetting inputApprovalSetting = randomApprovalSetting;
            ApprovalSetting auditAppliedApprovalSetting = inputApprovalSetting.DeepClone();
            ApprovalSetting storageApprovalSetting = auditAppliedApprovalSetting.DeepClone();
            storageApprovalSetting.UpdatedWhen = storageApprovalSetting.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            ApprovalSetting auditPreservedApprovalSetting = auditAppliedApprovalSetting.DeepClone();
            ApprovalSetting updatedApprovalSetting = auditPreservedApprovalSetting.DeepClone();
            ApprovalSetting expectedApprovalSetting = updatedApprovalSetting.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    auditAppliedApprovalSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedApprovalSetting,
                    storageApprovalSetting))
                        .ReturnsAsync(auditPreservedApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingAsync(auditPreservedApprovalSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedApprovalSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSetting>>(
                        new EventPublishResult<ApprovalSetting>()));

            // when
            ApprovalSetting actualApprovalSetting =
                await this.approvalSettingService.ModifyApprovalSettingAsync(
                    inputApprovalSetting,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputApprovalSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectApprovalSettingByIdAsync(
                        auditAppliedApprovalSetting.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedApprovalSetting,
                        storageApprovalSetting),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalSettingAsync(auditPreservedApprovalSetting, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishApprovalSettingAsync(
                        It.IsAny<EventEnvelope<ApprovalSetting>>(),
                        ApprovalSettingEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // §8.6.2.1 criterion 1, the modify twin of the add test beside it: the caller's chosen
        // value round-trips untouched, in both directions.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldModifyApprovalSettingWithTheCallersIsAIReviewerAutomaticallyRequestedAsync(
            bool isAIReviewerAutomaticallyRequested)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalSetting randomApprovalSetting =
                CreateRandomModifyApprovalSetting(randomDateTimeOffset, randomUserId);

            randomApprovalSetting.IsAIReviewerAutomaticallyRequested = isAIReviewerAutomaticallyRequested;
            ApprovalSetting inputApprovalSetting = randomApprovalSetting;
            ApprovalSetting auditAppliedApprovalSetting = inputApprovalSetting.DeepClone();
            ApprovalSetting storageApprovalSetting = auditAppliedApprovalSetting.DeepClone();
            storageApprovalSetting.UpdatedWhen = storageApprovalSetting.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            // Deliberately the OPPOSITE of what the caller is modifying to. Left as the clone's
            // (matching) value, a "read the stored row back over the caller's change" bug would
            // coincidentally produce the same field value the test expects, and the assertion
            // below could not tell that apart from the caller's choice actually round-tripping.
            storageApprovalSetting.IsAIReviewerAutomaticallyRequested = !isAIReviewerAutomaticallyRequested;

            ApprovalSetting auditPreservedApprovalSetting = auditAppliedApprovalSetting.DeepClone();
            ApprovalSetting updatedApprovalSetting = auditPreservedApprovalSetting.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    auditAppliedApprovalSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedApprovalSetting,
                    storageApprovalSetting))
                        .ReturnsAsync(auditPreservedApprovalSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingAsync(auditPreservedApprovalSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedApprovalSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingAsync(
                    It.IsAny<EventEnvelope<ApprovalSetting>>(),
                    ApprovalSettingEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSetting>>(
                        new EventPublishResult<ApprovalSetting>()));

            // when
            ApprovalSetting actualApprovalSetting =
                await this.approvalSettingService.ModifyApprovalSettingAsync(
                    inputApprovalSetting,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSetting.IsAIReviewerAutomaticallyRequested
                .Should().Be(isAIReviewerAutomaticallyRequested);

            // Asserted again against what was actually HANDED TO STORAGE, not only against the
            // pre-baked return value above — same reasoning as the Add twin. It.Is inspects the
            // actual argument UpdateApprovalSettingAsync was called with, which is what tells a
            // "read the stored row back over the caller's change" bug apart from the caller's
            // value genuinely round-tripping, now that the stored row above deliberately holds
            // the opposite value.
            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingAsync(
                    It.Is<ApprovalSetting>(setting =>
                        setting.IsAIReviewerAutomaticallyRequested
                            == isAIReviewerAutomaticallyRequested),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
