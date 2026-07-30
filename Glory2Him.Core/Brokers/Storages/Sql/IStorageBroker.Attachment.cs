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
using Glory2Him.Core.Models.Foundations.Attachments;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial interface IStorageBroker
    {
        ValueTask<Attachment> InsertAttachmentAsync(
            Attachment attachment,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Attachment>> SelectAllAttachmentsAsync();

        ValueTask<Attachment> SelectAttachmentByIdAsync(
            Guid attachmentId,
            CancellationToken cancellationToken = default);

        ValueTask<Attachment> UpdateAttachmentAsync(
            Attachment attachment,
            CancellationToken cancellationToken = default);

        ValueTask<Attachment> DeleteAttachmentAsync(
            Attachment attachment,
            CancellationToken cancellationToken = default);

        ValueTask BulkInsertAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpdateAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default);

        ValueTask BulkDeleteAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Attachment>> BulkReadAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default);

        ValueTask BulkUpsertAttachmentsAsync(
            List<Attachment> attachments,
            CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsAttachmentAsync(
            Guid attachmentId,
            CancellationToken cancellationToken = default);
    }
}
