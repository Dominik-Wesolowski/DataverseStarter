using System;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Application.Entities.Accounts.Repository
{
    public interface IAccountRepository
    {
        Entity GetById(Guid id);
    }
}