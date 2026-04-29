using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Common.Exceptions
{
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}
