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
    public class ConfirmDialogComponentTests : BunitContext
    {
        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        [Fact]
        public void ShouldRenderMessageAndActionsWhenVisible()
        {
            // given
            string randomMessage = GetRandomString();

            // when
            IRenderedComponent<ConfirmDialog> renderedDialog =
                Render<ConfirmDialog>(parameters => parameters
                    .Add(dialog => dialog.Visible, true)
                    .Add(dialog => dialog.Message, randomMessage));

            // then
            renderedDialog.Markup.Should().Contain(randomMessage);
            renderedDialog.Markup.Should().Contain("OK");
            renderedDialog.Markup.Should().Contain("Cancel");
        }

        [Fact]
        public void ShouldInvokeOnConfirmWhenConfirmClicked()
        {
            // given
            bool wasConfirmed = false;

            IRenderedComponent<ConfirmDialog> renderedDialog =
                Render<ConfirmDialog>(parameters => parameters
                    .Add(dialog => dialog.Visible, true)
                    .Add(dialog => dialog.OnConfirm,
                        EventCallback.Factory.Create(this, () => wasConfirmed = true)));

            // when (the confirm button carries the danger colour by default)
            renderedDialog.Find("button.btn-danger").Click();

            // then
            wasConfirmed.Should().BeTrue();
        }

        [Fact]
        public void ShouldInvokeOnCancelWhenCancelClicked()
        {
            // given
            bool wasCancelled = false;

            IRenderedComponent<ConfirmDialog> renderedDialog =
                Render<ConfirmDialog>(parameters => parameters
                    .Add(dialog => dialog.Visible, true)
                    .Add(dialog => dialog.OnCancel,
                        EventCallback.Factory.Create(this, () => wasCancelled = true)));

            // when
            renderedDialog.Find("button.btn-secondary").Click();

            // then
            wasCancelled.Should().BeTrue();
        }
    }
}
