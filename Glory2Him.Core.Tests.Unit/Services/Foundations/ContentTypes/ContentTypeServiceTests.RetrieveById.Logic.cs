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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveContentTypeByIdAsync()
        {
            // given
            ContentType randomContentType = CreateRandomContentType();
            ContentType storageContentType = randomContentType;
            storageContentType.IsDeleted = false;
            storageContentType.ApprovalStatus = ApprovalStatus.Approved;
            storageContentType.IsPublished = true;
            storageContentType.PublishDate = null;
            ContentType expectedContentType = storageContentType;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentType);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            ContentType actualContentType =
                await this.contentTypeService.RetrieveContentTypeByIdAsync(
                    randomContentType.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentType.Should().BeEquivalentTo(expectedContentType);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveNonPublicContentTypeByIdWhenUserIsAdminAsync()
        {
            // given: only an Admin may read a non-public content type — reference data
            // has no owner branch
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ContentType randomContentType = CreateRandomContentType();
            ContentType storageContentType = randomContentType;
            storageContentType.IsDeleted = false;
            storageContentType.ApprovalStatus = ApprovalStatus.Draft;
            storageContentType.IsPublished = false;
            ContentType expectedContentType = storageContentType;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentType);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            ContentType actualContentType =
                await this.contentTypeService.RetrieveContentTypeByIdAsync(
                    randomContentType.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentType.Should().BeEquivalentTo(expectedContentType);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
