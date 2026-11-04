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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure.QueryModels
{
    /// <summary>
    /// The entity a queryable read answers in these tests. It holds a column of each kind the
    /// allowlist names: two nullable strings, so a needle can be another column; a non-nullable
    /// string, which OData wraps in an inserted conversion inside a function, as it does the
    /// host's own; an integer, a decimal, a flags enum and a nullable Guid. Beside those it holds
    /// what the allowlist refuses: a single-valued and a collection-valued navigation, through
    /// which it reaches past itself, and a list of strings, a collection-valued structural
    /// property, which a $select item can carry a nested option on.
    ///
    /// <para><c>[Required]</c> is what makes a string non-nullable to OData here, because this
    /// project has nullable reference types disabled.</para>
    /// </summary>
    public class QueryItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Other { get; set; }

        [Required]
        public string Fixed { get; set; }

        public int Version { get; set; }
        public decimal Score { get; set; }
        public QueryItemKinds Kinds { get; set; }
        public Guid? GroupId { get; set; }
        public QueryItemParent Parent { get; set; }
        public ICollection<QueryItemChild> Children { get; set; }
        public ICollection<string> Labels { get; set; }
    }
}
