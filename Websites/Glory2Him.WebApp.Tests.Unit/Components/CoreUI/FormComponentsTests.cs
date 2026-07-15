// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;
using Microsoft.AspNetCore.Components;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class FormComponentsTests : BunitContext
    {
        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        [Fact]
        public void ShouldRenderFormTextLabelAndRaiseValueChanged()
        {
            // given
            string randomLabel = GetRandomString();
            string capturedValue = null;

            IRenderedComponent<FormText> renderedText =
                Render<FormText>(parameters => parameters
                    .Add(text => text.Label, randomLabel)
                    .Add(text => text.ValueChanged,
                        EventCallback.Factory.Create<string>(
                            this, value => capturedValue = value)));

            // when
            renderedText.Find("input").Input("hello");

            // then
            renderedText.Find("label").TextContent.Should().Contain(randomLabel);
            capturedValue.Should().Be("hello");
        }

        [Fact]
        public void ShouldRenderFormSelectOptionsAndRaiseValueChanged()
        {
            // given
            var options = new List<SelectOption>
            {
                new SelectOption { Value = "1", Text = "One" },
                new SelectOption { Value = "2", Text = "Two" },
            };

            string capturedValue = null;

            IRenderedComponent<FormSelect> renderedSelect =
                Render<FormSelect>(parameters => parameters
                    .Add(select => select.Options, options)
                    .Add(select => select.ValueChanged,
                        EventCallback.Factory.Create<string>(
                            this, value => capturedValue = value)));

            // when
            renderedSelect.Find("select").Change("2");

            // then
            renderedSelect.FindAll("option").Should().HaveCount(2);
            capturedValue.Should().Be("2");
        }

        [Fact]
        public void ShouldRenderFormSwitchAndRaiseValueChanged()
        {
            // given
            bool capturedValue = false;

            IRenderedComponent<FormSwitch> renderedSwitch =
                Render<FormSwitch>(parameters => parameters
                    .Add(formSwitch => formSwitch.Label, "Enabled")
                    .Add(formSwitch => formSwitch.ValueChanged,
                        EventCallback.Factory.Create<bool>(
                            this, value => capturedValue = value)));

            // when
            renderedSwitch.Find("input.form-check-input").Change(true);

            // then
            capturedValue.Should().BeTrue();
        }

        [Fact]
        public void ShouldRenderFormDateAndRaiseValueChanged()
        {
            // given
            DateTimeOffset? capturedValue = null;

            IRenderedComponent<FormDate> renderedDate =
                Render<FormDate>(parameters => parameters
                    .Add(date => date.Label, "Published")
                    .Add(date => date.ValueChanged,
                        EventCallback.Factory.Create<DateTimeOffset?>(
                            this, value => capturedValue = value)));

            // when
            renderedDate.Find("input[type=date]").Change("2022-02-18");

            // then
            capturedValue.Should().NotBeNull();
            capturedValue!.Value.Year.Should().Be(2022);
        }

        [Fact]
        public void ShouldRenderSpinnerWhenVisibleAndHideWhenNot()
        {
            // given . when
            IRenderedComponent<Spinner> visibleSpinner = Render<Spinner>();

            IRenderedComponent<Spinner> hiddenSpinner =
                Render<Spinner>(parameters =>
                    parameters.Add(spinner => spinner.Visible, false));

            // then
            visibleSpinner.FindAll("div.spinner-border").Should().HaveCount(1);
            hiddenSpinner.FindAll("div.spinner-border").Should().BeEmpty();
        }
    }
}
