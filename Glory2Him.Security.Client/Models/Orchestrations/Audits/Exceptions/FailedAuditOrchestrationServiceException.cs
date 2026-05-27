// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using Xeptions;

namespace Glory2Him.Security.Client.Models.Orchestrations.Audits.Exceptions
{
    internal class FailedAuditOrchestrationServiceException : Xeption
    {
        public FailedAuditOrchestrationServiceException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}