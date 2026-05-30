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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.StorageClient.Tests.Unit.Models.Foundations.Users;
using Microsoft.EntityFrameworkCore.Metadata;
using Moq;

namespace G2H.StorageClient.Tests.Unit.Services.Foundations.Operations
{
    public partial class OperationServiceTests
    {
        [Fact]
        public async Task ExistsAsyncShouldReturnTrueWhenEntityExists()
        {
            // Given
            User randomUser = CreateRandomUser();
            object[] inputIds = new object[] { randomUser.Id };
            List<User> storageUsers = new List<User> { randomUser };

            Mock<IEntityType> entityTypeMock = new Mock<IEntityType>();
            Mock<IKey> primaryKeyMock = new Mock<IKey>();
            Mock<IProperty> propertyMock = new Mock<IProperty>();

            PropertyInfo keyPropertyInfo = typeof(User).GetProperty("Id");
            propertyMock.Setup(p => p.PropertyInfo).Returns(keyPropertyInfo);
            propertyMock.Setup(p => p.Name).Returns(keyPropertyInfo.Name);
            propertyMock.Setup(p => p.ClrType).Returns(keyPropertyInfo.PropertyType);
            primaryKeyMock.Setup(pk => pk.Properties).Returns(new List<IProperty> { propertyMock.Object });
            entityTypeMock.Setup(et => et.FindPrimaryKey()).Returns(primaryKeyMock.Object);

            storageBrokerMock.Setup(broker =>
                broker.FindEntityTypeAsync<User>())
                    .ReturnsAsync(entityTypeMock.Object);

            storageBrokerMock.Setup(broker =>
                broker.SelectAllAsync<User>())
                    .ReturnsAsync(storageUsers.AsQueryable());

            // When
            bool actualResult = await operationService.ExistsAsync<User>(inputIds);

            // Then
            actualResult.Should().BeTrue();

            storageBrokerMock.Verify(broker =>
                broker.FindEntityTypeAsync<User>(),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.SelectAllAsync<User>(),
                    Times.Once);

            storageBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExistsAsyncShouldReturnFalseWhenEntityDoesNotExist()
        {
            // Given
            object[] inputIds = new object[] { Guid.NewGuid() };
            List<User> storageUsers = new List<User>();

            Mock<IEntityType> entityTypeMock = new Mock<IEntityType>();
            Mock<IKey> primaryKeyMock = new Mock<IKey>();
            Mock<IProperty> propertyMock = new Mock<IProperty>();

            PropertyInfo keyPropertyInfo = typeof(User).GetProperty("Id");
            propertyMock.Setup(p => p.PropertyInfo).Returns(keyPropertyInfo);
            propertyMock.Setup(p => p.Name).Returns(keyPropertyInfo.Name);
            propertyMock.Setup(p => p.ClrType).Returns(keyPropertyInfo.PropertyType);
            primaryKeyMock.Setup(pk => pk.Properties).Returns(new List<IProperty> { propertyMock.Object });
            entityTypeMock.Setup(et => et.FindPrimaryKey()).Returns(primaryKeyMock.Object);

            storageBrokerMock.Setup(broker =>
                broker.FindEntityTypeAsync<User>())
                    .ReturnsAsync(entityTypeMock.Object);

            storageBrokerMock.Setup(broker =>
                broker.SelectAllAsync<User>())
                    .ReturnsAsync(storageUsers.AsQueryable());

            // When
            bool actualResult = await operationService.ExistsAsync<User>(inputIds);

            // Then
            actualResult.Should().BeFalse();

            storageBrokerMock.Verify(broker =>
                broker.FindEntityTypeAsync<User>(),
                    Times.Once);

            storageBrokerMock.Verify(broker =>
                broker.SelectAllAsync<User>(),
                    Times.Once);

            storageBrokerMock.VerifyNoOtherCalls();
        }
    }
}
