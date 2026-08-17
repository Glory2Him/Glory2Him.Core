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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    /// <summary>
    /// The version fork's demotion of the previous latest row — the only operation permitted to
    /// move <c>IsLatestVersion</c> (design §3.4 rule 18, §9.7.1 rule 2).
    /// </summary>
    public partial class ContentItemServiceTests
    {
        // A stored row as the fork finds it: the current tip, and published as an approved one
        // would be, so a test can prove the demotion leaves publication alone.
        private ContentItem CreateDemotableStorageContentItem(string ownerUserId)
        {
            ContentItem contentItem = CreateRandomContentItem();
            contentItem.IsDeleted = false;
            contentItem.IsLatestVersion = true;
            contentItem.ApprovalStatus = ApprovalStatus.Approved;
            contentItem.IsPublished = true;
            contentItem.PublishDate = GetRandomDateTimeOffset();
            contentItem.CreatedBy = ownerUserId;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(ownerUserId);

            return contentItem;
        }

        [Fact]
        public async Task ShouldDemoteContentItemVersionAsync()
        {
            // given: the write the fork could not make before. IsLatestVersion is an IVersion
            // member and the general modify pins it, so demoting through the modify asked the
            // one path required to refuse this write to make it.
            string ownerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            ContentItem storageContentItem = CreateDemotableStorageContentItem(ownerUserId);
            ContentItem savedContentItem = null;

            SetupContentItemStorageRead(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ContentItem entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ContentItem, CancellationToken>(
                            (entity, _) => savedContentItem = entity.DeepClone())
                        .ReturnsAsync((ContentItem entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            await this.contentItemService.DemoteContentItemVersionAsync(
                storageContentItem.Id,
                TestContext.Current.CancellationToken);

            // then
            savedContentItem.IsLatestVersion.Should().BeFalse();

            // IsLatestVersion marks the edit tip and nothing else. The previously published row
            // stays publicly visible until the new version is approved (§3.4.1), so a demotion
            // that touched publication would take the group dark mid-fork.
            savedContentItem.IsPublished.Should().BeTrue();
            savedContentItem.PublishDate.Should().Be(storageContentItem.PublishDate);
            savedContentItem.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

            // its OWN fact — never Modified, which the approval workflow reads as an amendment
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Demoted),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Modified),
                Times.Never);
        }

        [Theory]
        [InlineData(Roles.Publisher)]
        [InlineData(Roles.Admin)]
        [InlineData(Roles.Reviewer)]
        public async Task ShouldThrowUnauthorizedOnDemoteIfCallerIsNotTheOwnerAsync(
            string role)
        {
            // given: §3.4 rule 8 — the owner is the only creator of new versions, and Publisher
            // and Admin roles never fork one. The demotion is a step inside that fork, so the
            // roles that may otherwise write the row are refused it. A Reviewer holds write
            // permission through the modify and must still never move the version tip.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(role);
            ContentItem storageContentItem = CreateDemotableStorageContentItem(GetRandomString());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            SetupContentItemStorageRead(storageContentItem);

            var unauthorizedContentItemException =
                new UnauthorizedContentItemException(
                    message: "The current user is not allowed to demote this content item version.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> demoteTask =
                this.contentItemService.DemoteContentItemVersionAsync(
                    storageContentItem.Id,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(demoteTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDemoteIfTheRowIsNotTheLatestAsync()
        {
            // given: a row that is already not the tip has nothing to demote. Letting the call
            // through would publish a Demoted fact for a write that changed nothing, leaving a
            // subscriber to infer a version move that never happened.
            string ownerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            ContentItem storageContentItem = CreateDemotableStorageContentItem(ownerUserId);
            storageContentItem.IsLatestVersion = false;

            SetupContentItemStorageRead(storageContentItem);

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is not the latest version and cannot be demoted.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            // when
            ValueTask<ContentItem> demoteTask =
                this.contentItemService.DemoteContentItemVersionAsync(
                    storageContentItem.Id,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(demoteTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnDemoteIfTheRowIsSoftDeletedAsync()
        {
            // given: a removed row is a takedown. Reported as not-found, matching the read
            // posture, so a removed id is not distinguishable from one that never existed.
            string ownerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            ContentItem storageContentItem = CreateDemotableStorageContentItem(ownerUserId);
            storageContentItem.IsDeleted = true;

            SetupContentItemStorageRead(storageContentItem);

            var notFoundContentItemException =
                new NotFoundContentItemException(
                    message: $"Content item not found with id: {storageContentItem.Id}.");

            // when
            ValueTask<ContentItem> demoteTask =
                this.contentItemService.DemoteContentItemVersionAsync(
                    storageContentItem.Id,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(demoteTask.AsTask);

            // then
            actualException.InnerException.Should()
                .BeEquivalentTo(notFoundContentItemException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDemoteIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.Id),
                values: "Id is required");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            // when
            ValueTask<ContentItem> demoteTask =
                this.contentItemService.DemoteContentItemVersionAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(demoteTask.AsTask);

            // then: the row was never read
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
