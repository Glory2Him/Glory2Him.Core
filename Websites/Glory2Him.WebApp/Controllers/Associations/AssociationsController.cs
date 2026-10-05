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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Services.Orchestrations.Associations;
using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Controllers.Associations
{
    public class AssociationsController : RESTFulController
    {
        private readonly IAssociationOrchestrationService associationOrchestrationService;

        public AssociationsController(IAssociationOrchestrationService associationOrchestrationService) =>
            this.associationOrchestrationService = associationOrchestrationService;

        public ValueTask<ActionResult<AssociationSuggestionResult>> PostAssociationAsync(
            [FromBody] Association association,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
