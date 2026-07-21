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

using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveTagByIdAsync()
        {
            // given
            Tag randomTag = CreateRandomTag();
            Tag storageTag = randomTag;
            Tag expectedTag = storageTag;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    randomTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            // when
            Tag actualTag =
                await this.tagService.RetrieveTagByIdAsync(
                    randomTag.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(expectedTag);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    randomTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
