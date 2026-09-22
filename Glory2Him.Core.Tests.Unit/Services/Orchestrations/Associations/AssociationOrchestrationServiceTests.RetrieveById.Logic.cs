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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Theory]
        [InlineData(Scope.AllVersions)]
        [InlineData(Scope.ThisVersionOnly)]
        public async Task ShouldReturnTheAssociationOnRetrieveByIdWhenBothEndpointsAreVisibleAsync(
            Scope contentItemEndpointScope)
        {
            // given: the by-id read resolves its two endpoints DIRECTLY rather than composing —
            // it holds one row, so there is nothing here for a query composition to save. What it
            // does NOT vary is which row it asks about: an AllVersions endpoint is answered at its
            // group and a ThisVersionOnly endpoint at its row, exactly as the composite's terms
            // are (§SEC14.3, two resolvers and one predicate).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association storedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            storedAssociation.EntityAScope = contentItemEndpointScope;

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

            if (contentItemEndpointScope == Scope.AllVersions)
            {
                this.contentItemServiceMock.Verify(service =>
                    service.RetrieveContentItemsByGroupIdAsync(
                        storedAssociation.EntityAGroupId,
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
            else
            {
                this.contentItemServiceMock.Verify(service =>
                    service.RetrieveContentItemByIdAsync(
                        storedAssociation.EntityAKeyId,
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            // the non-versioned far end is answered at its row whatever the scope column says
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

        // Stubs whichever read the endpoint's scope makes this association ask for — the group
        // slice for an AllVersions versioned end, the row for everything else.
        private void SetupEndpointByIdReads(
            Association storedAssociation,
            ContentItem contentItemEndpoint,
            Tag tagEndpoint)
        {
            if (storedAssociation.EntityAScope == Scope.AllVersions)
            {
                this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemsByGroupIdAsync(
                        storedAssociation.EntityAGroupId,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new List<ContentItem> { contentItemEndpoint });
            }
            else
            {
                this.contentItemServiceMock.Setup(service =>
                    service.RetrieveContentItemByIdAsync(
                        storedAssociation.EntityAKeyId,
                        It.IsAny<CancellationToken>()))
                            .ReturnsAsync(contentItemEndpoint);
            }

            this.tagServiceMock.Setup(service =>
                service.RetrieveTagByIdAsync(
                    storedAssociation.EntityBKeyId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(tagEndpoint);
        }
    }
}
