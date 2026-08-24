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
using Glory2Him.Core.Models.Foundations.Reactions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Reactions
{
    public partial class ReactionsControllerTests
    {
        [Fact]
        public async Task ShouldRemoveRecordOnHardDeleteByIdsAsync()
        {
            // given
            Reaction randomReaction = CreateRandomReaction();
            Reaction storageReaction = randomReaction;
            Reaction expectedReaction = storageReaction.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedReaction);

            var expectedActionResult =
                new ActionResult<Reaction>(expectedObjectResult);

            reactionServiceMock
                .Setup(service => service.HardRemoveReactionByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageReaction);

            // when
            ActionResult<Reaction> actualActionResult =
                await reactionsController.HardDeleteReactionByIdAsync(randomReaction.Id, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            reactionServiceMock
                .Verify(service => service.HardRemoveReactionByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            reactionServiceMock.VerifyNoOtherCalls();
        }
    }
}
