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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllAssociationsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);

            IQueryable<Association> randomAssociations =
                CreateRandomAssociations();

            foreach (Association association in randomAssociations)
            {
                association.IsDeleted = false;
            }

            IQueryable<Association> storageAssociations = randomAssociations;
            IQueryable<Association> expectedAssociations = storageAssociations;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicAssociationsWhenCallerIsAnonymousAsync()
        {
            // given: every row is editorial, so what this tests is the editorial posture,
            // which rule 7 leaves unchanged (AssociationService.AssociationReads.md §1 rule 4)
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association publicAssociation = CreateRandomAssociation();
            publicAssociation.IsDeleted = false;
            publicAssociation.UserId = null;
            publicAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicAssociation.IsPublished = true;
            publicAssociation.PublishDate = null;

            Association pastPublishedAssociation = CreateRandomAssociation();
            pastPublishedAssociation.IsDeleted = false;
            pastPublishedAssociation.UserId = null;
            pastPublishedAssociation.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedAssociation.IsPublished = true;

            pastPublishedAssociation.PublishDate =
                randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            Association draftAssociation = CreateRandomAssociation();
            draftAssociation.IsDeleted = false;
            draftAssociation.UserId = null;
            draftAssociation.ApprovalStatus = ApprovalStatus.Draft;
            draftAssociation.IsPublished = false;

            Association futurePublishedAssociation = CreateRandomAssociation();
            futurePublishedAssociation.IsDeleted = false;
            futurePublishedAssociation.UserId = null;
            futurePublishedAssociation.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedAssociation.IsPublished = true;

            futurePublishedAssociation.PublishDate =
                randomDateTimeOffset.AddDays(GetRandomNumber());

            Association deletedAssociation = CreateRandomAssociation();
            deletedAssociation.IsDeleted = true;
            deletedAssociation.UserId = null;
            deletedAssociation.ApprovalStatus = ApprovalStatus.Approved;
            deletedAssociation.IsPublished = true;
            deletedAssociation.PublishDate = null;

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    publicAssociation,
                    pastPublishedAssociation,
                    draftAssociation,
                    futurePublishedAssociation,
                    deletedAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    publicAssociation,
                    pastPublishedAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllPublicAndOwnAssociationsWhenUserHasNoReviewRoleAsync()
        {
            // given: every row is editorial, so what this tests is the editorial posture,
            // which rule 7 leaves unchanged (AssociationService.AssociationReads.md §1 rule 4)
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association publicAssociation = CreateRandomAssociation();
            publicAssociation.IsDeleted = false;
            publicAssociation.UserId = null;
            publicAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicAssociation.IsPublished = true;
            publicAssociation.PublishDate = null;

            Association ownDraftAssociation = CreateRandomAssociation();
            ownDraftAssociation.IsDeleted = false;
            ownDraftAssociation.UserId = null;
            ownDraftAssociation.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftAssociation.IsPublished = false;
            ownDraftAssociation.CreatedBy = randomActorUserId;

            Association othersDraftAssociation = CreateRandomAssociation();
            othersDraftAssociation.IsDeleted = false;
            othersDraftAssociation.UserId = null;
            othersDraftAssociation.ApprovalStatus = ApprovalStatus.Draft;
            othersDraftAssociation.IsPublished = false;

            Association ownDeletedAssociation = CreateRandomAssociation();
            ownDeletedAssociation.IsDeleted = true;
            ownDeletedAssociation.UserId = null;
            ownDeletedAssociation.CreatedBy = randomActorUserId;

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    publicAssociation,
                    ownDraftAssociation,
                    othersDraftAssociation,
                    ownDeletedAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    publicAssociation,
                    ownDraftAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRetrieveAllNonDeletedAssociationsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no clock, no
            // user-id resolution
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            Association publicAssociation = CreateRandomAssociation();
            publicAssociation.IsDeleted = false;
            publicAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicAssociation.IsPublished = true;
            publicAssociation.PublishDate = null;

            Association draftAssociation = CreateRandomAssociation();
            draftAssociation.IsDeleted = false;
            draftAssociation.ApprovalStatus = ApprovalStatus.Draft;
            draftAssociation.IsPublished = false;

            Association futurePublishedAssociation = CreateRandomAssociation();
            futurePublishedAssociation.IsDeleted = false;
            futurePublishedAssociation.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedAssociation.IsPublished = true;

            futurePublishedAssociation.PublishDate =
                GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            Association deletedAssociation = CreateRandomAssociation();
            deletedAssociation.IsDeleted = true;

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    publicAssociation,
                    draftAssociation,
                    futurePublishedAssociation,
                    deletedAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    publicAssociation,
                    draftAssociation,
                    futurePublishedAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveTheCallersOwnPersonalAssociationWhateverItsStatusAsync()
        {
            // given: the caller's own personal row, written by another account and still
            // under review, so only an owner term on UserId can admit it (§1 rules 1 and 3).
            // The control row misses on that term alone: another reader's.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association ownPersonalAssociation = CreateRandomAssociation();
            ownPersonalAssociation.IsDeleted = false;
            ownPersonalAssociation.ApprovalStatus = ApprovalStatus.Submitted;
            ownPersonalAssociation.IsPublished = false;
            ownPersonalAssociation.UserId = randomActorUserId;
            ownPersonalAssociation.CreatedBy = GetRandomString();

            Association othersPersonalAssociation = CreateRandomAssociation();
            othersPersonalAssociation.IsDeleted = false;
            othersPersonalAssociation.ApprovalStatus = ApprovalStatus.Submitted;
            othersPersonalAssociation.IsPublished = false;
            othersPersonalAssociation.UserId = GetRandomString();
            othersPersonalAssociation.CreatedBy = GetRandomString();

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    ownPersonalAssociation,
                    othersPersonalAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    ownPersonalAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAnotherReadersPersonalAssociationWhenCallerHoldsAnEndpointReviewRoleAsync()
        {
            // given: another reader's reaction, still under review, read by a caller whose one
            // role is scoped to the reaction endpoint. The control row is another reader's too,
            // on endpoints the role does not cover.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.ReactionReviewers);

            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association othersReaction = CreateRandomReaction(readerUserId: GetRandomString());
            othersReaction.IsDeleted = false;
            othersReaction.ApprovalStatus = ApprovalStatus.Submitted;
            othersReaction.IsPublished = false;

            Association othersUnscopedPersonalAssociation = CreateRandomAssociation();
            othersUnscopedPersonalAssociation.IsDeleted = false;
            othersUnscopedPersonalAssociation.ApprovalStatus = ApprovalStatus.Submitted;
            othersUnscopedPersonalAssociation.IsPublished = false;
            othersUnscopedPersonalAssociation.UserId = GetRandomString();
            othersUnscopedPersonalAssociation.CreatedBy = GetRandomString();

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    othersReaction,
                    othersUnscopedPersonalAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    othersReaction
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldNotRetrieveAPublicPersonalAssociationWhenCallerIsAnonymousAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given: a reader's reaction and an editorial association, both approved,
            // published and past their publish date. Only the editorial one is public.
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            int randomDaysInPast = GetRandomNegativeNumber();

            Association publicPersonalAssociation =
                CreateRandomReaction(readerUserId: GetRandomString());

            publicPersonalAssociation.IsDeleted = false;
            publicPersonalAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicPersonalAssociation.IsPublished = true;

            publicPersonalAssociation.PublishDate =
                randomDateTimeOffset.AddDays(randomDaysInPast);

            Association publicEditorialAssociation = CreateRandomAssociation();
            publicEditorialAssociation.IsDeleted = false;
            publicEditorialAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicEditorialAssociation.IsPublished = true;
            publicEditorialAssociation.UserId = null;

            publicEditorialAssociation.PublishDate =
                randomDateTimeOffset.AddDays(randomDaysInPast);

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    publicPersonalAssociation,
                    publicEditorialAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    publicEditorialAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotRetrieveAnotherReadersPublicPersonalAssociationAsync()
        {
            // given: another reader's reaction and an editorial association, both public,
            // read by a signed-in caller with no review role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association othersPublicPersonalAssociation =
                CreateRandomReaction(readerUserId: GetRandomString());

            othersPublicPersonalAssociation.IsDeleted = false;
            othersPublicPersonalAssociation.ApprovalStatus = ApprovalStatus.Approved;
            othersPublicPersonalAssociation.IsPublished = true;
            othersPublicPersonalAssociation.PublishDate = null;

            Association publicEditorialAssociation = CreateRandomAssociation();
            publicEditorialAssociation.IsDeleted = false;
            publicEditorialAssociation.ApprovalStatus = ApprovalStatus.Approved;
            publicEditorialAssociation.IsPublished = true;
            publicEditorialAssociation.PublishDate = null;
            publicEditorialAssociation.UserId = null;

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    othersPublicPersonalAssociation,
                    publicEditorialAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    publicEditorialAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotRetrieveAPersonalAssociationTheCallerCreatedForAnotherReaderAsync()
        {
            // given: two non-public rows the caller created. The personal one is another
            // reader's, so CreatedBy does not make it the caller's (§1 rule 3); the editorial
            // one is still theirs by CreatedBy (§1 rule 4).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association personalAssociationForAnotherReader = CreateRandomAssociation();
            personalAssociationForAnotherReader.IsDeleted = false;
            personalAssociationForAnotherReader.ApprovalStatus = ApprovalStatus.Submitted;
            personalAssociationForAnotherReader.IsPublished = false;
            personalAssociationForAnotherReader.UserId = GetRandomString();
            personalAssociationForAnotherReader.CreatedBy = randomActorUserId;

            Association ownEditorialAssociation = CreateRandomAssociation();
            ownEditorialAssociation.IsDeleted = false;
            ownEditorialAssociation.ApprovalStatus = ApprovalStatus.Submitted;
            ownEditorialAssociation.IsPublished = false;
            ownEditorialAssociation.UserId = null;
            ownEditorialAssociation.CreatedBy = randomActorUserId;

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    personalAssociationForAnotherReader,
                    ownEditorialAssociation
                }.AsQueryable();

            IQueryable<Association> expectedAssociations =
                new List<Association>
                {
                    ownEditorialAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEquivalentTo(expectedAssociations);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldNotRetrieveAPersonalAssociationForACallerWithNoUserIdAsync(
            string blankUserId)
        {
            // given: a signed-in caller whose identity carries a blank user id, and a
            // non-public personal row whose UserId is that same value. A caller with no user
            // id owns no personal row (§1 rule 5).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association blankUserIdPersonalAssociation = CreateRandomAssociation();
            blankUserIdPersonalAssociation.IsDeleted = false;
            blankUserIdPersonalAssociation.ApprovalStatus = ApprovalStatus.Submitted;
            blankUserIdPersonalAssociation.IsPublished = false;
            blankUserIdPersonalAssociation.UserId = blankUserId;
            blankUserIdPersonalAssociation.CreatedBy = GetRandomString();

            IQueryable<Association> storageAssociations =
                new List<Association>
                {
                    blankUserIdPersonalAssociation
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageAssociations);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(blankUserId);

            // when
            IQueryable<Association> actualAssociations =
                await this.associationService.RetrieveAllAssociationsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualAssociations.Should().BeEmpty();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllAssociationsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
