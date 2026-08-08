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
        public async Task ShouldCreateNextEventEnvelopeAsync()
        {
            // given
            string rootContent = GetRandomString();
            string nextContent = GetRandomString();

            EventEnvelope<string> rootEventEnvelope =
                await this.eventEnvelopeClient.CreateAsync(rootContent, TestContext.Current.CancellationToken);

            // when
            EventEnvelope<string> actualEventEnvelope =
                await this.eventEnvelopeClient.CreateNextAsync(rootEventEnvelope, nextContent, TestContext.Current.CancellationToken);

            // then
            actualEventEnvelope.Content.Should().Be(nextContent);
            actualEventEnvelope.SecurityContext.Should().BeEquivalentTo(rootEventEnvelope.SecurityContext);
            actualEventEnvelope.RequestContext.Should().BeEquivalentTo(rootEventEnvelope.RequestContext);
            actualEventEnvelope.Metadata.EventId.Should().NotBe(Guid.Empty);
            actualEventEnvelope.Metadata.EventId.Should().NotBe(rootEventEnvelope.Metadata.EventId);
            actualEventEnvelope.Metadata.EventType.Should().Be(nameof(String));
            actualEventEnvelope.Metadata.Version.Should().Be(1);
            actualEventEnvelope.Metadata.RetryCount.Should().Be(0);

            actualEventEnvelope.Metadata.CausationId.Should().Be(
                rootEventEnvelope.Metadata.EventId.ToString());

            actualEventEnvelope.Metadata.ParentCorrelationId.Should().Be(
                rootEventEnvelope.Metadata.ParentCorrelationId);
        }
    }
}
