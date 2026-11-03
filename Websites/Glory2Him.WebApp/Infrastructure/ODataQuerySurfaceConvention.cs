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
                if (filters[index] is EnableQueryAttribute)
                {
                    filters[index] = new NarrowedEnableQueryAttribute();
                }
            }
        }
    }
}
