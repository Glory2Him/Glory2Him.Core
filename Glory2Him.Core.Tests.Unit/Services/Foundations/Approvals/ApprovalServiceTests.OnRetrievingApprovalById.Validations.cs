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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingApprovalByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Approval>? nullEnvelope = null;

            var invalidApprovalEventException =
                new InvalidApprovalEventException(
                    message: "Invalid approval event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalEventException);

            // when
            ValueTask<EventEnvelope<Approval>?> onRetrieveTask =
                this.approvalService.OnRetrievingApprovalByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    onRetrieveTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughNotFoundValidationExceptionOnRetrievingApprovalByIdEventAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Approval>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content =new Approval { Id = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    requestEnvelope.Content.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((Approval?)null);

            // when
            ValueTask<EventEnvelope<Approval>?> onRetrieveTask =
                this.approvalService.OnRetrievingApprovalByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    onRetrieveTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped —
            // the substrate wrapper must not double-wrap it.
            actualApprovalValidationException.InnerException
                .Should().BeOfType<NotFoundApprovalException>();

            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
