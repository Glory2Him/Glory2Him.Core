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
using Glory2Him.WebApp.Controllers.ContentItems;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OData.Query;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItems
{
    /// <summary>
    /// Criterion 6, asserted on the action's METADATA. The two halves below are the shape of
    /// the feed's query surface, and each is invisible to every behavioural test in the suite:
    /// a route that grew an <c>[EnableQuery]</c> would keep answering, and a parameter that
    /// grew a <c>[BindRequired]</c> would keep answering for every caller who named a page.
    /// </summary>
    public partial class ContentItemsControllerTests
    {
        /// <summary>
        /// The page is an ARGUMENT of the read, composed into the same SQL as the predicate and
        /// the order — so <see cref="EnableQueryAttribute"/> would apply <c>$skip</c> a SECOND
        /// time to an already-paged list, and <c>EnsureStableOrdering</c> would re-key the
        /// answer over the order §DOM11.3 asked for. The two are mutually exclusive on one
        /// route, and this is the one that goes.
        /// </summary>
        [Fact]
        public void GetFeedShouldCarryNoEnableQueryAttribute()
        {
            // Given
            MethodInfo methodInfo = typeof(ContentItemsController)
                .GetMethod(nameof(ContentItemsController.GetContentItemFeed));

            // When
            bool hasEnableQuery = methodInfo
                .GetCustomAttributes(typeof(EnableQueryAttribute), inherit: true)
                .Any();

            // Then
            hasEnableQuery.Should().BeFalse();
        }

        /// <summary>
        /// The mirror of <c>ApprovalsControllerTests.PostDecisionShouldRequireTheDecisionToBeBound</c>,
        /// asserted the other way round. <c>Program.cs</c> reserves <c>[BindRequired]</c> for a
        /// parameter that must be present to ADDRESS the operation, and every shipped use of it
        /// in these controllers is that case. <c>GET api/ContentItems/Feed</c> with no query
        /// string is a completely addressed request — the front page of the feed — so binding
        /// must not refuse it.
        ///
        /// <para><b>The nullability is load-bearing rather than stylistic.</b> A non-nullable
        /// <c>int take</c> binds an absent parameter to <c>0</c>, and <c>take = 0</c> is a
        /// validation failure — which would collapse "asked for nothing" into "said nothing"
        /// and turn the bare URL into a 400.</para>
        /// </summary>
        [Theory]
        [InlineData("skip")]
        [InlineData("take")]
        public void GetFeedShouldTakeThePageAsAnOptionalNullableParameter(string parameterName)
        {
            // Given
            MethodInfo methodInfo = typeof(ContentItemsController)
                .GetMethod(nameof(ContentItemsController.GetContentItemFeed));

            ParameterInfo pageParameter = methodInfo
                .GetParameters()
                .Single(parameter => parameter.Name == parameterName);

            // When
            bool isBindRequired = pageParameter
                .GetCustomAttributes(typeof(BindRequiredAttribute), inherit: true)
                .Any();

            // Then
            isBindRequired.Should().BeFalse();
            pageParameter.ParameterType.Should().Be(typeof(int?));
        }
    }
}
