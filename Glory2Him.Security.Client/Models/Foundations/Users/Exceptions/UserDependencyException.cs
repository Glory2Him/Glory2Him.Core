// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using Xeptions;

namespace Glory2Him.Security.Client.Models.Foundations.Users.Exceptions
{
    internal class UserDependencyException : Xeption
    {
        public UserDependencyException(string message, Xeption innerException)
            : base(message, innerException)
        { }
    }
}