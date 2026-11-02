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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldAddTagAndReplyOnAddingTagEventAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag randomTag = CreateTagFiller(randomDateTimeOffset).Create();
            Tag inputTag = randomTag;
            Tag auditAppliedTag = inputTag.DeepClone();
            Tag storageTag = auditAppliedTag.DeepClone();
            Tag expectedTag = storageTag.DeepClone();

            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = inputTag,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertTagAsync(auditAppliedTag, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageTag);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(It.IsAny<EventEnvelope<Tag>>(), TagEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<Tag>>(
                        new EventPublishResult<Tag>()));

            // when
            EventEnvelope<Tag>? actualReplyEnvelope =
                await this.tagService.OnAddingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedTag);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertTagAsync(auditAppliedTag, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ClaimedTagIds))]
        public async Task ShouldAddTagUnderAMintedIdOnAddingTagEventAsync(Guid claimedId)
        {
            // given
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string contributorUserId = Guid.NewGuid().ToString();
            Tag inputTag = CreateTagFiller(currentDateTime, contributorUserId).Create();

            // the publisher's claim is neither compared nor refused: the row takes the Id the
            // service mints (TagService.TagSuggestions.md §2 rule 1)
            inputTag.Id = claimedId;
            Guid mintedId = Guid.NewGuid();
            mintedId.Should().NotBe(claimedId);
            Guid firstProcessedEventId = Guid.NewGuid();
            Guid secondProcessedEventId = Guid.NewGuid();

            Tag expectedInsertedTag = inputTag.DeepClone();
            expectedInsertedTag.Id = mintedId;
            Tag storedTag = expectedInsertedTag.DeepClone();
            Tag insertedTag = null;
            CancellationToken inputCancellationToken = TestContext.Current.CancellationToken;
            SecurityContext contributorSecurityContext = CreateAuthenticatedSecurityContext();

            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = contributorSecurityContext,
                Content = inputTag,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            string expectedEventName =
                $"{nameof(Tag)}{TagEventOperation.Adding}";

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    inputCancellationToken))
                        .ReturnsAsync(false);

            // the mint is the identifier broker's first call in the add; the two that follow
            // are the processed-event records written after the insert
            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(mintedId)
                    .ReturnsAsync(firstProcessedEventId)
                    .ReturnsAsync(secondProcessedEventId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputTag, contributorSecurityContext))
                    .ReturnsAsync((Tag entity, SecurityContext _) => entity);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(contributorSecurityContext))
                    .ReturnsAsync(contributorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertTagAsync(
                    It.Is<Tag>(tag => tag.Id == mintedId),
                    inputCancellationToken))
                        .Callback<Tag, CancellationToken>((entity, _) =>
                            insertedTag = entity.DeepClone())
                        .ReturnsAsync(storedTag);

            // when
            EventEnvelope<Tag>? actualReplyEnvelope =
                await this.tagService.OnAddingTagAsync(
                    requestEnvelope,
                    inputCancellationToken);

            // then: the row is inserted under the minted Id, never the claimed one, and the
            // Tag-Added fact and the reply carry the minted Id
            insertedTag.Should().BeEquivalentTo(expectedInsertedTag);
            insertedTag.Id.Should().Be(mintedId);
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeSameAs(storedTag);
            actualReplyEnvelope.Content.Id.Should().Be(mintedId);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    requestEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    inputCancellationToken),
                Times.Once);

            // one Id for the new row, and one for each of the two processed-event records
            this.identifierBrokerMock.Verify(broker =>
                    broker.GetIdentifierAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputTag, contributorSecurityContext),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(contributorSecurityContext),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertTagAsync(
                        It.Is<Tag>(tag => tag.Id == mintedId),
                        inputCancellationToken),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishTagAsync(
                    It.Is<EventEnvelope<Tag>>(envelope =>
                        envelope.Content == storedTag
                            && envelope.Content.Id == mintedId),
                    TagEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.Id == firstProcessedEventId
                            && processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
                    inputCancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.Id == secondProcessedEventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.TagOnAddingTagSubscriptionName),
                    inputCancellationToken),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipAddAndReplyNullWhenAddingTagEventAlreadyProcessedAsync()
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
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Tag>? actualReplyEnvelope =
                await this.tagService.OnAddingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
