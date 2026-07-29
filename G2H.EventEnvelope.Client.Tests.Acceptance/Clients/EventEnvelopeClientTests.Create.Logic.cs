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
using G2H.EventEnvelope.Client.Models.Foundations;

namespace G2H.EventEnvelope.Client.Tests.Acceptance.Clients
{
    public partial class EventEnvelopeClientTests
    {
        [Fact]
        public async Task ShouldCreateEventEnvelopeAsync()
        {
            // given
            string randomContent = GetRandomString();

            // when
            EventEnvelope<string> actualEventEnvelope =
                await this.eventEnvelopeClient.CreateAsync(randomContent);

            // then
            actualEventEnvelope.Content.Should().Be(randomContent);
            actualEventEnvelope.SecurityContext.Should().NotBeNull();
            actualEventEnvelope.RequestContext.CorrelationId.Should().NotBe(Guid.Empty);
            actualEventEnvelope.RequestContext.SourceSystem.Should().Be("Glory2Him.Core");

            actualEventEnvelope.RequestContext.RequestedDate.Should().BeCloseTo(
                DateTimeOffset.UtcNow,
                TimeSpan.FromMinutes(1));

            actualEventEnvelope.Metadata.EventId.Should().NotBe(Guid.Empty);
            actualEventEnvelope.Metadata.EventType.Should().Be(nameof(String));
            actualEventEnvelope.Metadata.Version.Should().Be(1);
            actualEventEnvelope.Metadata.RetryCount.Should().Be(0);
            actualEventEnvelope.Metadata.CausationId.Should().BeNull();
        }
    }
}
