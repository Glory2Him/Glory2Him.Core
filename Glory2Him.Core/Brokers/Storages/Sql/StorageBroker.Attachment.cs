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
using Glory2Him.Core.Models.Foundations.Attachments;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<Attachment> Attachments { get; set; }

        public async ValueTask<Attachment> InsertAttachmentAsync(
            Attachment attachment,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(attachment, cancellationToken);

        public async ValueTask<IQueryable<Attachment>> SelectAllAttachmentsAsync() =>
            await SelectAllAsync<Attachment>();

        public async ValueTask<Attachment> SelectAttachmentByIdAsync(
            Guid attachmentId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<Attachment>(new object[] { attachmentId }, cancellationToken);

        public async ValueTask<Attachment> UpdateAttachmentAsync(
            Attachment attachment,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(attachment, cancellationToken);

        public async ValueTask<Attachment> DeleteAttachmentAsync(
            Attachment attachment,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(attachment, cancellationToken);

        public async ValueTask BulkInsertAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(attachments, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(attachments, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(attachments, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<Attachment>> BulkReadAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(attachments, cancellationToken);

        public async ValueTask BulkUpsertAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(attachments, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsAttachmentAsync(
            Guid attachmentId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<Attachment>(new object[] { attachmentId }, cancellationToken);
    }
}
