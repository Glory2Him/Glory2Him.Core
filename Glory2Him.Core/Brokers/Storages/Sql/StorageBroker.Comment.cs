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
using EFxceptions;
using Glory2Him.Core.Models.Foundations.Comments;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<Comment> Comments { get; set; }

        public async ValueTask<Comment> InsertCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(comment, cancellationToken);

        public async ValueTask<IQueryable<Comment>> SelectAllCommentsAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<Comment>(cancellationToken);

        public async ValueTask<Comment> SelectCommentByIdAsync(
            Guid commentId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<Comment>(new object[] { commentId }, cancellationToken);

        public async ValueTask<Comment> UpdateCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(comment, cancellationToken);

        public async ValueTask<Comment> DeleteCommentAsync(
            Comment comment,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(comment, cancellationToken);

        public async ValueTask BulkInsertCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(comments, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(comments, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(comments, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<Comment>> BulkReadCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(comments, cancellationToken);

        public async ValueTask BulkUpsertCommentsAsync(
            List<Comment> comments,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(comments, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsCommentAsync(
            Guid commentId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<Comment>(new object[] { commentId }, cancellationToken);
    }
}
