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

using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;
using Microsoft.AspNetCore.Components;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class ModalComponentTests : BunitContext
    {
        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        [Fact]
        public void ShouldRenderNothingWhenNotVisible()
        {
            // given . when
            IRenderedComponent<Modal> renderedModal =
                Render<Modal>(parameters =>
                    parameters.Add(modal => modal.Visible, false));

            // then
            renderedModal.FindAll("div.modal").Should().BeEmpty();
        }

        [Fact]
        public void ShouldRenderTitleAndBodyWhenVisible()
        {
            // given
            string randomTitle = GetRandomString();
            string randomBody = GetRandomString();

            // when
            IRenderedComponent<Modal> renderedModal =
                Render<Modal>(parameters => parameters
                    .Add(modal => modal.Visible, true)
                    .Add(modal => modal.Title, randomTitle)
                    .AddChildContent(randomBody));

            // then
            renderedModal.Find("div.modal-title").TextContent.Should().Contain(randomTitle);
            renderedModal.Find("div.modal-body").TextContent.Should().Contain(randomBody);
            renderedModal.FindAll("div.modal-backdrop").Should().HaveCount(1);
        }

        [Fact]
        public void ShouldApplySizeClass()
        {
            // given . when
            IRenderedComponent<Modal> renderedModal =
                Render<Modal>(parameters => parameters
                    .Add(modal => modal.Visible, true)
                    .Add(modal => modal.Size, "lg"));

            // then
            renderedModal.Find("div.modal-dialog").ClassList.Should().Contain("modal-lg");
        }

        [Fact]
        public void ShouldInvokeOnCloseWhenCloseClicked()
        {
            // given
            bool wasClosed = false;

            IRenderedComponent<Modal> renderedModal =
                Render<Modal>(parameters => parameters
                    .Add(modal => modal.Visible, true)
                    .Add(modal => modal.OnClose,
                        EventCallback.Factory.Create(this, () => wasClosed = true)));

            // when
            renderedModal.Find("button.btn-close").Click();

            // then
            wasClosed.Should().BeTrue();
        }
    }
}
