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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    // THE ROW-SHAPED GATE, gathered in one file rather than split across the four write
    // operations it governs. The rule is a single sentence — a per-type DEFAULT is
    // Administrators-only, an item OVERRIDE admits the publisher tier for its content type —
    // and a rule read in one place is a rule that can be checked for holes. The per-operation
    // Validations files keep the assertions that are genuinely about their own operation.
    public partial class ContentItemSettingServiceTests
    {
        // The three tiers §18.6 composes for one content type, each on its own so a hole in any
        // one of them is a red test rather than a case the theory happened not to cover.
        public static TheoryData<string[]> PublisherTierRoleSets() =>
            new TheoryData<string[]>
            {
                new[] { Roles.Publishers },
                new[] { Roles.ContentItemPublishers },
                new[] { Roles.PublishersFor(EntityType.ContentItem, ScopeTestContentType) }
            };

        // The blocks drawn from the same scope the grant is drawn from. Each is paired with
        // Administrators deliberately: §18.6 rule 2 says no grant overrides a block whose scope
        // covers the row, and the widest grant there is is the one worth proving it against.
        public static TheoryData<string[]> ContentItemBlockRoleSets() =>
            new TheoryData<string[]>
            {
                new[] { Roles.Administrators, Roles.ContentItemReadOnly },
                new[]
                {
                    Roles.Administrators,
                    Roles.ReadOnlyFor(EntityType.ContentItem, ScopeTestContentType)
                }
            };

        // Two DIFFERENT members, pinned rather than drawn. The filler draws a content type, and a
        // narrow-tier test that let it draw would pass whenever the draw happened to agree —
        // proving nothing about the check it exists to exercise.
        private const ContentType ScopeTestContentType = ContentType.Devotional;
        private const ContentType OtherScopeTestContentType = ContentType.Quote;

        [Theory]
        [MemberData(nameof(PublisherTierRoleSets))]
        public async Task ShouldThrowValidationExceptionOnAddIfPublisherAuthorsADefaultAndLogItAsync(
            string[] publisherRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(publisherRoles);
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentType = ScopeTestContentType;

            // the row that governs the whole content type — the one a publisher may not author
            someContentItemSetting.ContentItemId = null;

            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is not allowed to administer content type default settings.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfNarrowPublisherAuthorsAnotherContentTypeAndLogItAsync()
        {
            // given: trusted with one content type, writing an override of another
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.PublishersFor(EntityType.ContentItem, OtherScopeTestContentType));

            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentType = ScopeTestContentType;

            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is not allowed to administer settings for this content type.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ContentItemBlockRoleSets))]
        public async Task ShouldThrowValidationExceptionOnAddIfContentItemBlockCoversTheRowAndLogItAsync(
            string[] blockedRoles)
        {
            // given: an administrator, blocked at a scope that covers this row
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRoles);
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            someContentItemSetting.ContentType = ScopeTestContentType;

            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is blocked from administering content item settings.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> addContentItemSettingTask =
                this.contentItemSettingService.AddContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    addContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(PublisherTierRoleSets))]
        public async Task ShouldAddOverrideWhenCallerHoldsPublisherTierAsync(string[] publisherRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(publisherRoles);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItemSetting randomContentItemSetting =
                CreateContentItemSettingFiller(randomDateTimeOffset).Create();

            randomContentItemSetting.ContentType = ScopeTestContentType;
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            ContentItemSetting auditAppliedContentItemSetting = inputContentItemSetting.DeepClone();
            ContentItemSetting storageContentItemSetting = auditAppliedContentItemSetting.DeepClone();
            ContentItemSetting expectedContentItemSetting = storageContentItemSetting.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentItemSetting.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertContentItemSettingAsync(auditAppliedContentItemSetting, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItemSetting);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<ContentItemSetting>>(
                        new EventPublishResult<ContentItemSetting>()));

            // when
            ContentItemSetting actualContentItemSetting =
                await this.contentItemSettingService.AddContentItemSettingAsync(
                    inputContentItemSetting,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSetting.Should().BeEquivalentTo(expectedContentItemSetting);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertContentItemSettingAsync(
                        auditAppliedContentItemSetting,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemSettingAsync(
                    It.IsAny<EventEnvelope<ContentItemSetting>>(),
                    ContentItemSettingEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ContentItemSettingOnAddingContentItemSettingSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // THE STORED ROW DECIDES. The caller sends a row that looks like an override — the shape
        // they are entitled to write — while the row actually in storage is a default. A gate
        // that read the caller's copy would admit this.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfStoredRowIsADefaultAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);

            randomContentItemSetting.ContentType = ScopeTestContentType;
            ContentItemSetting inputContentItemSetting = randomContentItemSetting;
            ContentItemSetting storageContentItemSetting = randomContentItemSetting.DeepClone();
            storageContentItemSetting.ContentItemId = null;

            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is not allowed to administer content type default settings.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    inputContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    inputContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    inputContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Proven as an ADMINISTRATOR on purpose: the pin is an invariant about what a row is, not
        // a role check. Somebody who may write both scopes still may not move a row between them.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemIdIsChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);

            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            ContentItemSetting storageContentItemSetting = randomContentItemSetting.DeepClone();
            storageContentItemSetting.ContentItemId = Guid.NewGuid();
            storageContentItemSetting.UpdatedWhen = randomContentItemSetting.UpdatedWhen.AddDays(-1);

            var invalidContentItemSettingException = new InvalidContentItemSettingException(
                message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.ContentItemId),
                values: $"Value is not the same as {nameof(ContentItemSetting.ContentItemId)}");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting))
                        .ReturnsAsync(invalidContentItemSetting);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentTypeIsChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ContentItemSetting randomContentItemSetting =
                CreateRandomModifyContentItemSetting(randomDateTimeOffset, randomUserId);

            randomContentItemSetting.ContentType = ScopeTestContentType;
            ContentItemSetting invalidContentItemSetting = randomContentItemSetting;
            ContentItemSetting storageContentItemSetting = randomContentItemSetting.DeepClone();
            storageContentItemSetting.ContentType = OtherScopeTestContentType;
            storageContentItemSetting.UpdatedWhen = randomContentItemSetting.UpdatedWhen.AddDays(-1);

            var invalidContentItemSettingException = new InvalidContentItemSettingException(
                message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.AddData(
                key: nameof(ContentItemSetting.ContentType),
                values: $"Value is not the same as {nameof(ContentItemSetting.ContentType)}");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemSettingException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting))
                        .ReturnsAsync(invalidContentItemSetting);

            // when
            ValueTask<ContentItemSetting> modifyContentItemSettingTask =
                this.contentItemSettingService.ModifyContentItemSettingAsync(
                    invalidContentItemSetting,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    modifyContentItemSettingTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemSetting, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    invalidContentItemSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemSetting,
                    storageContentItemSetting),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveIfNarrowPublisherRemovesAnotherContentTypeAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.PublishersFor(EntityType.ContentItem, OtherScopeTestContentType));

            Guid randomContentItemSettingId = Guid.NewGuid();
            Guid inputContentItemSettingId = randomContentItemSettingId;

            ContentItemSetting storageContentItemSetting =
                CreateContentItemSettingFiller(GetRandomDateTimeOffset()).Create();

            storageContentItemSetting.ContentType = ScopeTestContentType;

            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is not allowed to administer settings for this content type.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    inputContentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    inputContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    inputContentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
