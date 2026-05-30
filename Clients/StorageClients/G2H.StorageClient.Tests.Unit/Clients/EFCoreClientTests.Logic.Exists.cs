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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Moq;

namespace G2H.StorageClient.Tests.Unit.Clients
{
    public partial class EFCoreClientTests
    {
        [Fact]
        public async Task ExistsAsyncShouldDelegateToOperationServiceAsync()
        {
            // Given
            User randomUser = CreateRandomUser();
            object[] inputIds = new object[] { randomUser.Id };
            bool expectedResult = true;

            operationServiceMock.Setup(service =>
                service.ExistsAsync<User>(inputIds, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedResult);

            // When
            bool actualResult = await efCoreClient.ExistsAsync<User>(inputIds);

            // Then
            actualResult.Should().Be(expectedResult);

            operationServiceMock.Verify(service =>
                service.ExistsAsync<User>(inputIds, It.IsAny<CancellationToken>()),
                    Times.Once);

            operationServiceMock.VerifyNoOtherCalls();
        }
    }
}
