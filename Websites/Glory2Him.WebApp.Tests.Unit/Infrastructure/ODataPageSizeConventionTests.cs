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
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Configuration;
using Glory2Him.WebApp.Infrastructure;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    public class ODataPageSizeConventionTests
    {
        [Fact]
        public void ShouldPageAnActionThatNamesNoPageSize()
        {
            // given
            var enableQueryAttribute = new EnableQueryAttribute();

            ApplicationModel applicationModel =
                CreateApplicationModelWithActionFilter(enableQueryAttribute);

            var oDataPageSizeConvention = new ODataPageSizeConvention(pageSize: 50);

            // when
            oDataPageSizeConvention.Apply(applicationModel);

            // then
            enableQueryAttribute.PageSize.Should().Be(50);
        }

        // The attribute keeps whatever it was given: this convention supplies the host default,
        // it does not overrule an exposer that had a reason to page differently.
        [Fact]
        public void ShouldLeaveAnActionThatNamesItsOwnPageSize()
        {
            // given
            var enableQueryAttribute = new EnableQueryAttribute { PageSize = 7 };

            ApplicationModel applicationModel =
                CreateApplicationModelWithActionFilter(enableQueryAttribute);

            var oDataPageSizeConvention = new ODataPageSizeConvention(pageSize: 50);

            // when
            oDataPageSizeConvention.Apply(applicationModel);

            // then
            enableQueryAttribute.PageSize.Should().Be(7);
        }

        // [EnableQuery] is legal on the controller too, where it covers every action at once.
        [Fact]
        public void ShouldPageAControllerThatNamesNoPageSize()
        {
            // given
            var enableQueryAttribute = new EnableQueryAttribute();

            ApplicationModel applicationModel =
                CreateApplicationModelWithControllerFilter(enableQueryAttribute);

            var oDataPageSizeConvention = new ODataPageSizeConvention(pageSize: 50);

            // when
            oDataPageSizeConvention.Apply(applicationModel);

            // then
            enableQueryAttribute.PageSize.Should().Be(50);
        }

        // EnableQueryAttribute.PageSize refuses anything below 1, so "no paging" cannot be
        // assigned — only left alone, which is what a non-positive setting asks for.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ShouldLeavePagingOffWhenTheConfiguredPageSizeIsNotPositive(int pageSize)
        {
            // given
            var enableQueryAttribute = new EnableQueryAttribute();

            ApplicationModel applicationModel =
                CreateApplicationModelWithActionFilter(enableQueryAttribute);

            var oDataPageSizeConvention = new ODataPageSizeConvention(pageSize);

            // when
            oDataPageSizeConvention.Apply(applicationModel);

            // then
            enableQueryAttribute.PageSize.Should().Be(0);
        }

        [Fact]
        public void ShouldReadThePageSizeFromConfiguration()
        {
            // given
            IConfiguration configuration = BuildConfiguration(
                (ODataPageSizeConvention.ConfigurationKey, "5000"));

            var enableQueryAttribute = new EnableQueryAttribute();

            ApplicationModel applicationModel =
                CreateApplicationModelWithActionFilter(enableQueryAttribute);

            // when
            ODataPageSizeConvention oDataPageSizeConvention =
                ODataPageSizeConvention.FromConfiguration(configuration);

            oDataPageSizeConvention.Apply(applicationModel);

            // then
            enableQueryAttribute.PageSize.Should().Be(5000);
        }

        // A host that was never configured must still page rather than serve whole tables.
        [Fact]
        public void ShouldFallBackToTheDefaultPageSizeWhenConfigurationNamesNone()
        {
            // given
            IConfiguration configuration = BuildConfiguration();

            var enableQueryAttribute = new EnableQueryAttribute();

            ApplicationModel applicationModel =
                CreateApplicationModelWithActionFilter(enableQueryAttribute);

            // when
            ODataPageSizeConvention oDataPageSizeConvention =
                ODataPageSizeConvention.FromConfiguration(configuration);

            oDataPageSizeConvention.Apply(applicationModel);

            // then
            enableQueryAttribute.PageSize.Should()
                .Be(ODataPageSizeConvention.FallbackPageSize);
        }

        private static IConfiguration BuildConfiguration(
            params (string Key, string Value)[] entries)
        {
            var settings = new Dictionary<string, string>();

            foreach ((string key, string value) in entries)
            {
                settings[key] = value;
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        private static ApplicationModel CreateApplicationModelWithActionFilter(
            EnableQueryAttribute enableQueryAttribute)
        {
            ApplicationModel applicationModel = CreateApplicationModel();

            MethodInfo actionMethod =
                typeof(SampleController).GetMethod(nameof(SampleController.Get));

            var actionModel = new ActionModel(
                actionMethod,
                new List<object> { enableQueryAttribute });

            actionModel.Filters.Add(enableQueryAttribute);
            applicationModel.Controllers[0].Actions.Add(actionModel);

            return applicationModel;
        }

        private static ApplicationModel CreateApplicationModelWithControllerFilter(
            EnableQueryAttribute enableQueryAttribute)
        {
            ApplicationModel applicationModel = CreateApplicationModel();
            applicationModel.Controllers[0].Filters.Add(enableQueryAttribute);

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
