// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Moq;

namespace G2H.StorageClient.Tests.Unit.Clients
{
    public partial class EFCoreClientTests
    {
        [Fact]
        public async Task BulkUpdateAsyncShouldDelegateToOperationServiceAsync()
        {
            // Given
            List<User> randomUsers = CreateRandomUsers();
            List<User> inputUsers = randomUsers;

            // When
            await efCoreClient.BulkUpdateAsync(inputUsers);

            // Then
            operationServiceMock.Verify(service =>
                service.BulkUpdateAsync(inputUsers, true, It.IsAny<CancellationToken>()),
                    Times.Once);

            operationServiceMock.VerifyNoOtherCalls();
        }
    }
}
