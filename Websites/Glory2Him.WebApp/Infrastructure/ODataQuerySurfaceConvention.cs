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

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Query;

namespace Glory2Him.WebApp.Infrastructure
{
    /// <summary>
    /// Replaces every <c>[EnableQuery]</c> in a controller's or an action's filters with a
    /// <see cref="NarrowedEnableQueryAttribute"/> carrying the same public settings, so every queryable
    /// read accepts only the query surface that attribute allows (GHSA-3m43-v266-m6qf). An
    /// exposer keeps writing a bare <c>[EnableQuery]</c>; the host decides what it accepts.
    ///
    /// <para><b>How it takes effect.</b> <see cref="EnableQueryAttribute"/> is an MVC filter,
    /// and MVC builds each action's filter descriptors from the model's filter lists after the
    /// conventions run. Replacing the instance in a controller's or an action's
    /// <c>Filters</c> is therefore the same as having written the narrowed attribute there. The
    /// original stays in the model's <c>Attributes</c>, which are read-only metadata and never run
    /// as a filter.</para>
    ///
    /// <para><b>It narrows and never widens what an attribute's public settings allow.</b> The
    /// replacement carries every public setting of the attribute it replaces, so whatever those
    /// settings allowed or refused, the replacement allows or refuses the same, and the narrowed
    /// attribute's own rule only refuses more.
    /// <see cref="EnableQueryAttribute.PageSize"/>, <see cref="EnableQueryAttribute.MaxTop"/> and
    /// <see cref="EnableQueryAttribute.MaxSkip"/> read an unset value back as <c>0</c>, and a
    /// <c>0</c> written through their setters is refused (the page size) or refuses every positive
    /// <c>$top</c> and <c>$skip</c> (the two limits). So each is copied only when it is set, and an
    /// unset one stays unset. An attribute that set <c>MaxTop</c> or <c>MaxSkip</c> to <c>0</c>
    /// itself cannot be told apart from an unset one through those settings, so its replacement
    /// leaves that limit unset; an exposer refuses <c>$top</c> or <c>$skip</c> by leaving it out
    /// of <see cref="EnableQueryAttribute.AllowedQueryOptions"/>, which is carried. A subclass of
    /// <see cref="EnableQueryAttribute"/> is replaced by a plain
    /// <see cref="NarrowedEnableQueryAttribute"/> with its public settings, and loses what the
    /// subclass adds. No read on the host has either shape.</para>
    ///
    /// <para><b>Beside <see cref="ODataPageSizeConvention"/>.</b> The host's page size reaches
    /// the replacement whichever of the two runs first: run after it, the replacement copies the
    /// size it wrote; run before it, the replacement is the <see cref="EnableQueryAttribute"/> it
    /// then fills.</para>
    ///
    /// <para><b>What it does not reach.</b> It visits a controller's and an action's filters
    /// only. An <see cref="EnableQueryAttribute"/> registered globally through
    /// <c>MvcOptions.Filters</c> sits in the application model's own filters, which it does not
    /// visit, so that attribute would not be narrowed; the host-level check that every query
    /// attribute is narrowed fails on one. Any other way of applying a caller's query options is
    /// outside both: a filter factory that creates the attribute, a filter provider, an action or
    /// a minimal-API endpoint taking <see cref="ODataQueryOptions"/>, or OData's endpoint filter.
    /// The host has none, and adding one is a design change made under GHSA-3m43-v266-m6qf.</para>
    /// </summary>
    public sealed class ODataQuerySurfaceConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (ControllerModel controller in application.Controllers)
            {
                NarrowEnableQueryAttributes(controller.Filters);

                foreach (ActionModel action in controller.Actions)
                {
                    NarrowEnableQueryAttributes(action.Filters);
                }
            }
        }

        private static void NarrowEnableQueryAttributes(IList<IFilterMetadata> filters)
        {
            for (int index = 0; index < filters.Count; index++)
            {
                if (filters[index] is EnableQueryAttribute enableQueryAttribute)
                {
                    filters[index] = CreateNarrowedEnableQueryAttribute(enableQueryAttribute);
                }
            }
        }

        private static NarrowedEnableQueryAttribute CreateNarrowedEnableQueryAttribute(
            EnableQueryAttribute enableQueryAttribute)
        {
            var narrowedEnableQueryAttribute = new NarrowedEnableQueryAttribute
            {
                AllowedArithmeticOperators = enableQueryAttribute.AllowedArithmeticOperators,
                AllowedFunctions = enableQueryAttribute.AllowedFunctions,
                AllowedLogicalOperators = enableQueryAttribute.AllowedLogicalOperators,
                AllowedOrderByProperties = enableQueryAttribute.AllowedOrderByProperties,
                AllowedQueryOptions = enableQueryAttribute.AllowedQueryOptions,
                EnableConstantParameterization = enableQueryAttribute.EnableConstantParameterization,
                EnableCorrelatedSubqueryBuffering = enableQueryAttribute.EnableCorrelatedSubqueryBuffering,
                EnsureStableOrdering = enableQueryAttribute.EnsureStableOrdering,
                HandleNullPropagation = enableQueryAttribute.HandleNullPropagation,
                HandleReferenceNavigationPropertyExpandFilter =
                    enableQueryAttribute.HandleReferenceNavigationPropertyExpandFilter,
                MaxAnyAllExpressionDepth = enableQueryAttribute.MaxAnyAllExpressionDepth,
                MaxExpansionDepth = enableQueryAttribute.MaxExpansionDepth,
                MaxFunctionCallDepth = enableQueryAttribute.MaxFunctionCallDepth,
                MaxNodeCount = enableQueryAttribute.MaxNodeCount,
                MaxOrderByNodeCount = enableQueryAttribute.MaxOrderByNodeCount,
                Order = enableQueryAttribute.Order
            };

            if (enableQueryAttribute.MaxSkip != 0)
            {
                narrowedEnableQueryAttribute.MaxSkip = enableQueryAttribute.MaxSkip;
            }

            if (enableQueryAttribute.MaxTop != 0)
            {
                narrowedEnableQueryAttribute.MaxTop = enableQueryAttribute.MaxTop;
            }

            if (enableQueryAttribute.PageSize != 0)
            {
                narrowedEnableQueryAttribute.PageSize = enableQueryAttribute.PageSize;
            }

            return narrowedEnableQueryAttribute;
        }
    }
}
