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
using Glory2Him.Core.Models.Orchestrations.Associations;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public async Task ShouldReturnNoContentOnDeletePairWhenTheReactionWasRemovedAsync()
        {
            // given
            Association randomAssociation = CreateRandomAssociation();
            Association inputAssociation = randomAssociation;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            AssociationRemovalResult removedResult =
                CreateAssociationRemovalResult(AssociationRemovalStatus.Removed);

            associationOrchestrationServiceMock
                .Setup(service => service.RemoveAssociationByPairAsync(inputAssociation, cancellationToken))
                    .ReturnsAsync(removedResult);

            // when
            ActionResult actualActionResult =
                await associationsController.DeleteAssociationPairAsync(inputAssociation, cancellationToken);

            // then
            actualActionResult.Should().BeOfType<NoContentResult>();

            associationOrchestrationServiceMock
                .Verify(service => service.RemoveAssociationByPairAsync(inputAssociation, cancellationToken),
                    Times.Once);

            associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
