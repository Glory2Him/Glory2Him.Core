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
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Glory2Him.Core.Services.Orchestrations.Associations;
using Glory2Him.WebApp.Controllers.Associations;
using Microsoft.AspNetCore.Mvc;
using RESTFulSense.Controllers;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Associations
{
    public partial class AssociationsControllerTests
    {
        [Fact]
        public void ShouldBindTheAssociationOrchestrationAlone()
        {
            // given
            Type controllerType = typeof(AssociationsController);
            string expectedRouteTemplate = "api/[controller]";

            Type[] expectedParameterTypes =
                new[] { typeof(IAssociationOrchestrationService) };

            // when
            Type[][] actualConstructorParameterTypes = controllerType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Select(constructor => constructor
                    .GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .ToArray())
                .ToArray();

            ApiControllerAttribute actualApiControllerAttribute =
                controllerType.GetCustomAttribute<ApiControllerAttribute>(inherit: true);

            RouteAttribute actualRouteAttribute =
                controllerType.GetCustomAttribute<RouteAttribute>(inherit: true);

            // then
            actualConstructorParameterTypes.Should().ContainSingle()
                .Which.Should().Equal(expectedParameterTypes);

            actualApiControllerAttribute.Should().NotBeNull();
            actualRouteAttribute.Should().NotBeNull();
            actualRouteAttribute.Template.Should().Be(expectedRouteTemplate);
            controllerType.Should().BeDerivedFrom<RESTFulController>();
        }
    }
}
