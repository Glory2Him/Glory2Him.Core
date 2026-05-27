// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using Xeptions;

namespace Glory2Him.Security.Client.Models.Foundations.Audits.Exceptions
{
    internal class AuditDependencyException : Xeption
    {
        public AuditDependencyException(string message, Xeption innerException)
            : base(message, innerException)
        { }
    }
}