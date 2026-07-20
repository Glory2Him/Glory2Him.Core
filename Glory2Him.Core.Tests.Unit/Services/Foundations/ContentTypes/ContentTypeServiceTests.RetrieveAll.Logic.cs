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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllContentTypesAsync()
        {
            // given
            IQueryable<ContentType> randomContentTypes = CreateRandomContentTypes();
            IQueryable<ContentType> storageContentTypes = randomContentTypes;
            IQueryable<ContentType> expectedContentTypes = storageContentTypes;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentTypesAsync())
                    .ReturnsAsync(storageContentTypes);

            // when
            IQueryable<ContentType> actualContentTypes =
                await this.contentTypeService.RetrieveAllContentTypesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentTypes.Should().BeEquivalentTo(expectedContentTypes);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentTypesAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
