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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Glory2Him.WebApp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Query;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    public class ODataQuerySurfaceConventionTests
    {
        [Fact]
        public void ShouldNarrowAnActionsBareEnableQuery()
        {
            // given
            var enableQueryAttribute = new EnableQueryAttribute();

            ApplicationModel applicationModel =
                CreateApplicationModelWithActionFilter(enableQueryAttribute);

            var oDataQuerySurfaceConvention = new ODataQuerySurfaceConvention();

            // when
            oDataQuerySurfaceConvention.Apply(applicationModel);

            // then
            GetActionFilters(applicationModel).OfType<EnableQueryAttribute>()
                .Should().ContainSingle()
                    .Which.Should().BeOfType<NarrowedEnableQueryAttribute>();
        }

        private static IList<IFilterMetadata> GetActionFilters(ApplicationModel applicationModel) =>
            applicationModel.Controllers[0].Actions[0].Filters;

        private static ApplicationModel CreateApplicationModelWithActionFilter(
            IFilterMetadata filter)
        {
            ApplicationModel applicationModel = CreateApplicationModel();

            MethodInfo actionMethod =
                typeof(SampleController).GetMethod(nameof(SampleController.Get));

            var actionModel = new ActionModel(
                actionMethod,
                new List<object> { filter });

            actionModel.Filters.Add(filter);
            applicationModel.Controllers[0].Actions.Add(actionModel);

            return applicationModel;
        }

        private static ApplicationModel CreateApplicationModel()
        {
            var controllerModel = new ControllerModel(
                typeof(SampleController).GetTypeInfo(),
                new List<object>());

            var applicationModel = new ApplicationModel();
            applicationModel.Controllers.Add(controllerModel);

            return applicationModel;
        }

        // Stands in for a real exposer: the convention only ever reads the filters hanging off
        // the model, so the action's body and route are beside the point.
        private class SampleController : ControllerBase
        {
            public IActionResult Get() => Ok();
        }
    }
}
