// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentTypes;

namespace Glory2Him.Core.Services.Foundations.ContentTypes
{
    public partial class ContentTypeService : IContentTypeService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IEventBroker eventBroker;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ContentTypeService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IEventBroker eventBroker,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.eventBroker = eventBroker;
            this.securityAuditBroker = securityAuditBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ContentType> AddContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                contentType = await this.securityAuditBroker.ApplyAddAuditValuesAsync(contentType);
                await ValidateOnAddContentTypeAsync(contentType);

                ContentType addedContentType =
                    await this.storageBroker.InsertContentTypeAsync(contentType, cancellationToken);

                var envelope = new EventEnvelope<ContentType> { Content = addedContentType };
                await this.eventBroker.PublishContentTypeAsync(envelope, "ContentTypeAdded");

                return addedContentType;
            });

        public async ValueTask<IQueryable<ContentType>> RetrieveAllContentTypesAsync(
            CancellationToken cancellationToken = default) =>
            await this.storageBroker.SelectAllContentTypesAsync();

        public ValueTask<ContentType> RetrieveContentTypeByIdAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public ValueTask<ContentType> ModifyContentTypeAsync(
            ContentType contentType,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public ValueTask<ContentType> RemoveContentTypeByIdAsync(
            Guid contentTypeId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public ValueTask<ContentType> HardRemoveContentTypeByIdAsync(
            Guid contentTypeId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
