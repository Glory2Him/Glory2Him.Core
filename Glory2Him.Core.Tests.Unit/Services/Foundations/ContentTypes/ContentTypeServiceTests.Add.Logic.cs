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
        public async Task ShouldAddContentTypeAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentType randomContentType = CreateContentTypeFiller(randomDateTimeOffset).Create();
            ContentType inputContentType = randomContentType;
            ContentType auditAppliedContentType = inputContentType.DeepClone();
            ContentType storageContentType = auditAppliedContentType.DeepClone();
            ContentType expectedContentType = storageContentType.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentType.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertContentTypeAsync(auditAppliedContentType, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentType);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentTypeAsync(It.IsAny<EventEnvelope<ContentType>>(), ContentTypeEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<ContentType>>(
                        new EventPublishResult<ContentType>()));

            // when
            ContentType actualContentType =
                await this.contentTypeService.AddContentTypeAsync(
                    inputContentType,
                    TestContext.Current.CancellationToken);

            // then
            actualContentType.Should().BeEquivalentTo(expectedContentType);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertContentTypeAsync(auditAppliedContentType, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentTypeAsync(
                    It.IsAny<EventEnvelope<ContentType>>(),
                    ContentTypeEventOperation.Added),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
