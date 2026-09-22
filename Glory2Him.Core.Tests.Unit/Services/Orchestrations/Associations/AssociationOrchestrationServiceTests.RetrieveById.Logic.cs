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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldReturnTheAssociationOnRetrieveByIdWhenBothEndpointsAreVisibleAsync()
        {
            // given: the by-id read resolves its two endpoints DIRECTLY rather than composing —
            // it holds one row, so there is nothing for a query composition to save.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association storedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            this.associationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storedAssociation);

            SetupEndpointByIdReads(storedAssociation, contentItemEndpoint, tagEndpoint);

            // when
            Association actualAssociation =
                await this.associationOrchestrationService.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeSameAs(storedAssociation);

            this.associationServiceMock.Verify(service =>
                service.RetrieveAssociationByIdAsync(
                    storedAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(
                    storedAssociation.EntityAKeyId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.tagServiceMock.Verify(service =>
                service.RetrieveTagByIdAsync(
                    storedAssociation.EntityBKeyId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        private void SetupEndpointByIdReads(
            Association storedAssociation,
            ContentItem contentItemEndpoint,
            Tag tagEndpoint)
        {
            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(
                    storedAssociation.EntityAKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(contentItemEndpoint);

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(
                    storedAssociation.EntityBKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(tagEndpoint);
        }
    }
}
