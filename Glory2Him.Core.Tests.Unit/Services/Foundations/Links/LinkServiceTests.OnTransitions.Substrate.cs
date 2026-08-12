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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        // ── OnSubmitting ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldSubmitOnSubmittingLinkEventAsync()
        {
            // given: the event path carries the id in the envelope; the do-work reads only the
            // id off it and drives the row Draft -> Submitted, exactly as the direct path does
            Link storageLink = CreateSubmittableStorageLink();

            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Link { Id = storageLink.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageLink.CreatedBy);

            SetupLinkStorageRead(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Link>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Link entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(
                    It.IsAny<Link>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Link entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Link>>(
                            new EventPublishResult<Link>()));

            // when
            EventEnvelope<Link>? actualReplyEnvelope =
                await this.linkService.OnSubmittingLinkAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipSubmitAndReplyNullWhenSubmittingLinkEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Link { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnSubmittingLinkSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Link>? actualReplyEnvelope =
                await this.linkService.OnSubmittingLinkAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnSubmittingLinkSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmittingLinkEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Link>? nullEnvelope = null;

            var invalidLinkEventException =
                new InvalidLinkEventException(
                    message: "Invalid link event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkEventException);

            // when
            ValueTask<EventEnvelope<Link>?> onSubmittingTask =
                this.linkService.OnSubmittingLinkAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    onSubmittingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        // ── OnApproving ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldApproveOnApprovingLinkEventAsync()
        {
            // given
            Link storageLink = CreateApprovableStorageLink();

            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = CreateApprovalDecision(storageLink.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupLinkStorageRead(storageLink);
            SetupAccessBrokerToPermit();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Link>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Link entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(
                    It.IsAny<Link>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Link entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Link>>(
                            new EventPublishResult<Link>()));

            // when
            EventEnvelope<Link>? actualReplyEnvelope =
                await this.linkService.OnApprovingLinkAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Approved),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipApproveAndReplyNullWhenApprovingLinkEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = CreateApprovalDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnApprovingLinkSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Link>? actualReplyEnvelope =
                await this.linkService.OnApprovingLinkAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: a duplicate approve neither re-decides nor re-announces
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnApprovingLinkSubscriptionName,
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
        public async Task ShouldThrowValidationExceptionOnApprovingLinkEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Link>? nullEnvelope = null;

            var invalidLinkEventException =
                new InvalidLinkEventException(
                    message: "Invalid link event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedLinkValidationException =
                new LinkValidationException(
                    message: "Link validation error occurred, fix the errors and try again.",
                    innerException: invalidLinkEventException);

            // when
            ValueTask<EventEnvelope<Link>?> onApprovingTask =
                this.linkService.OnApprovingLinkAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    onApprovingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedLinkValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
