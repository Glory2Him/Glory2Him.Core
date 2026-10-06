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
using Force.DeepCloner;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public async Task ShouldReturnCreatedOnPostWhenARowWasCreatedAsync()
        {
            // given
            Association randomAssociation = CreateRandomAssociation();
            Association inputAssociation = randomAssociation;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            AssociationSuggestionResult upsertedResult =
                CreateAssociationSuggestionResult(AssociationSuggestionStatus.Created);

            AssociationSuggestionResult expectedResult = upsertedResult.DeepClone();

            var expectedObjectResult =
                new CreatedObjectResult(expectedResult);

            var expectedActionResult =
                new ActionResult<AssociationSuggestionResult>(expectedObjectResult);

            associationOrchestrationServiceMock
                .Setup(service => service.UpsertAssociationAsync(inputAssociation, cancellationToken))
                    .ReturnsAsync(upsertedResult);

            // when
            ActionResult<AssociationSuggestionResult> actualActionResult =
                await associationsController.PostAssociationAsync(inputAssociation, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            associationOrchestrationServiceMock
                .Verify(service => service.UpsertAssociationAsync(inputAssociation, cancellationToken),
                    Times.Once);

            associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(AssociationSuggestionStatus.Restored)]
        [InlineData(AssociationSuggestionStatus.Repointed)]
        [InlineData(AssociationSuggestionStatus.AlreadyApproved)]
        [InlineData(AssociationSuggestionStatus.AlreadyPending)]
        [InlineData(AssociationSuggestionStatus.OverlapsExisting)]
        public async Task ShouldReturnOkOnPostForEveryOutcomeThatCreatedNoRowAsync(
            AssociationSuggestionStatus status)
        {
            // given
            Association randomAssociation = CreateRandomAssociation();
            Association inputAssociation = randomAssociation;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            AssociationSuggestionResult upsertedResult = CreateAssociationSuggestionResult(status);
            AssociationSuggestionResult expectedResult = upsertedResult.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedResult);

            var expectedActionResult =
                new ActionResult<AssociationSuggestionResult>(expectedObjectResult);

            associationOrchestrationServiceMock
                .Setup(service => service.UpsertAssociationAsync(inputAssociation, cancellationToken))
                    .ReturnsAsync(upsertedResult);

            // when
            ActionResult<AssociationSuggestionResult> actualActionResult =
                await associationsController.PostAssociationAsync(inputAssociation, cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            associationOrchestrationServiceMock
                .Verify(service => service.UpsertAssociationAsync(inputAssociation, cancellationToken),
                    Times.Once);

            associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
