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
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Storages.Identity;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Foundations.IdentityUsers.Exceptions;
using Microsoft.Data.SqlClient;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.IdentityUsers
{
    /// <summary>
    /// Reads role membership out of the security database (design 12.7.1). Read-only by
    /// construction: its broker exposes no write, and this service publishes no event and stamps
    /// no audit values - there is nothing here for a fact to describe.
    ///
    /// <para>No EventEnvelope either, and that is the deliberate difference from every other
    /// foundation. An envelope exists to carry the caller identity into a decision, and this
    /// service takes none: WHO may enumerate users is decided by the orchestration above it
    /// before the call is made (16.7.4), because the answer depends on the entity being reviewed,
    /// which this service never sees.</para>
    /// </summary>
    internal class IdentityUserService : IIdentityUserService
    {
        private readonly IIdentityCoreStorageBroker identityCoreStorageBroker;
        private readonly ILoggingBroker loggingBroker;

        public IdentityUserService(
            IIdentityCoreStorageBroker identityCoreStorageBroker,
            ILoggingBroker loggingBroker)
        {
            this.identityCoreStorageBroker = identityCoreStorageBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<IReadOnlyList<IdentityUser>> RetrieveIdentityUsersInRolesAsync(
            IEnumerable<string> roleNames,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<string> normalizedRoleNames = (roleNames ?? Enumerable.Empty<string>())
                    .Where(roleName => string.IsNullOrWhiteSpace(roleName) is false)
                    .Select(roleName => roleName.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();

                // Fail closed. An empty tier means the caller composed the role names wrongly,
                // and returning everybody would turn a composition bug into a directory dump.
                if (normalizedRoleNames.Count == 0)
                {
                    return Array.Empty<IdentityUser>();
                }

                return await this.identityCoreStorageBroker.SelectIdentityUsersInRolesAsync(
                    normalizedRoleNames: normalizedRoleNames,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IReadOnlyList<IdentityUser>> RetrieveIdentityUsersByIdsAsync(
            IEnumerable<string> userIds,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<Guid> parsedUserIds = (userIds ?? Enumerable.Empty<string>())
                    .Where(userId => string.IsNullOrWhiteSpace(userId) is false)
                    .Select(userId =>
                        Guid.TryParse(userId.Trim(), out Guid parsedUserId)
                            ? parsedUserId
                            : Guid.Empty)
                    .Where(parsedUserId => parsedUserId != Guid.Empty)
                    .Distinct()
                    .ToList();

                // Fail closed, matching the roles read. An unparseable id is a caller bug, and
                // the empty set that a page of them collapses to must never be read as "all".
                if (parsedUserIds.Count == 0)
                {
                    return Array.Empty<IdentityUser>();
                }

                return await this.identityCoreStorageBroker.SelectIdentityUsersByIdsAsync(
                    userIds: parsedUserIds,
                    cancellationToken: cancellationToken);
            });

        private delegate ValueTask<IReadOnlyList<IdentityUser>> ReturningIdentityUsersFunction();

        private async ValueTask<IReadOnlyList<IdentityUser>> TryCatch(
            ReturningIdentityUsersFunction returningIdentityUsersFunction)
        {
            try
            {
                return await returningIdentityUsersFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutIdentityUserException = new TimeoutIdentityUserException(
                    message: "Failed identity user timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

                throw await CreateAndLogDependencyExceptionAsync(timeoutIdentityUserException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageIdentityUserException = new FailedStorageIdentityUserException(
                    message: "Failed identity user storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    failedStorageIdentityUserException);
            }
            catch (Exception exception)
            {
                var failedIdentityUserServiceException = new FailedIdentityUserServiceException(
                    message: "Failed identity user service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedIdentityUserServiceException);
            }
        }

        private async ValueTask<IdentityUserDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var identityUserDependencyException = new IdentityUserDependencyException(
                message: "Identity user dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(identityUserDependencyException);

            return identityUserDependencyException;
        }

        private async ValueTask<IdentityUserDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var identityUserDependencyException = new IdentityUserDependencyException(
                message: "Identity user dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(identityUserDependencyException);

            return identityUserDependencyException;
        }

        private async ValueTask<IdentityUserServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var identityUserServiceException = new IdentityUserServiceException(
                message: "Identity user service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(identityUserServiceException);

            return identityUserServiceException;
        }
    }
}
