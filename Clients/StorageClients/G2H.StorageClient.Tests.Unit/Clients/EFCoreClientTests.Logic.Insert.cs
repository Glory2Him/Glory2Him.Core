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
        public async Task InsertAsyncShouldReturnInsertedObjectAsync()
        {
            // Given
            User randomUser = CreateRandomUser();
            User inputUser = randomUser;
            User expectedUser = inputUser.DeepClone();

            operationServiceMock.Setup(service =>
                service.InsertAsync(inputUser, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputUser);

            // When
            User actualUser = await efCoreClient.InsertAsync(inputUser);

            // Then
            actualUser.Should().BeEquivalentTo(expectedUser);

            operationServiceMock.Verify(service =>
                service.InsertAsync(inputUser, It.IsAny<CancellationToken>()),
                    Times.Once);

            operationServiceMock.VerifyNoOtherCalls();
        }
    }
}
