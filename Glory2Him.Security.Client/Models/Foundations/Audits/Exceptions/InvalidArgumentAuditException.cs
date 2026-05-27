// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using Xeptions;

namespace Glory2Him.Security.Client.Models.Foundations.Audits.Exceptions
{
    internal class InvalidArgumentAuditException : Xeption
    {
        public InvalidArgumentAuditException(string message)
            : base(message)
        { }
    }
}