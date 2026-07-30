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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Comments;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<Comment> InsertCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Comment>> SelectAllCommentsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Comment> SelectCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken = default);

        ValueTask<Comment> UpdateCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default);

        ValueTask<Comment> DeleteCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Comment>> BulkReadCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsCommentAsync(
            Guid commentId,
            CancellationToken cancellationToken = default);
    }
}
