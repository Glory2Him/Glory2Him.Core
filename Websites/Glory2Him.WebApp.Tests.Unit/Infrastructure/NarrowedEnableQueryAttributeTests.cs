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

using Glory2Him.WebApp.Tests.Unit.Infrastructure.QueryModels;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    /// <summary>
    /// Each test validates its query through <see cref="HostQueryValidation"/>, the way
    /// <c>[EnableQuery]</c> validates one on a queryable read in the host's query configuration.
    /// </summary>
    public partial class NarrowedEnableQueryAttributeTests
    {
        // The React search's shape: two case-folded pattern terms, one over the non-nullable
        // string, beside an enum member and a null Guid. OData wraps it in a conversion over a
        // boolean subexpression, and the non-nullable column in one inside the function.
        private const string ReactSearchShapedFilter =
            "(contains(tolower(fixed),'x') or contains(tolower(name),'y')) "
                + "and kinds eq 'Beta' and groupId eq null";

        // A type-cast segment names a type by its full name. A row naming a type the model does
        // not hold is refused by the parser, so each such row is checked red under a mutant.
        private const string QueryModelNamespace = "Glory2Him.WebApp.Tests.Unit.Infrastructure.QueryModels";
        private const string ItemTypeName = QueryModelNamespace + "." + nameof(QueryItem);
        private const string ParentTypeName = QueryModelNamespace + "." + nameof(QueryItemParent);
        private const string ChildTypeName = QueryModelNamespace + "." + nameof(QueryItemChild);
    }
}
