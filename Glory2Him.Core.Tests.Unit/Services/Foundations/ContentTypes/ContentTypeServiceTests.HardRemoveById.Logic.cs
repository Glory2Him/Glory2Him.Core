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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveContentTypeByIdAsync()
        {
            // given
            ContentType randomContentType = CreateRandomContentType();
            ContentType storageContentType = randomContentType;
            ContentType expectedContentType = storageContentType.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentType);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteContentTypeAsync(storageContentType, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentType);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentTypeAsync(It.IsAny<EventEnvelope<ContentType>>(), ContentTypeEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ContentType>>(
                        new EventPublishResult<ContentType>()));

            // when
            ContentType actualContentType =
                await this.contentTypeService.HardRemoveContentTypeByIdAsync(
                    randomContentType.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentType.Should().BeEquivalentTo(expectedContentType);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentTypeAsync(storageContentType, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentTypeAsync(It.IsAny<EventEnvelope<ContentType>>(), ContentTypeEventOperation.Removed),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
