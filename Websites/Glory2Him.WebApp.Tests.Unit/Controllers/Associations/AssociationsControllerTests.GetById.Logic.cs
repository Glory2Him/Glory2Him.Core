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
using Force.DeepCloner;
using Glory2Him.Core.Models.Foundations.Associations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public async Task ShouldReturnOkWithTheAssociationOnGetByIdAsync()
        {
            // given
            Association randomAssociation = CreateRandomStoredAssociation();
            Guid inputAssociationId = randomAssociation.Id;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            Association retrievedAssociation = randomAssociation;
            Association expectedAssociation = retrievedAssociation.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedAssociation);

            var expectedActionResult =
                new ActionResult<Association>(expectedObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RetrieveAssociationByIdAsync(
                    inputAssociationId,
                    cancellationToken))
                        .ReturnsAsync(retrievedAssociation);

            // when
            ActionResult<Association> actualActionResult =
                await this.associationsController.GetAssociationByIdAsync(
                    inputAssociationId,
                    cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RetrieveAssociationByIdAsync(
                    inputAssociationId,
                    cancellationToken),
                        Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
