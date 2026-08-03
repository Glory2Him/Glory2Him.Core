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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyingTagEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<Tag>?> onModifyingTask =
                this.tagService.OnModifyingTagAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    onModifyingTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyingTagEventWhenTagNotFoundAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag inputTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag noTag = null!;

            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = inputTag,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundTagException = new NotFoundTagException(
                message: $"Tag not found with id: {inputTag.Id}.");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: notFoundTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputTag);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    inputTag.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noTag);

            // when
            ValueTask<EventEnvelope<Tag>?> onModifyingTask =
                this.tagService.OnModifyingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    onModifyingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    inputTag.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
