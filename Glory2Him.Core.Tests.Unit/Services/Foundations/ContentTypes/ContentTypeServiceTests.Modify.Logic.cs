// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
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
        public async Task ShouldModifyContentTypeAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentType randomContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);
            ContentType inputContentType = randomContentType;
            ContentType auditAppliedContentType = inputContentType.DeepClone();
            ContentType storageContentType = auditAppliedContentType.DeepClone();
            storageContentType.UpdatedWhen = storageContentType.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            ContentType auditPreservedContentType = auditAppliedContentType.DeepClone();
            ContentType updatedContentType = auditPreservedContentType.DeepClone();
            ContentType expectedContentType = updatedContentType.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync())
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentType))
                    .ReturnsAsync(auditAppliedContentType);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    auditAppliedContentType.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedContentType,
                    storageContentType))
                        .ReturnsAsync(auditPreservedContentType);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentTypeAsync(auditPreservedContentType, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedContentType);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentTypeAsync(It.IsAny<EventEnvelope<ContentType>>(), "ContentTypeModified"))
                    .Returns(ValueTask.CompletedTask);

            // when
            ContentType actualContentType =
                await this.contentTypeService.ModifyContentTypeAsync(
                    inputContentType,
                    TestContext.Current.CancellationToken);

            // then
            actualContentType.Should().BeEquivalentTo(expectedContentType);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputContentType),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentTypeByIdAsync(
                        auditAppliedContentType.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedContentType,
                        storageContentType),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentTypeAsync(auditPreservedContentType, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentTypeAsync(It.IsAny<EventEnvelope<ContentType>>(), "ContentTypeModified"),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
