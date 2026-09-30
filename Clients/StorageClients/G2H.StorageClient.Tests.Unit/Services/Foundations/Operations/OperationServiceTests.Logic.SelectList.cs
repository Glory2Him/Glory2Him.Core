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
using FluentAssertions;
using Force.DeepCloner;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Moq;

namespace G2H.StorageClient.Tests.Unit.Services.Foundations.Operations
{
    public partial class OperationServiceTests
    {
        [Fact]
        public async Task ShouldReturnOnlyTheRowsTheQuerySelectsAsync()
        {
            // Given
            string matchingSurname = GetRandomString();
            List<User> matchingUsers = CreateRandomUsers();
            matchingUsers.ForEach(user => user.Surname = matchingSurname);
            List<User> nonMatchingUsers = CreateRandomUsers();
            IQueryable<User> storageUsers = matchingUsers.Concat(nonMatchingUsers).AsQueryable();
            List<User> expectedUsers = matchingUsers.DeepClone();
            CancellationToken inputCancellationToken = new CancellationTokenSource().Token;

            Func<IQueryable<User>, IQueryable<User>> inputQuery = users =>
                users.Where(user => user.Surname == matchingSurname);

            storageBrokerMock.Setup(broker =>
                broker.SelectAllAsync<User>())
                    .ReturnsAsync(storageUsers);

            storageBrokerMock.Setup(broker =>
                broker.SelectListAsync(It.IsAny<IQueryable<User>>(), inputCancellationToken))
                    .ReturnsAsync((IQueryable<User> query, CancellationToken _) => query.ToList());

            // When
            IReadOnlyList<User> actualUsers =
                await operationService.SelectListAsync(inputQuery, inputCancellationToken);

            // Then
            actualUsers.Should().BeEquivalentTo(expectedUsers);

            storageBrokerMock.Verify(broker =>
                broker.SelectAllAsync<User>(),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.SelectListAsync(It.IsAny<IQueryable<User>>(), inputCancellationToken),
                    Times.Once);

            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnTheProjectionTheQueryShapesInItsOrderAsync()
        {
            // Given
            List<User> randomUsers = CreateRandomUsers();

            for (int index = 0; index < randomUsers.Count; index++)
                randomUsers[index].Username = $"{randomUsers.Count - index:D2}";

            IQueryable<User> storageUsers = randomUsers.AsQueryable();

            List<string> expectedEmails = randomUsers
                .AsEnumerable()
                .Reverse()
                .Select(user => user.Email)
                .ToList();

            Func<IQueryable<User>, IQueryable<string>> inputQuery = users =>
                users.OrderBy(user => user.Username).Select(user => user.Email);

            storageBrokerMock.Setup(broker =>
                broker.SelectAllAsync<User>())
                    .ReturnsAsync(storageUsers);

            storageBrokerMock.Setup(broker =>
                broker.SelectListAsync(It.IsAny<IQueryable<string>>(), default))
                    .ReturnsAsync((IQueryable<string> query, CancellationToken _) => query.ToList());

            // When
            IReadOnlyList<string> actualEmails = await operationService.SelectListAsync(inputQuery);

            // Then
            actualEmails.Should().Equal(expectedEmails);

            storageBrokerMock.Verify(broker =>
                broker.SelectAllAsync<User>(),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.SelectListAsync(It.IsAny<IQueryable<string>>(), default),
                    Times.Once);

            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnAnEmptyListWhenTheQuerySelectsNothingAsync()
        {
            // Given
            List<User> randomUsers = CreateRandomUsers();
            IQueryable<User> storageUsers = randomUsers.AsQueryable();

            Func<IQueryable<User>, IQueryable<User>> inputQuery = users =>
                users.Where(user => false);

            storageBrokerMock.Setup(broker =>
                broker.SelectAllAsync<User>())
                    .ReturnsAsync(storageUsers);

            storageBrokerMock.Setup(broker =>
                broker.SelectListAsync(It.IsAny<IQueryable<User>>(), default))
                    .ReturnsAsync((IQueryable<User> query, CancellationToken _) => query.ToList());

            // When
            IReadOnlyList<User> actualUsers = await operationService.SelectListAsync(inputQuery);

            // Then
            actualUsers.Should().NotBeNull().And.BeEmpty();

            storageBrokerMock.Verify(broker =>
                broker.SelectAllAsync<User>(),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.SelectListAsync(It.IsAny<IQueryable<User>>(), default),
                    Times.Once);

            storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}
