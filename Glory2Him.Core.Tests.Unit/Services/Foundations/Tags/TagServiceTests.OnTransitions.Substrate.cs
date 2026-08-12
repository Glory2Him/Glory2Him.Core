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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        // ── OnSubmitting ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldSubmitOnSubmittingTagEventAsync()
        {
            // given: the event path carries the id in the envelope; the do-work reads only the
            // id off it and drives the row Draft -> Submitted, exactly as the direct path does
            Tag storageTag = CreateSubmittableStorageTag();

            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Tag { Id = storageTag.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageTag.CreatedBy);

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

            // when
            EventEnvelope<Tag>? actualReplyEnvelope =
                await this.tagService.OnSubmittingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipSubmitAndReplyNullWhenSubmittingTagEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Tag { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnSubmittingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Tag>? actualReplyEnvelope =
                await this.tagService.OnSubmittingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnSubmittingTagSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmittingTagEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Tag>? nullEnvelope = null;

            var invalidTagEventException =
                new InvalidTagEventException(
                    message: "Invalid tag event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagEventException);

            // when
            ValueTask<EventEnvelope<Tag>?> onSubmittingTask =
                this.tagService.OnSubmittingTagAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(
                    onSubmittingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        // ── OnApproving ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldApproveOnApprovingTagEventAsync()
        {
            // given
            Tag storageTag = CreateApprovableStorageTag();

            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = CreateApprovalDecision(storageTag.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupTagStorageRead(storageTag);
            SetupAccessBrokerToPermit();

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

            // when
            EventEnvelope<Tag>? actualReplyEnvelope =
                await this.tagService.OnApprovingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Approved),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipApproveAndReplyNullWhenApprovingTagEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = CreateApprovalDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnApprovingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Tag>? actualReplyEnvelope =
                await this.tagService.OnApprovingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: a duplicate approve neither re-decides nor re-announces
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnApprovingTagSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApprovingTagEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Tag>? nullEnvelope = null;

            var invalidTagEventException =
                new InvalidTagEventException(
                    message: "Invalid tag event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagEventException);

            // when
            ValueTask<EventEnvelope<Tag>?> onApprovingTask =
                this.tagService.OnApprovingTagAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(
                    onApprovingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
