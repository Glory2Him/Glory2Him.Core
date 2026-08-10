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
using System.Threading.Tasks;
using G2H.Security.Client.Models.Clients.Access.Exceptions;
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Foundations.Access.Exceptions;
using G2H.Security.Client.Services.Foundations.Access;
using Xeptions;

namespace G2H.Security.Client.Clients.Access
{
    internal class AccessClient : IAccessClient
    {
        private readonly IAccessService accessService;

        public AccessClient(IAccessService accessService)
        {
            this.accessService = accessService;
        }

        public async ValueTask<ApprovalConditionsVerdict> EvaluateApprovalConditionsAsync(
            ApprovalConditionsRequest approvalConditionsRequest)
        {
            try
            {
                return await this.accessService
                    .EvaluateApprovalConditionsAsync(approvalConditionsRequest);
            }
            catch (AccessValidationException accessValidationException)
            {
                throw CreateAccessClientValidationException(
                    accessValidationException.InnerException as Xeption);
            }
            catch (AccessServiceException accessServiceException)
            {
                throw CreateAccessClientDependencyException(
                    accessServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateAccessClientServiceException(exception);
            }
        }

        public async ValueTask<AccessVerdict> MayRecordApprovalReviewAsync(
            RecordReviewRequest recordReviewRequest)
        {
            try
            {
                return await this.accessService
                    .MayRecordApprovalReviewAsync(recordReviewRequest);
            }
            catch (AccessValidationException accessValidationException)
            {
                throw CreateAccessClientValidationException(
                    accessValidationException.InnerException as Xeption);
            }
            catch (AccessServiceException accessServiceException)
            {
                throw CreateAccessClientDependencyException(
                    accessServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateAccessClientServiceException(exception);
            }
        }

        public async ValueTask<AccessVerdict> MayDecideApprovalAsync(
            DecideApprovalRequest decideApprovalRequest)
        {
            try
            {
                return await this.accessService
                    .MayDecideApprovalAsync(decideApprovalRequest);
            }
            catch (AccessValidationException accessValidationException)
            {
                throw CreateAccessClientValidationException(
                    accessValidationException.InnerException as Xeption);
            }
            catch (AccessServiceException accessServiceException)
            {
                throw CreateAccessClientDependencyException(
                    accessServiceException.InnerException as Xeption);
            }
            catch (Exception exception)
            {
                throw CreateAccessClientServiceException(exception);
            }
        }

        private static AccessClientValidationException CreateAccessClientValidationException(
            Xeption? innerException)
        {
            return new AccessClientValidationException(
                message: "Access client validation error occurred, fix the error and try again.",
                innerException!,
                data: innerException?.Data!);
        }

        private static AccessClientDependencyException CreateAccessClientDependencyException(
            Xeption? innerException)
        {
            return new AccessClientDependencyException(
                message: "Access client dependency error occurred, please contact support.",
                innerException!,
                data: innerException?.Data!);
        }

        private static AccessClientServiceException CreateAccessClientServiceException(
            Exception innerException)
        {
            return new AccessClientServiceException(
                message: "Access client service error occurred, please contact support.",
                innerException,
                data: innerException.Data);
        }
    }
}
