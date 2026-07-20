// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Factories.Events
{
    public partial class EventEnvelopeFactoryTests
    {
        [Fact]
        public async Task ShouldCreateNextEnvelopeCarryingSourceContextForwardAsync()
        {
            // given
            var sourceEnvelope = new EventEnvelope<ContentType>
            {
                Content = new ContentType { Id = Guid.NewGuid() },
                SecurityContext = CreateRandomSecurityContext(),

                RequestContext = new RequestContext
                {
                    CorrelationId = Guid.NewGuid(),
                    RequestedDate = GetRandomDateTimeOffset(),
                    SourceSystem = GetRandomString()
                },

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    EventType = nameof(ContentType),
                    Version = 1,
                    ParentCorrelationId = Guid.NewGuid()
                }
            };

            // a different content type than the source proves EventType reflects the reply
            var nextContent = new ContentItem { Id = Guid.NewGuid() };
            Guid randomEventId = Guid.NewGuid();

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .Returns(new ValueTask<Guid>(randomEventId));

            // when
            EventEnvelope<ContentItem> actualEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(sourceEnvelope, nextContent);

            // then
            actualEnvelope.Content.Should().BeSameAs(nextContent);

            // the guarantees the request/reply chain depends on: the source's security and
            // request context ride forward untouched — never recaptured from the ambient caller
            actualEnvelope.SecurityContext.Should().BeSameAs(sourceEnvelope.SecurityContext);
            actualEnvelope.RequestContext.Should().BeSameAs(sourceEnvelope.RequestContext);

            actualEnvelope.Metadata.EventId.Should().Be(randomEventId);
            actualEnvelope.Metadata.EventType.Should().Be(nameof(ContentItem));
            actualEnvelope.Metadata.Version.Should().Be(1);
            actualEnvelope.Metadata.RetryCount.Should().Be(0);

            actualEnvelope.Metadata.CausationId.Should().Be(
                sourceEnvelope.Metadata.EventId.ToString());

            actualEnvelope.Metadata.ParentCorrelationId.Should().Be(
                sourceEnvelope.Metadata.ParentCorrelationId);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Once);

            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
        }
    }
}
