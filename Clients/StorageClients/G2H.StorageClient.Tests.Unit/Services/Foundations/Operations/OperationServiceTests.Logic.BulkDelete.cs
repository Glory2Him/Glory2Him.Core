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

using System.Collections.Generic;
using System.Threading.Tasks;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace G2H.StorageClient.Tests.Unit.Services.Foundations.Operations
{
    public partial class OperationServiceTests
    {
        [Fact]
        public async Task BulkDeleteAsyncShouldDeletedTheRecordsWithoutTransaction()
        {
            // Given
            bool useTransaction = false;
            IEnumerable<User> randomUsers = CreateRandomUsers();
            IEnumerable<User> inputUsers = randomUsers;

            // When
            await operationService.BulkDeleteAsync(objects: inputUsers, useTransaction);

            // Then
            storageBrokerMock.Verify(broker =>
                broker.BulkDeleteAsync(inputUsers, default),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.SaveChangesAsync(default),
                    Times.Once);

            foreach (var user in inputUsers)
            {
                storageBrokerMock.Verify(broker =>
                    broker.UpdateObjectStateAsync(user, EntityState.Detached),
                        Times.Once);
            }

            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task BulkDeleteAsyncShouldDeletedTheRecordsWithTransaction()
        {
            // Given
            bool useTransaction = true;
            IEnumerable<User> randomUsers = CreateRandomUsers();
            IEnumerable<User> inputUsers = randomUsers;

            storageBrokerMock.Setup(broker =>
                broker.BeginTransactionAsync(default))
                    .ReturnsAsync(dbContextTransactionMock.Object);

            // When
            await operationService.BulkDeleteAsync(objects: inputUsers, useTransaction);

            // Then
            storageBrokerMock.Verify(broker =>
                broker.BeginTransactionAsync(default),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.BulkDeleteAsync(inputUsers, default),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.SaveChangesAsync(default),
                    Times.Once);

            dbContextTransactionMock.Verify(transaction =>
                transaction.CommitAsync(default),
                    Times.Once);

            foreach (var user in inputUsers)
            {
                storageBrokerMock.Verify(broker =>
                    broker.UpdateObjectStateAsync(user, EntityState.Detached),
                        Times.Once);
            }

            dbContextTransactionMock.Verify(transaction =>
                transaction.Dispose(),
                    Times.Once);

            storageBrokerMock.VerifyNoOtherCalls();
            dbContextTransactionMock.VerifyNoOtherCalls();
        }
    }
}
