// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System.Collections;
using Xeptions;

namespace Glory2Him.Security.Client.Models.Clients.Audits.Exceptions
{
    public class AuditClientValidationException : Xeption
    {
        public AuditClientValidationException(string message, Xeption innerException, IDictionary data)
            : base(message, innerException, data)
        { }
    }
}