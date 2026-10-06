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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Force.DeepCloner;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public async Task ShouldReturnOkWithTheSummariesAsync()
        {
            // given
            Guid[] randomContentItemIds = CreateRandomContentItemIds();
            Guid[] inputContentItemIds = randomContentItemIds;
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            List<ContentItemReactionSummary> retrievedSummaries =
                CreateRandomContentItemReactionSummaries(inputContentItemIds);

            List<ContentItemReactionSummary> expectedSummaries = retrievedSummaries.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedSummaries);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ContentItemReactionSummary>>(expectedObjectResult);

            associationOrchestrationServiceMock
                .Setup(service => service.RetrieveContentItemReactionSummariesAsync(
                    inputContentItemIds,
                    cancellationToken))
                        .ReturnsAsync(retrievedSummaries);

            // when
            ActionResult<IReadOnlyList<ContentItemReactionSummary>> actualActionResult =
                await associationsController.GetReactionSummariesAsync(
                    inputContentItemIds,
                    cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            associationOrchestrationServiceMock
                .Verify(service => service.RetrieveContentItemReactionSummariesAsync(
                    inputContentItemIds,
                    cancellationToken),
                        Times.Once);

            associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldHandTheOrchestrationEveryContentItemIdItIsGivenAsync()
        {
            // given
            Guid[] randomContentItemIds = CreateRandomContentItemIds();

            // A repeated id rides along, because the set is de-duplicated by the orchestration
            // (§ARC16.8, The set, its bounds) and never by the exposer.
            Guid[] inputContentItemIds =
                randomContentItemIds.Append(randomContentItemIds[0]).ToArray();

            // A copy, so the match below compares the ids themselves rather than the array the
            // action happened to be handed.
            Guid[] expectedContentItemIds = inputContentItemIds.DeepClone();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            List<ContentItemReactionSummary> retrievedSummaries =
                CreateRandomContentItemReactionSummaries(randomContentItemIds);

            associationOrchestrationServiceMock
                .Setup(service => service.RetrieveContentItemReactionSummariesAsync(
                    It.Is<IReadOnlyList<Guid>>(contentItemIds =>
                        contentItemIds.SequenceEqual(expectedContentItemIds)),
                    cancellationToken))
                        .ReturnsAsync(retrievedSummaries);

            // when
            await associationsController.GetReactionSummariesAsync(
                inputContentItemIds,
                cancellationToken);

            // then
            associationOrchestrationServiceMock
                .Verify(service => service.RetrieveContentItemReactionSummariesAsync(
                    It.Is<IReadOnlyList<Guid>>(contentItemIds =>
                        contentItemIds.SequenceEqual(expectedContentItemIds)),
                    cancellationToken),
                        Times.Once);

            associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
