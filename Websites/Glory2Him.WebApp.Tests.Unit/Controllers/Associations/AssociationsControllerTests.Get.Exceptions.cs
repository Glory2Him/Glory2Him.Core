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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public async Task ShouldReturnFailedDependencyOnGetIfDependencyErrorOccurredAsync()
        {
            // given
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            var someInnerException = new Xeption();

            var associationOrchestrationDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: GetRandomString(),
                    innerException: someInnerException);

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(someInnerException);

            var expectedActionResult =
                new ActionResult<IQueryable<Association>>(expectedFailedDependencyObjectResult);

            this.associationOrchestrationServiceMock.Setup(service =>
                service.RetrieveAllAssociationsAsync(cancellationToken))
                    .ThrowsAsync(associationOrchestrationDependencyException);

            // when
            ActionResult<IQueryable<Association>> actualActionResult =
                await this.associationsController.Get(cancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.associationOrchestrationServiceMock.Verify(service =>
                service.RetrieveAllAssociationsAsync(cancellationToken),
                    Times.Once);

            this.associationOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
