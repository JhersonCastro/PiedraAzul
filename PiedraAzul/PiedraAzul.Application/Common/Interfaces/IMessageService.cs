using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Common.Interfaces;
public interface IMessageService
{
    Task<bool> SMSAsync(string phoneNumber, string message);
}
