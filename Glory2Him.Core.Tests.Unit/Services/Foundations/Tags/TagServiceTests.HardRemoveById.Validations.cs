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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            var invalidTagId = Guid.Empty;

            var invalidTagException = new InvalidTagException(
                message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.UpsertDataList(
                key: nameof(Tag.Id),
                value: "Id is required");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: invalidTagException);

            // when
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    invalidTagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    hardRemoveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfTagNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Guid someTagId = Guid.NewGuid();
            Tag noTag = null;

            var notFoundTagException = new NotFoundTagException(
                message: $"Tag not found with id: {someTagId}.");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: notFoundTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noTag);

            // when
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    hardRemoveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Guid someTagId = Guid.NewGuid();

            var unauthorizedTagException = new UnauthorizedTagException(
                message: "The current user is not authenticated.");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    hardRemoveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            Guid someTagId = Guid.NewGuid();

            var unauthorizedTagException = new UnauthorizedTagException(
                message: "The current user is not allowed to permanently remove this tag.");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    hardRemoveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldBlockHardRemoveWhenTheCallerIsGloballyReadOnlyAndLogItAsync()
        {
            // given: the global ReadOnly ban outranks Admin, so a banned Admin is refused before
            // the row is even read — the destructive surface is not an exception to the site-wide
            // contribution ban.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Admin, Roles.ReadOnly);

            Guid someTagId = Guid.NewGuid();

            var unauthorizedTagException = new UnauthorizedTagException(
                message: "The current user is blocked from contributing tags.");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> hardRemoveTagByIdTask =
                this.tagService.HardRemoveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    hardRemoveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
