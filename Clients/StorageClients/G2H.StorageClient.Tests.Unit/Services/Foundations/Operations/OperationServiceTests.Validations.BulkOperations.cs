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
using System.Threading.Tasks;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;

namespace G2H.StorageClient.Tests.Unit.Services.Foundations.Operations
{
    public partial class OperationServiceTests
    {
        [Fact]
        public async Task BulkInsertAsyncShouldThrowArgumentNullExceptionWhenCollectionIsNull()
        {
            // Given
            IEnumerable<User> nullUsers = null;

            // When
            ValueTask bulkInsertTask = operationService.BulkInsertAsync(objects: nullUsers);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: bulkInsertTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task BulkReadAsyncShouldThrowArgumentNullExceptionWhenCollectionIsNull()
        {
            // Given
            IEnumerable<User> nullUsers = null;

            // When
            ValueTask<IEnumerable<User>> bulkReadTask = operationService.BulkReadAsync(objects: nullUsers);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: bulkReadTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task BulkUpdateAsyncShouldThrowArgumentNullExceptionWhenCollectionIsNull()
        {
            // Given
            IEnumerable<User> nullUsers = null;

            // When
            ValueTask bulkUpdateTask = operationService.BulkUpdateAsync(objects: nullUsers);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: bulkUpdateTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task BulkDeleteAsyncShouldThrowArgumentNullExceptionWhenCollectionIsNull()
        {
            // Given
            IEnumerable<User> nullUsers = null;

            // When
            ValueTask bulkDeleteTask = operationService.BulkDeleteAsync(objects: nullUsers);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: bulkDeleteTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task BulkUpsertAsyncShouldThrowArgumentNullExceptionWhenCollectionIsNull()
        {
            // Given
            IEnumerable<User> nullUsers = null;

            // When
            ValueTask bulkUpsertTask = operationService.BulkUpsertAsync(objects: nullUsers);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: bulkUpsertTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExistsAsyncShouldThrowArgumentNullExceptionWhenObjectIdsIsNull()
        {
            // Given
            object[] nullObjectIds = null;

            // When
            ValueTask<bool> existsTask = operationService.ExistsAsync<User>(objectIds: nullObjectIds);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: existsTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}


