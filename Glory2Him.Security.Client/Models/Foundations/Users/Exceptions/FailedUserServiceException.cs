// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using Xeptions;

namespace Glory2Him.Security.Client.Models.Foundations.Users.Exceptions
{
    internal class FailedUserServiceException : Xeption
    {
        public FailedUserServiceException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}