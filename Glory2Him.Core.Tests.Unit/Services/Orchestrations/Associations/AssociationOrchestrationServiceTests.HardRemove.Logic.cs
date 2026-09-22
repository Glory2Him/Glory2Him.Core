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
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Associations
{
    public partial class AssociationOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveTheAssociationForAnAdministratorAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            ContentItem contentItemEndpoint = CreateEndpointContentItem();
            Tag tagEndpoint = CreateEndpointTag();

            Association hardRemovedAssociation =
                CreateStoredAssociation(contentItemEndpoint, tagEndpoint);

            this.associationServiceMock.Setup(service =>
                service.HardRemoveAssociationByIdAsync(
                    hardRemovedAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(hardRemovedAssociation);

            // when
            Association actualAssociation =
                await this.associationOrchestrationService.HardRemoveAssociationByIdAsync(
                    hardRemovedAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeSameAs(hardRemovedAssociation);

            this.associationServiceMock.Verify(service =>
                service.HardRemoveAssociationByIdAsync(
                    hardRemovedAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // no second read to duplicate the foundation's half of the gate, and no endpoint
            // resolution: this member composes nothing either
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
