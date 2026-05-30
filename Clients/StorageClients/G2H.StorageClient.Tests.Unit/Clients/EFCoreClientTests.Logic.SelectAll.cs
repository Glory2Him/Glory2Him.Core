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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Moq;

namespace G2H.StorageClient.Tests.Unit.Clients
{
    public partial class EFCoreClientTests
    {
        [Fact]
        public async Task SelectAllAsyncShouldReturnExpectedUsersAsync()
        {
            // Given
            List<User> randomUsers = CreateRandomUsers();
            IQueryable<User> expectedUsers = randomUsers.AsQueryable().DeepClone();

            operationServiceMock.Setup(service =>
                service.SelectAllAsync<User>(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(randomUsers.AsQueryable());

            // When
            IQueryable<User> actualUsers = await efCoreClient.SelectAllAsync<User>();

            // Then
            actualUsers.Should().BeEquivalentTo(expectedUsers);

            operationServiceMock.Verify(service =>
                service.SelectAllAsync<User>(It.IsAny<CancellationToken>()),
                    Times.Once);

            operationServiceMock.VerifyNoOtherCalls();
        }
    }
}
