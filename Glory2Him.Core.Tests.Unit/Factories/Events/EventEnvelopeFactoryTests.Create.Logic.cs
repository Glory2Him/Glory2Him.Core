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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Factories.Events
{
    public partial class EventEnvelopeFactoryTests
    {
        [Fact]
        public async Task ShouldCreateRootEnvelopeCapturingCurrentCallerAsync()
        {
            // given
            var content = new ContentType { Id = Guid.NewGuid() };
            SecurityContext randomSecurityContext = CreateRandomSecurityContext();
            Guid randomEventId = Guid.NewGuid();
            Guid randomCorrelationId = Guid.NewGuid();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            this.securityBrokerMock.Setup(broker =>
                broker.GetCurrentSecurityContextAsync())
                    .Returns(new ValueTask<SecurityContext>(randomSecurityContext));

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .Returns(new ValueTask<Guid>(randomEventId))
                    .Returns(new ValueTask<Guid>(randomCorrelationId));

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .Returns(new ValueTask<DateTimeOffset>(randomDateTimeOffset));

            // when
            EventEnvelope<ContentType> actualEnvelope =
                await this.eventEnvelopeFactory.CreateAsync(content);

            // then
            actualEnvelope.Content.Should().BeSameAs(content);
            actualEnvelope.SecurityContext.Should().BeSameAs(randomSecurityContext);
            actualEnvelope.RequestContext.CorrelationId.Should().Be(randomCorrelationId);
            actualEnvelope.RequestContext.RequestedDate.Should().Be(randomDateTimeOffset);
            actualEnvelope.RequestContext.SourceSystem.Should().Be("Glory2Him.Core");
            actualEnvelope.Metadata.EventId.Should().Be(randomEventId);
            actualEnvelope.Metadata.EventType.Should().Be(nameof(ContentType));
            actualEnvelope.Metadata.Version.Should().Be(1);
            actualEnvelope.Metadata.RetryCount.Should().Be(0);
            actualEnvelope.Metadata.CausationId.Should().BeNull();
            actualEnvelope.Metadata.ParentCorrelationId.Should().BeNull();

            this.securityBrokerMock.Verify(broker =>
                broker.GetCurrentSecurityContextAsync(),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
        }
    }
}
