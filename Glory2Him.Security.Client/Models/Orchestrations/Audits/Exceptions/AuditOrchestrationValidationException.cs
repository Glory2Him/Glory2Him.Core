// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using Xeptions;

namespace Glory2Him.Security.Client.Models.Orchestrations.Audits.Exceptions
{
    internal class AuditOrchestrationValidationException : Xeption
    {
        public AuditOrchestrationValidationException(string message, Xeption innerException)
            : base(message, innerException)
        { }
    }
}
