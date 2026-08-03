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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrievingLinkByIdEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<Link>?> onRetrieveTask =
                this.linkService.OnRetrievingLinkByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    onRetrieveTask.AsTask);

            // then
            actualLinkValidationException.Should().BeEquivalentTo(
                expectedLinkValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkValidationException))),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughNotFoundValidationExceptionOnRetrievingLinkByIdEventAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Link { Id = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    requestEnvelope.Content.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync((Link?)null);

            // when
            ValueTask<EventEnvelope<Link>?> onRetrieveTask =
                this.linkService.OnRetrievingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualLinkValidationException =
                await Assert.ThrowsAsync<LinkValidationException>(
                    onRetrieveTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped —
            // the substrate wrapper must not double-wrap it.
            actualLinkValidationException.InnerException
                .Should().BeOfType<NotFoundLinkException>();

            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
