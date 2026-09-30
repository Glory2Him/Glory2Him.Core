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
using Glory2Him.Core.Models.Foundations.Associations;
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
