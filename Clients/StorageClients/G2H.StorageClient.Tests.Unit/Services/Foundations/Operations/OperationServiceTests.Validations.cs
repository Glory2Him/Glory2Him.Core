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
using System.Threading.Tasks;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;

namespace G2H.StorageClient.Tests.Unit.Services.Foundations.Operations
{
    public partial class OperationServiceTests
    {
        [Fact]
        public async Task InsertAsyncShouldThrowArgumentNullExceptionWhenObjectIsNull()
        {
            // Given
            User nullUser = null;

            // When
            ValueTask<User> insertUserTask = operationService.InsertAsync(@object: nullUser);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: insertUserTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateAsyncShouldThrowArgumentNullExceptionWhenObjectIsNull()
        {
            // Given
            User nullUser = null;

            // When
            ValueTask<User> updateUserTask = operationService.UpdateAsync(@object: nullUser);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: updateUserTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteAsyncShouldThrowArgumentNullExceptionWhenObjectIsNull()
        {
            // Given
            User nullUser = null;

            // When
            ValueTask<User> deleteUserTask = operationService.DeleteAsync(@object: nullUser);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: deleteUserTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SelectAsyncShouldThrowArgumentNullExceptionWhenObjectIdsIsNull()
        {
            // Given
            object[] nullObjectIds = null;

            // When
            ValueTask<User> selectUserTask = operationService.SelectAsync<User>(objectIds: nullObjectIds);

            // Then
            await Assert.ThrowsAsync<ArgumentNullException>(testCode: selectUserTask.AsTask);
            storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}
