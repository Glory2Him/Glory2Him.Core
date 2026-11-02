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
using EFxceptions.Models.Exceptions;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnFindOrAddIfCancellationRequestedAsync()
        {
            // given
            Tag someTag = CreateRandomTag();
            someTag.Name = GetRandomTagName();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    someTag,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                findOrAddTagTask.AsTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(FindOrAddStorageCalls))]
        public async Task ShouldThrowDependencyExceptionOnFindOrAddIfOperationCanceledExceptionOccursAndLogItAsync(
            string failingStorageCall)
        {
            // given: the read or the insert is cancelled by a token other than the caller's
            Tag someTag = CreateTagFiller(GetRandomDateTimeOffset()).Create();
            someTag.Name = GetRandomTagName();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutTagException =
                new TimeoutTagException(
                    message: "Failed tag timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedTagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: timeoutTagException);

            SetupFindOrAddStorageCallToThrow(
                someTag,
                failingStorageCall,
                operationCanceledException);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    findOrAddTagTask.AsTask);

            // then
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            VerifyFindOrAddStorageCallAsked(failingStorageCall);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(FindOrAddStorageCalls))]
        public async Task ShouldThrowCriticalDependencyExceptionOnFindOrAddIfSqlErrorOccursAndLogItAsync(
            string failingStorageCall)
        {
            // given
            Tag someTag = CreateTagFiller(GetRandomDateTimeOffset()).Create();
            someTag.Name = GetRandomTagName();
            SqlException sqlException = GetSqlException();

            var failedStorageTagException = new FailedStorageTagException(
                message: "Failed tag storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedTagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: failedStorageTagException);

            SetupFindOrAddStorageCallToThrow(
                someTag,
                failingStorageCall,
                sqlException);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    findOrAddTagTask.AsTask);

            // then
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            VerifyFindOrAddStorageCallAsked(failingStorageCall);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnFindOrAddIfTheNameIsTakenAndNotFoundAgainAndLogItAsync()
        {
            // given: IX_Tags_Name refuses the insert, and the second read finds no row either,
            // so the refusal is thrown on as the add throws it (§3 rule 7)
            Tag someTag = CreateTagFiller(GetRandomDateTimeOffset()).Create();
            someTag.Name = GetRandomTagName();

            var duplicateKeyWithUniqueIndexException =
                new DuplicateKeyWithUniqueIndexException(message: GetRandomString());

            var alreadyExistsTagException = new AlreadyExistsTagException(
                message: "Tag already exists, a uniqueness rule rejected the write.",
                innerException: duplicateKeyWithUniqueIndexException,
                data: duplicateKeyWithUniqueIndexException.Data);

            var expectedTagDependencyValidationException =
                new TagDependencyValidationException(
                    message: "Tag dependency validation error occurred, fix the errors and try again.",
                    innerException: alreadyExistsTagException);

            SetupFindOrAddStorageCallToThrow(
                someTag,
                failingStorageCall: "insert",
                duplicateKeyWithUniqueIndexException);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken);

            TagDependencyValidationException actualTagDependencyValidationException =
                await Assert.ThrowsAsync<TagDependencyValidationException>(
                    findOrAddTagTask.AsTask);

            // then: read twice, and the insert never tried again
            actualTagDependencyValidationException.Should().BeEquivalentTo(
                expectedTagDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.Verify(broker =>
                broker.InsertTagAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyValidationException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnFindOrAddIfTheInsertFailsAndLogItAsync()
        {
            // given: the insert is the add's own, and so is its failure (§3 rules 6 and 11)
            Tag someTag = CreateTagFiller(GetRandomDateTimeOffset()).Create();
            someTag.Name = GetRandomTagName();
            var dbUpdateException = new DbUpdateException();

            var failedStorageTagException = new FailedStorageTagException(
                message: "Failed tag storage error occurred, contact support.",
                innerException: dbUpdateException,
                data: dbUpdateException.Data);

            var expectedTagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: failedStorageTagException);

            SetupFindOrAddStorageCallToThrow(
                someTag,
                failingStorageCall: "insert",
                dbUpdateException);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    findOrAddTagTask.AsTask);

            // then
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            VerifyFindOrAddStorageCallAsked(failingStorageCall: "insert");

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(FindOrAddInsertRefusals))]
        public async Task ShouldThrowDependencyValidationExceptionOnFindOrAddIfTheInsertIsRefusedAndLogItAsync(
            Exception insertRefusal,
            Xeption expectedInnerException)
        {
            // given: a refusal the add answers as a dependency validation failure, none of them
            // IX_Tags_Name's, so the name is read once and the insert is not tried again
            // (§3 rules 6, 7 and 11)
            Tag someTag = CreateTagFiller(GetRandomDateTimeOffset()).Create();
            someTag.Name = GetRandomTagName();

            var expectedTagDependencyValidationException =
                new TagDependencyValidationException(
                    message: "Tag dependency validation error occurred, fix the errors and try again.",
                    innerException: expectedInnerException);

            SetupFindOrAddStorageCallToThrow(
                someTag,
                failingStorageCall: "insert",
                insertRefusal);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken);

            TagDependencyValidationException actualTagDependencyValidationException =
                await Assert.ThrowsAsync<TagDependencyValidationException>(
                    findOrAddTagTask.AsTask);

            // then
            actualTagDependencyValidationException.Should().BeEquivalentTo(
                expectedTagDependencyValidationException);

            VerifyFindOrAddStorageCallAsked(failingStorageCall: "insert");

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyValidationException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(FindOrAddStorageCalls))]
        public async Task ShouldThrowServiceExceptionOnFindOrAddIfServiceErrorOccursAndLogItAsync(
            string failingStorageCall)
        {
            // given
            Tag someTag = CreateTagFiller(GetRandomDateTimeOffset()).Create();
            someTag.Name = GetRandomTagName();
            var serviceException = new Exception(GetRandomString());

            var failedTagServiceException = new FailedTagServiceException(
                message: "Failed tag service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedTagServiceException = new TagServiceException(
                message: "Tag service error occurred, contact support.",
                innerException: failedTagServiceException);

            SetupFindOrAddStorageCallToThrow(
                someTag,
                failingStorageCall,
                serviceException);

            // when
            ValueTask<TagNameResult> findOrAddTagTask =
                this.tagService.FindOrAddTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken);

            TagServiceException actualTagServiceException =
                await Assert.ThrowsAsync<TagServiceException>(
                    findOrAddTagTask.AsTask);

            // then
            actualTagServiceException.Should().BeEquivalentTo(
                expectedTagServiceException);

            VerifyFindOrAddStorageCallAsked(failingStorageCall);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagServiceException))),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // the two storage calls the find-or-add makes: the read by name, and the insert it
        // makes where no row holds the name
        public static TheoryData<string> FindOrAddStorageCalls() =>
            new TheoryData<string>
            {
                "read",
                "insert"
            };

        // each refusal of the insert the add answers as a dependency validation failure, with
        // what the add wraps it in (TagService.Exceptions.cs); none is IX_Tags_Name's refusal,
        // which arrives as DuplicateKeyWithUniqueIndexException (§3 rule 7)
        public static TheoryData<Exception, Xeption> FindOrAddInsertRefusals()
        {
            string someMessage = GetRandomString();
            var duplicateKeyException = new DuplicateKeyException(someMessage);

            var foreignKeyConstraintConflictException =
                new ForeignKeyConstraintConflictException(someMessage);

            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            return new TheoryData<Exception, Xeption>
            {
                {
                    duplicateKeyException,
                    new AlreadyExistsTagException(
                        message: "Tag already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data)
                },
                {
                    foreignKeyConstraintConflictException,
                    new InvalidTagReferenceException(
                        message: "Invalid tag reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data)
                },
                {
                    dbUpdateConcurrencyException,
                    new LockedTagException(
                        message: "Locked tag record, please try again later.",
                        innerException: dbUpdateConcurrencyException,
                        data: dbUpdateConcurrencyException.Data)
                }
            };
        }

        private void SetupFindOrAddStorageCallToThrow(
            Tag someTag,
            string failingStorageCall,
            Exception exception)
        {
            if (failingStorageCall == "read")
            {
                this.storageBrokerMock.Setup(broker =>
                    broker.SelectTagsAsync(
                        It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                        It.IsAny<CancellationToken>()))
                            .ThrowsAsync(exception);

                return;
            }

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<TagNameMatch>());

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(someTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someTag.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(someTag.CreatedWhen);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertTagAsync(someTag, It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exception);
        }

        private void VerifyFindOrAddStorageCallAsked(string failingStorageCall)
        {
            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagsAsync(
                    It.IsAny<Func<IQueryable<Tag>, IQueryable<TagNameMatch>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            Times insertTimes = failingStorageCall == "insert"
                ? Times.Once()
                : Times.Never();

            this.storageBrokerMock.Verify(broker =>
                broker.InsertTagAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()),
                insertTimes);
        }
    }
}
