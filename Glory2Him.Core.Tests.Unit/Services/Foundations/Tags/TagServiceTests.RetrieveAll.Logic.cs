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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllTagsAsync()
        {
            // given
            IQueryable<Tag> randomTags = CreateRandomTags();
            IQueryable<Tag> storageTags = randomTags;
            IQueryable<Tag> expectedTags = storageTags;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageTags);

            // when
            IQueryable<Tag> actualTags =
                await this.tagService.RetrieveAllTagsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualTags.Should().BeEquivalentTo(expectedTags);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllTagsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
