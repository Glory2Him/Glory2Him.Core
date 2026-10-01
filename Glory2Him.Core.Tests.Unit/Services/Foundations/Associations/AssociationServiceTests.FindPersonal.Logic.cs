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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldFindTheReadersLiveReactionRowAsync()
        {
            // given
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association lookupRequest = CreatePersonalLookupRequest(readerUserId);

            Association readersRow =
                CreateStoredPersonalRow(lookupRequest, isDeleted: false);

            // a reaction still waiting on review: a condition carrying a status, publish-date
            // or IsPublished term would miss it, and the lookup runs over the unfiltered store
            readersRow.ApprovalStatus = ApprovalStatus.Submitted;
            readersRow.IsPublished = false;
            readersRow.PublishDate = DateTimeOffset.UtcNow.AddDays(GetRandomNumber());

            // the unrelated rows come first, so a lookup that took the first row of the
            // store rather than the reader's would answer one of them
            List<Association> storageAssociations =
                CreateRandomAssociations().Append(readersRow).ToList();

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            var expectedMatch = new PersonalAssociationMatch
            {
                Id = readersRow.Id,
                EntityBKeyId = readersRow.EntityBKeyId,
                IsDeleted = false
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalLookupOver(storageAssociations, inputCancellationToken);

            // when
            PersonalAssociationMatch? actualMatch =
                await this.associationService.FindPersonalAssociationAsync(
                    lookupRequest,
                    inputCancellationToken);

            // then
            actualMatch.Should().BeEquivalentTo(expectedMatch);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(lookupRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalLookupAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldFindTheReadersWithdrawnReactionRowAsync()
        {
            // given
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association lookupRequest = CreatePersonalLookupRequest(readerUserId);

            Association readersWithdrawnRow =
                CreateStoredPersonalRow(lookupRequest, isDeleted: true);

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(readersWithdrawnRow).ToList();

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            var expectedMatch = new PersonalAssociationMatch
            {
                Id = readersWithdrawnRow.Id,
                EntityBKeyId = readersWithdrawnRow.EntityBKeyId,
                IsDeleted = true
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalLookupOver(storageAssociations, inputCancellationToken);

            // when
            PersonalAssociationMatch? actualMatch =
                await this.associationService.FindPersonalAssociationAsync(
                    lookupRequest,
                    inputCancellationToken);

            // then
            actualMatch.Should().BeEquivalentTo(expectedMatch);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(lookupRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalLookupAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldFindTheRowWhenTheEndpointsAreNamedTheOtherWayRoundAsync()
        {
            // given
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association canonicalRequest = CreatePersonalLookupRequest(readerUserId);

            Association readersRow =
                CreateStoredPersonalRow(canonicalRequest, isDeleted: false);

            // the reaction on endpoint A and the host on B: the order a caller cannot be expected
            // to know, and one no stored row is ever in
            Association reversedRequest = ReverseEndpoints(canonicalRequest);

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(readersRow).ToList();

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            var expectedMatch = new PersonalAssociationMatch
            {
                Id = readersRow.Id,
                EntityBKeyId = readersRow.EntityBKeyId,
                IsDeleted = false
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalLookupOver(storageAssociations, inputCancellationToken);

            // when
            PersonalAssociationMatch? actualMatch =
                await this.associationService.FindPersonalAssociationAsync(
                    reversedRequest,
                    inputCancellationToken);

            // then
            actualMatch.Should().BeEquivalentTo(expectedMatch);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(reversedRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalLookupAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(PersonalKeyTerms))]
        public async Task ShouldFindNoRowThatMissesATermOfThePersonalKeyAsync(string missedTerm)
        {
            // given
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association lookupRequest = CreatePersonalLookupRequest(readerUserId);

            Association nearMissRow = MissOnePersonalKeyTerm(
                CreateStoredPersonalRow(lookupRequest, isDeleted: false),
                missedTerm);

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(nearMissRow).ToList();

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalLookupOver(storageAssociations, inputCancellationToken);

            // when
            PersonalAssociationMatch? actualMatch =
                await this.associationService.FindPersonalAssociationAsync(
                    lookupRequest,
                    inputCancellationToken);

            // then
            actualMatch.Should().BeNull();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(lookupRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalLookupAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldPreferTheLiveRowThenTheLatestAsync(bool isTheWinnerLive)
        {
            // given: two rows of one reader on one host, written before this feature. The losing
            // row is stored first, so a lookup that took the first match would answer it. Where
            // the winner is live the loser is the more recently updated, so recency alone would
            // pick the loser; where both are withdrawn, recency is what decides.
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Association lookupRequest = CreatePersonalLookupRequest(readerUserId);
            DateTimeOffset earlierDate = GetRandomDateTimeOffset();
            DateTimeOffset laterDate = earlierDate.AddDays(GetRandomNumber());

            Association winningRow =
                CreateStoredPersonalRow(lookupRequest, isDeleted: isTheWinnerLive is false);

            Association losingRow =
                CreateStoredPersonalRow(lookupRequest, isDeleted: true);

            winningRow.UpdatedWhen = isTheWinnerLive ? earlierDate : laterDate;
            losingRow.UpdatedWhen = isTheWinnerLive ? laterDate : earlierDate;

            List<Association> storageAssociations =
                new List<Association> { losingRow, winningRow };

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            var expectedMatch = new PersonalAssociationMatch
            {
                Id = winningRow.Id,
                EntityBKeyId = winningRow.EntityBKeyId,
                IsDeleted = winningRow.IsDeleted
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalLookupOver(storageAssociations, inputCancellationToken);

            // when
            PersonalAssociationMatch? actualMatch =
                await this.associationService.FindPersonalAssociationAsync(
                    lookupRequest,
                    inputCancellationToken);

            // then
            actualMatch.Should().BeEquivalentTo(expectedMatch);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(lookupRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalLookupAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReadOnlyRolesOverAReaction))]
        public async Task ShouldFindTheReadersRowWhateverReadOnlyRoleTheyHoldAsync(
            string readOnlyRole)
        {
            // given: a lookup is a read, and reads ask no read-only role (§SEC14.7 posture A′
            // rule 3)
            string readerUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(readOnlyRole);
            Association lookupRequest = CreatePersonalLookupRequest(readerUserId);

            Association readersRow =
                CreateStoredPersonalRow(lookupRequest, isDeleted: false);

            List<Association> storageAssociations =
                CreateRandomAssociations().Append(readersRow).ToList();

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            var expectedMatch = new PersonalAssociationMatch
            {
                Id = readersRow.Id,
                EntityBKeyId = readersRow.EntityBKeyId,
                IsDeleted = false
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(readerUserId);

            SetupPersonalLookupOver(storageAssociations, inputCancellationToken);

            // when
            PersonalAssociationMatch? actualMatch =
                await this.associationService.FindPersonalAssociationAsync(
                    lookupRequest,
                    inputCancellationToken);

            // then
            actualMatch.Should().BeEquivalentTo(expectedMatch);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(lookupRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            VerifyPersonalLookupAsked(inputCancellationToken, Times.Once());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnFindPersonalIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            Association lookupRequest = CreatePersonalLookupRequest(GetRandomString());

            var unauthorizedAssociationException =
                new UnauthorizedAssociationException(
                    message: "The current user is not authenticated.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<PersonalAssociationMatch?> findTask =
                this.associationService.FindPersonalAssociationAsync(
                    lookupRequest,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(findTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(lookupRequest),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(CallerRoleSetsAskingForAnotherReadersRow))]
        public async Task ShouldFindNothingForAnotherReadersUserIdAsync(string[] callerRoles)
        {
            // given: both readers hold a row on the item, so a lookup that answered for the
            // caller or for the named reader would each find something
            string callerUserId = GetRandomString();
            string anotherReaderUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(callerRoles);
            Association lookupRequest = CreatePersonalLookupRequest(anotherReaderUserId);

            Association anotherReadersRow =
                CreateStoredPersonalRow(lookupRequest, isDeleted: false);

            Association callersRow =
                CreateStoredPersonalRow(lookupRequest, isDeleted: false);

            callersRow.UserId = callerUserId;

            List<Association> storageAssociations =
                new List<Association> { anotherReadersRow, callersRow };

            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            string expectedWarning =
                "Personal content item association lookup denied. User " +
                $"\"{callerUserId}\" asked for another user's row; reported to the caller as " +
                "not found.";

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext))
                    .ReturnsAsync(callerUserId);

            SetupPersonalLookupOver(storageAssociations, inputCancellationToken);

            // when
            PersonalAssociationMatch? actualMatch =
                await this.associationService.FindPersonalAssociationAsync(
                    lookupRequest,
                    inputCancellationToken);

            // then: the same answer as a reader with no row, and storage never asked
            actualMatch.Should().BeNull();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(lookupRequest),
                    Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(this.ambientSecurityContext),
                    Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(expectedWarning),
                    Times.Once);

            VerifyPersonalLookupAsked(inputCancellationToken, Times.Never());

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // the lookup serves the owner alone, so no role lets a caller find another reader's row —
        // the review tier's audit reads another reader's row by other means (§SEC14.7 posture A′
        // rule 7)
        public static TheoryData<string[]> CallerRoleSetsAskingForAnotherReadersRow() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.Administrators }
            };

        public static TheoryData<string> PersonalKeyTerms() =>
            new TheoryData<string>
            {
                nameof(Association.EntityAType),
                nameof(Association.EntityAEffectiveId),
                nameof(Association.EntityBType),
                nameof(Association.UserId)
            };

        // the reader's row, moved off the request's personal key on the named term alone:
        // another host type, another host, another far-end type or another reader
        private static Association MissOnePersonalKeyTerm(Association storedRow, string term)
        {
            switch (term)
            {
                case nameof(Association.EntityAType):
                    storedRow.EntityAType = EntityType.BibleReference;
                    break;

                case nameof(Association.EntityAEffectiveId):
                    storedRow.EntityAGroupId = Guid.NewGuid();
                    break;

                case nameof(Association.EntityBType):
                    storedRow.EntityBType = EntityType.Tag;
                    break;

                case nameof(Association.UserId):
                    storedRow.UserId = GetRandomString();
                    break;
            }

            return WithDatabaseComputedEffectiveIds(storedRow);
        }

        // A reader's reaction on a Quote, as the withdrawal hands it over: the host on endpoint A
        // under AllVersions, with a group id that differs from its key id, so the effective id the
        // lookup keys on is the group's and a lookup keyed on the version would miss the row.
        private static Association CreatePersonalLookupRequest(string readerUserId)
        {
            Association lookupRequest = CreateRandomReaction(readerUserId);
            lookupRequest.EntityAKeyId = Guid.NewGuid();
            lookupRequest.EntityAGroupId = Guid.NewGuid();
            lookupRequest.EntityBKeyId = Guid.NewGuid();
            lookupRequest.EntityBGroupId = lookupRequest.EntityBKeyId;

            return lookupRequest;
        }

        // The reader's stored row on the request's host, pointing at a reaction of its own, and
        // carrying the effective ids the database computes. Another version of the host is what
        // is stored, which an AllVersions host still names.
        private static Association CreateStoredPersonalRow(
            Association lookupRequest,
            bool isDeleted)
        {
            Association storedRow = lookupRequest.DeepClone();
            storedRow.Id = Guid.NewGuid();
            storedRow.EntityAKeyId = Guid.NewGuid();
            storedRow.EntityBKeyId = Guid.NewGuid();
            storedRow.EntityBGroupId = storedRow.EntityBKeyId;
            storedRow.IsDeleted = isDeleted;
            storedRow.DeletedBy = isDeleted ? storedRow.UserId : null;

            return WithDatabaseComputedEffectiveIds(storedRow);
        }

        // The mock stands in for the storage client: it APPLIES the query-shaping function the
        // service authored to the seeded set, so each test proves the condition by executing it
        // (§ARC12.2.1 rule 5). The function is matched by any value because a lambda cannot be
        // compared for equality — what it selects is the assertion, not its identity.
        private void SetupPersonalLookupOver(
            IEnumerable<Association> storageAssociations,
            CancellationToken cancellationToken) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<PersonalAssociationMatch>>>(),
                    cancellationToken))
                        .ReturnsAsync((
                            Func<IQueryable<Association>, IQueryable<PersonalAssociationMatch>> query,
                            CancellationToken _) =>
                                (IReadOnlyList<PersonalAssociationMatch>)
                                    query(storageAssociations.AsQueryable()).ToList());

        private void VerifyPersonalLookupAsked(CancellationToken cancellationToken, Times times) =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationsAsync(
                    It.IsAny<Func<IQueryable<Association>, IQueryable<PersonalAssociationMatch>>>(),
                    cancellationToken),
                times);
    }
}
