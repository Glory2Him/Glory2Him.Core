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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    /// <summary>
    /// §8.4's SCOPE rules, refused at the service.
    ///
    /// <para><b>Why these exist as service rules at all.</b> Two database check constraints say
    /// the same thing, and until the global tier arrived they said it completely: while
    /// <c>EntityType</c> was NOT NULL, <c>ContentType IS NULL OR EntityType = N'ContentItem'</c>
    /// was determinate. Making it nullable left the right-hand side UNKNOWN for a null entity
    /// type, and a CHECK constraint ADMITS what it cannot decide — so both constraints silently
    /// stopped refusing the one shape the new tier made expressible.</para>
    ///
    /// <para>The constraints are still there, null-safe now, as the defence in depth §14.6 rule 2
    /// asks for. These rules are what turns a refusal into something a caller can act on: a
    /// dependency failure from a check constraint names no field.</para>
    /// </summary>
    public partial class ApprovalSettingServiceTests
    {
        public static TheoryData<EntityType?, ContentType?, bool?, string, string> InvalidScopes() =>
            new()
            {
                // The global row narrows nothing — it is the row every entity-type default
                // narrows, so a narrowing on it names a scope that cannot exist.
                {
                    null, ContentType.Quote, null,
                    nameof(ApprovalSetting.EntityType),
                    "Entity type is required for a setting narrowed by content type or personality"
                },
                {
                    null, null, true,
                    nameof(ApprovalSetting.EntityType),
                    "Entity type is required for a setting narrowed by content type or personality"
                },

                // Only a content item carries a content type, and only an association a
                // personality (§8.4, §4.2).
                {
                    EntityType.Tag, ContentType.Story, null,
                    nameof(ApprovalSetting.ContentType),
                    "Content type is only valid on a content item setting"
                },
                {
                    EntityType.Association, ContentType.Story, null,
                    nameof(ApprovalSetting.ContentType),
                    "Content type is only valid on a content item setting"
                },
                {
                    EntityType.Tag, null, false,
                    nameof(ApprovalSetting.IsPersonal),
                    "Personal scope is only valid on an association setting"
                },
                {
                    EntityType.ContentItem, null, true,
                    nameof(ApprovalSetting.IsPersonal),
                    "Personal scope is only valid on an association setting"
                },
            };

        [Theory]
        [MemberData(nameof(InvalidScopes))]
        public async Task ShouldThrowValidationExceptionOnAddIfTheScopeIsNotOneEightPointFourAllowsAsync(
            EntityType? invalidEntityType,
            ContentType? invalidContentType,
            bool? invalidIsPersonal,
            string expectedParameter,
            string expectedMessage)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ApprovalSetting invalidApprovalSetting = CreateRandomApprovalSetting();
            invalidApprovalSetting.EntityType = invalidEntityType;
            invalidApprovalSetting.ContentType = invalidContentType;
            invalidApprovalSetting.IsPersonal = invalidIsPersonal;
            invalidApprovalSetting.CreatedBy = randomUserId;
            invalidApprovalSetting.UpdatedBy = randomUserId;
            invalidApprovalSetting.CreatedWhen = randomDateTimeOffset;
            invalidApprovalSetting.UpdatedWhen = randomDateTimeOffset;

            var invalidApprovalSettingException =
                new InvalidApprovalSettingException(
                    message: "Approval setting is invalid, fix the errors and try again.");

            invalidApprovalSettingException.AddData(
                key: expectedParameter,
                values: expectedMessage);

            var expectedApprovalSettingValidationException =
                new ApprovalSettingValidationException(
                    message: "Approval setting validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSetting> addApprovalSettingTask =
                this.approvalSettingService.AddApprovalSettingAsync(
                    invalidApprovalSetting,
                    TestContext.Current.CancellationToken);

            ApprovalSettingValidationException actualApprovalSettingValidationException =
                await Assert.ThrowsAsync<ApprovalSettingValidationException>(
                    addApprovalSettingTask.AsTask);

            // then
            actualApprovalSettingValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingValidationException);

            // and nothing was written — a scope the design refuses never reaches storage
            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalSettingAsync(
                    It.IsAny<ApprovalSetting>(),
                    It.IsAny<System.Threading.CancellationToken>()),
                Times.Never);
        }
    }
}
