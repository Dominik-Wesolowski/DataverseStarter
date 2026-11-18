using Company.Crm.Application.Shared;
using System;
using Microsoft.Xrm.Sdk;

namespace Company.Crm.Application.Entities.Accounts.Service
{
    public interface IAccountService : IService
    {
        void NormalizeAccountName(Entity target, Entity preImage = null);
        string GetNormalizedName(Guid accountId);
    }
}
