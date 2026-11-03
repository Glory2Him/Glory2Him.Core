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

        // [EnableQuery] is legal on the controller too, where it covers every action at once.
        [Fact]
        public void ShouldNarrowAControllersBareEnableQuery()
        {
            // given
            var enableQueryAttribute = new EnableQueryAttribute();

            ApplicationModel applicationModel =
                CreateApplicationModelWithControllerFilter(enableQueryAttribute);

            var oDataQuerySurfaceConvention = new ODataQuerySurfaceConvention();

            // when
            oDataQuerySurfaceConvention.Apply(applicationModel);

            // then
            applicationModel.Controllers[0].Filters.OfType<EnableQueryAttribute>()
                .Should().ContainSingle()
                    .Which.Should().BeOfType<NarrowedEnableQueryAttribute>();
        }

        // The replacement narrows and never widens: whatever the replaced attribute allowed or
        // refused through its own settings, the replacement allows or refuses the same.
        [Fact]
        public void ShouldCarryEverySettingOfTheReplacedEnableQuery()
        {
            // given
            var enableQueryAttribute = new EnableQueryAttribute();

            foreach (KeyValuePair<string, object> setting in nonDefaultSettings)
            {
                typeof(EnableQueryAttribute).GetProperty(setting.Key)
                    .SetValue(enableQueryAttribute, setting.Value);
            }

            ApplicationModel applicationModel =
                CreateApplicationModelWithActionFilter(enableQueryAttribute);

            var oDataQuerySurfaceConvention = new ODataQuerySurfaceConvention();

            // when
            oDataQuerySurfaceConvention.Apply(applicationModel);

            // then
            EnableQueryAttribute replacement =
                GetActionFilters(applicationModel).OfType<EnableQueryAttribute>().Single();

            Dictionary<string, object> carriedSettings = nonDefaultSettings.Keys.ToDictionary(
                settingName => settingName,
                settingName => typeof(EnableQueryAttribute).GetProperty(settingName)
                    .GetValue(replacement));

            carriedSettings.Should().BeEquivalentTo(nonDefaultSettings);
        }

        // Every public settable setting has a value above, and it is not the default, so a
        // setting the convention dropped would read back differently. A setting a later OData
        // version adds fails here until it is given one.
        [Fact]
        public void ShouldHoldANonDefaultValueForEverySettableSetting()
        {
            // given
            var bareEnableQueryAttribute = new EnableQueryAttribute();

            // when
            string[] settableSettingNames = typeof(EnableQueryAttribute)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.SetMethod?.IsPublic is true)
                        .Select(property => property.Name)
                            .ToArray();

            string[] settingsHoldingTheirDefault = nonDefaultSettings
                .Where(setting => Equals(
                    typeof(EnableQueryAttribute).GetProperty(setting.Key)
                        .GetValue(bareEnableQueryAttribute),
                    setting.Value))
                .Select(setting => setting.Key)
                .ToArray();

            // then
            nonDefaultSettings.Keys.Should().BeEquivalentTo(settableSettingNames);
            settingsHoldingTheirDefault.Should().BeEmpty();
        }

        // The host registers both conventions, and the page size must reach the replacement
        // whichever runs first. 25 is not the page size convention's fallback, so a replacement
        // that took a size from anywhere else would read differently.
        [Fact]
        public void ShouldHoldTheHostsPageSizeWhenAppliedAfterThePageSizeConvention()
        {
            // given
            var enableQueryAttribute = new EnableQueryAttribute();

            ApplicationModel applicationModel =
                CreateApplicationModelWithActionFilter(enableQueryAttribute);

            var oDataPageSizeConvention = new ODataPageSizeConvention(pageSize: HostPageSize);
            var oDataQuerySurfaceConvention = new ODataQuerySurfaceConvention();

            // when
            oDataPageSizeConvention.Apply(applicationModel);
            oDataQuerySurfaceConvention.Apply(applicationModel);

            // then
            GetActionFilters(applicationModel).OfType<NarrowedEnableQueryAttribute>()
                .Should().ContainSingle()
                    .Which.PageSize.Should().Be(HostPageSize);
        }

        [Fact]
        public void ShouldHoldTheHostsPageSizeWhenAppliedBeforeThePageSizeConvention()
        {
            // given
            var enableQueryAttribute = new EnableQueryAttribute();

            ApplicationModel applicationModel =
                CreateApplicationModelWithActionFilter(enableQueryAttribute);

            var oDataQuerySurfaceConvention = new ODataQuerySurfaceConvention();
            var oDataPageSizeConvention = new ODataPageSizeConvention(pageSize: HostPageSize);

            // when
            oDataQuerySurfaceConvention.Apply(applicationModel);
            oDataPageSizeConvention.Apply(applicationModel);

            // then
            GetActionFilters(applicationModel).OfType<NarrowedEnableQueryAttribute>()
                .Should().ContainSingle()
                    .Which.PageSize.Should().Be(HostPageSize);
        }

        private const int HostPageSize = 25;

        private static readonly Dictionary<string, object> nonDefaultSettings = new()
        {
            [nameof(EnableQueryAttribute.AllowedArithmeticOperators)] = AllowedArithmeticOperators.Add,
            [nameof(EnableQueryAttribute.AllowedFunctions)] = AllowedFunctions.Contains,
            [nameof(EnableQueryAttribute.AllowedLogicalOperators)] = AllowedLogicalOperators.Equal,
            [nameof(EnableQueryAttribute.AllowedOrderByProperties)] = "name,version",
            [nameof(EnableQueryAttribute.AllowedQueryOptions)] =
                AllowedQueryOptions.Filter | AllowedQueryOptions.Top,
            [nameof(EnableQueryAttribute.EnableConstantParameterization)] = false,
            [nameof(EnableQueryAttribute.EnableCorrelatedSubqueryBuffering)] = true,
            [nameof(EnableQueryAttribute.EnsureStableOrdering)] = false,
            [nameof(EnableQueryAttribute.HandleNullPropagation)] = HandleNullPropagationOption.False,
            [nameof(EnableQueryAttribute.HandleReferenceNavigationPropertyExpandFilter)] = true,
            [nameof(EnableQueryAttribute.MaxAnyAllExpressionDepth)] = 3,
            [nameof(EnableQueryAttribute.MaxExpansionDepth)] = 4,
            [nameof(EnableQueryAttribute.MaxFunctionCallDepth)] = 9,
            [nameof(EnableQueryAttribute.MaxNodeCount)] = 42,
            [nameof(EnableQueryAttribute.MaxOrderByNodeCount)] = 6,
            [nameof(EnableQueryAttribute.MaxSkip)] = 11,
            [nameof(EnableQueryAttribute.MaxTop)] = 12,
            [nameof(EnableQueryAttribute.Order)] = 13,
            [nameof(EnableQueryAttribute.PageSize)] = 14
        };

        private static IList<IFilterMetadata> GetActionFilters(ApplicationModel applicationModel) =>
            applicationModel.Controllers[0].Actions[0].Filters;

        private static ApplicationModel CreateApplicationModelWithControllerFilter(
            IFilterMetadata filter)
        {
            ApplicationModel applicationModel = CreateApplicationModel();
            applicationModel.Controllers[0].Filters.Add(filter);

            return applicationModel;
        }

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
