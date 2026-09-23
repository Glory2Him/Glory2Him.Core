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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Tags;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldRemoveTheAssociationWhenTheCallerPassesTheRowFreeGateAsync()
        {
            // given: the takedown reason is the caller's and is forwarded verbatim — this layer
            // neither invents one nor drops it, because dropping it would leave #318's controller
            // with no way to record why a row came down.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association removedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            string deletionReason = GetRandomString();

            this.associationServiceMock.Setup(service =>
                service.RemoveAssociationByIdAsync(
                    removedAssociation.Id,
                    deletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(removedAssociation);

            // when
            Association actualAssociation =
                await this.associationOrchestrationService.RemoveAssociationByIdAsync(
                    removedAssociation.Id,
                    deletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeSameAs(removedAssociation);

            this.associationServiceMock.Verify(service =>
                service.RemoveAssociationByIdAsync(
                    removedAssociation.Id,
                    deletionReason,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // no second read to duplicate the foundation's half of the gate, and no endpoint
            // resolution: this member composes nothing
            this.associationServiceMock.Verify(service =>
                service.RetrieveAssociationByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.associationServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.tagServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
