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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Associations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        /// <summary>
        /// Criterion 1. The body is the orchestration's queryable itself, so <c>[EnableQuery]</c>
        /// composes over it and its options run in SQL with everything beneath it
        /// (<c>AssociationsController.AssociationReads.md</c> §1 rule 1). The queryable is a
        /// strict mock: an action that enumerated it, counted it or composed over it would touch a
        /// member and throw, and one that copied it into another collection would not hand back
        /// the same instance.
        /// </summary>
        [Fact]
        public async Task ShouldReturnOkWithTheAssociationsAsync()
        {
            // given
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            var retrievedAssociationsMock =
                new Mock<IQueryable<Association>>(MockBehavior.Strict);

            IQueryable<Association> retrievedAssociations = retrievedAssociationsMock.Object;
            IQueryable<Association> expectedAssociations = retrievedAssociations;

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RetrieveAllAssociationsAsync(cancellationToken))
                    .ReturnsAsync(retrievedAssociations);

            // when
            ActionResult<IQueryable<Association>> actualActionResult =
                await this.associationsController.Get(cancellationToken);

            // then
            OkObjectResult actualOkObjectResult =
                actualActionResult.Result.Should().BeOfType<OkObjectResult>().Subject;

            actualOkObjectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
            actualOkObjectResult.Value.Should().BeSameAs(expectedAssociations);
            retrievedAssociationsMock.VerifyNoOtherCalls();

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RetrieveAllAssociationsAsync(cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
