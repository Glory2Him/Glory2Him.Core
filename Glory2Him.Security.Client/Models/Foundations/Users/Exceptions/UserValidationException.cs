// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using Xeptions;

namespace Glory2Him.Security.Client.Models.Foundations.Users.Exceptions
{
    internal class UserValidationException : Xeption
    {
        public UserValidationException(string message, Xeption innerException)
            : base(message, innerException)
        { }
    }
}