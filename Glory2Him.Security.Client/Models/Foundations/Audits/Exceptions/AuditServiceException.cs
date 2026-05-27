// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using Xeptions;

namespace Glory2Him.Security.Client.Models.Foundations.Audits.Exceptions
{
    internal class AuditServiceException : Xeption
    {
        public AuditServiceException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}